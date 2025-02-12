using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SocketServers
{
	public class ServersManager<C> : IDisposable where C : BaseConnection, IDisposable, new()
	{
		private class EndpointInfo
		{
			public readonly ProtocolPort ProtocolPort;

			public readonly UnicastIPAddressInformation AddressInformation;

			public readonly ServerEndPoint ServerEndPoint;

			public EndpointInfo(ProtocolPort protocolPort, UnicastIPAddressInformation addressInformation)
			{
				ProtocolPort = protocolPort;
				AddressInformation = addressInformation;
				ServerEndPoint = new ServerEndPoint(ProtocolPort, AddressInformation.Address);
			}
		}

		private object sync;

		private bool running;

		private ThreadSafeDictionary<ServerEndPoint, Server<C>> servers;

		private ThreadSafeDictionary<ServerEndPoint, Server<C>> fakeServers;

		private List<ProtocolPort> protocolPorts;

		private List<UnicastIPAddressInformation> networkAddressInfos;

		private ServersManagerConfig config;

		private int nextPort;

		private Logger logger;

		public Func<NetworkInterface, IPInterfaceProperties, UnicastIPAddressInformation, bool> AddressPredicate
		{
			get;
			set;
		}

		public Func<ServerEndPoint, IPEndPoint> FakeAddressAction
		{
			get;
			set;
		}

		public Logger Logger => logger;

		private List<UnicastIPAddressInformation> NetworkAddresses
		{
			get
			{
				if (networkAddressInfos == null)
				{
					networkAddressInfos = new List<UnicastIPAddressInformation>();
					NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
					NetworkInterface[] array = allNetworkInterfaces;
					foreach (NetworkInterface networkInterface in array)
					{
						if (networkInterface.OperationalStatus == OperationalStatus.Up)
						{
							IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
							foreach (UnicastIPAddressInformation unicastAddress in iPProperties.UnicastAddresses)
							{
								if (AddressPredicate(networkInterface, iPProperties, unicastAddress))
								{
									networkAddressInfos.Add(unicastAddress);
								}
							}
						}
					}
				}
				return networkAddressInfos;
			}
		}

		public event EventHandler<ServerChangeEventArgs> ServerRemoved;

		public event EventHandler<ServerChangeEventArgs> ServerAdded;

		public event EventHandler<ServerInfoEventArgs> ServerInfo;

		public event ServerEventHandlerRef<ServersManager<C>, C, ServerAsyncEventArgs, bool> Received;

		public event ServerEventHandlerRef<ServersManager<C>, ServerAsyncEventArgs> Sent;

		public event ServerEventHandlerVal<ServersManager<C>, C> NewConnection;

		public event ServerEventHandlerVal<ServersManager<C>, C> EndConnection;

		public event ServerEventHandlerVal<ServersManager<C>, C, ServerAsyncEventArgs> BeforeSend;

		public ServersManager(ServersManagerConfig config)
		{
			if (!BufferManager.IsInitialized())
			{
				BufferManager.Initialize(256);
			}
			if (!EventArgsManager.IsInitialized())
			{
				EventArgsManager.Initialize();
			}
			running = false;
			sync = new object();
			protocolPorts = new List<ProtocolPort>();
			servers = new ThreadSafeDictionary<ServerEndPoint, Server<C>>();
			fakeServers = new ThreadSafeDictionary<ServerEndPoint, Server<C>>();
			AddressPredicate = DefaultAddressPredicate;
			FakeAddressAction = DefaultFakeAddressAction;
			this.config = config;
			nextPort = config.MinPort;
			logger = new Logger();
		}

		private static bool DefaultAddressPredicate(NetworkInterface interface1, IPInterfaceProperties properties, UnicastIPAddressInformation addrInfo)
		{
			return true;
		}

		private static IPEndPoint DefaultFakeAddressAction(ServerEndPoint endpoint)
		{
			return null;
		}

		public SocketError Start(bool ignoreErrros)
		{
			lock (sync)
			{
				running = true;
				NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
				return AddServers(GetEndpointInfos(protocolPorts), ignoreErrros);
			}
		}

		public void Dispose()
		{
			NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
			lock (sync)
			{
				running = false;
				servers.Remove((ServerEndPoint endpoint) => true, OnServerRemoved);
				logger.Dispose();
			}
		}

		public SocketError Bind(ProtocolPort pp)
		{
			lock (sync)
			{
				protocolPorts.Add(pp);
				if (running)
				{
					return AddServers(GetEndpointInfos(pp), ignoreErrors: false);
				}
				return SocketError.Success;
			}
		}

		public SocketError Bind(ref ProtocolPort pp)
		{
			lock (sync)
			{
				if (nextPort < 0)
				{
					throw new InvalidOperationException("Port range was not assigned");
				}
				for (int i = 0; i < config.MaxPort - config.MinPort; i++)
				{
					pp.Port = nextPort++;
					SocketError socketError = AddServers(GetEndpointInfos(pp), ignoreErrors: false);
					if (socketError != SocketError.AddressAlreadyInUse)
					{
						return socketError;
					}
				}
				return SocketError.TooManyOpenSockets;
			}
		}

		public void Unbind(ProtocolPort pp)
		{
			lock (sync)
			{
				protocolPorts.Remove(pp);
				if (running)
				{
					servers.Remove((ServerEndPoint endpoint) => endpoint.Port == pp.Port && endpoint.Protocol == pp.Protocol, OnServerRemoved);
				}
			}
		}

		public void SendAsync(ServerAsyncEventArgs e)
		{
			Server<C> value = servers.GetValue(e.LocalEndPoint);
			if (value == null)
			{
				value = fakeServers.GetValue(e.LocalEndPoint);
			}
			if (value != null)
			{
				if (logger.IsEnabled)
				{
					logger.Write(e, incomingOutgoing: false);
				}
				value.SendAsync(e);
			}
			else
			{
				e.SocketError = SocketError.NetworkDown;
				Server_Sent(null, e);
			}
		}

		public X509Certificate2 FindCertificateInStore(string thumbprint)
		{
			X509Store x509Store = null;
			try
			{
				x509Store = new X509Store(StoreLocation.LocalMachine);
				X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
				if (x509Certificate2Collection.Count > 0)
				{
					return x509Certificate2Collection[0];
				}
				return null;
			}
			finally
			{
				x509Store?.Close();
			}
		}

		public bool IsLocalAddress(IPAddress address)
		{
			return servers.Contain((Server<C> server) => server.LocalEndPoint.Address.Equals(address) || (server.FakeEndPoint != null && server.FakeEndPoint.Address.Equals(address)));
		}

		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			OnServerInfo(new ServerInfoEventArgs(ServerEndPoint.NoneEndPoint, "NetworkChange.NetworkAddressChanged"));
			lock (sync)
			{
				networkAddressInfos = null;
				IEnumerable<EndpointInfo> infos = GetEndpointInfos(protocolPorts);
				AddServers(infos, ignoreErrors: true);
				servers.Remove(delegate(ServerEndPoint endpoint)
				{
					foreach (EndpointInfo item in infos)
					{
						if (item.ServerEndPoint.Equals(endpoint))
						{
							return false;
						}
					}
					return true;
				}, OnServerRemoved);
			}
		}

		private IEnumerable<EndpointInfo> GetEndpointInfos(IEnumerable<ProtocolPort> pps)
		{
			foreach (UnicastIPAddressInformation address in NetworkAddresses)
			{
				foreach (ProtocolPort pp in pps)
				{
					yield return new EndpointInfo(pp, address);
				}
			}
		}

		private IEnumerable<EndpointInfo> GetEndpointInfos(ProtocolPort pp)
		{
			foreach (UnicastIPAddressInformation address in NetworkAddresses)
			{
				yield return new EndpointInfo(pp, address);
			}
		}

		private SocketError AddServers(IEnumerable<EndpointInfo> infos, bool ignoreErrors)
		{
			SocketError socketError = SocketError.Success;
			List<Server<C>> list = new List<Server<C>>();
			foreach (EndpointInfo info in infos)
			{
				if (!servers.ContainsKey(info.ServerEndPoint))
				{
					IPEndPoint ip4fake = null;
					if (info.ServerEndPoint.AddressFamily == AddressFamily.InterNetwork && info.AddressInformation.IPv4Mask != null)
					{
						ip4fake = FakeAddressAction(info.ServerEndPoint);
					}
					Server<C> server = Server<C>.Create(info.ServerEndPoint, ip4fake, info.AddressInformation.IPv4Mask, config);
					server.Received = Server_Received;
					server.Sent = Server_Sent;
					server.Failed = Server_Failed;
					server.NewConnection = Server_NewConnection;
					server.EndConnection = Server_EndConnection;
					server.BeforeSend = Server_BeforeSend;
					try
					{
						server.Start();
					}
					catch (SocketException ex)
					{
						if (!ignoreErrors)
						{
							socketError = ex.SocketErrorCode;
							goto IL_0144;
						}
						OnServerInfo(new ServerInfoEventArgs(info.ServerEndPoint, ex));
					}
					list.Add(server);
				}
			}
			goto IL_0144;
			IL_0144:
			if (!ignoreErrors && list.Count == 0)
			{
				socketError = SocketError.SystemNotReady;
			}
			if (socketError != 0)
			{
				foreach (Server<C> item in list)
				{
					item.Dispose();
				}
				return socketError;
			}
			foreach (Server<C> item2 in list)
			{
				servers.Add(item2.LocalEndPoint, item2);
				OnServerAdded(item2);
			}
			return socketError;
		}

		private bool Server_Received(Server<C> server, C c, ref ServerAsyncEventArgs e)
		{
			try
			{
				if (logger.IsEnabled)
				{
					logger.Write(e, incomingOutgoing: true);
				}
				if (this.Received != null)
				{
					return this.Received(this, c, ref e);
				}
				return false;
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in Received event handler", innerException);
			}
		}

		private void Server_Sent(Server<C> server, ServerAsyncEventArgs e)
		{
			try
			{
				if (this.Sent != null)
				{
					this.Sent(this, ref e);
				}
				if (e != null)
				{
					EventArgsManager.Put(e);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in Sent event handler", innerException);
			}
		}

		private void Server_Failed(Server<C> server, ServerInfoEventArgs e)
		{
			servers.Remove(server.LocalEndPoint, server);
			OnServerRemoved(server);
			OnServerInfo(e);
		}

		private void Server_NewConnection(Server<C> server, C e)
		{
			try
			{
				if (this.NewConnection != null)
				{
					this.NewConnection(this, e);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in NewConnection event handler", innerException);
			}
		}

		private void Server_EndConnection(Server<C> server, C c)
		{
			try
			{
				if (this.EndConnection != null)
				{
					this.EndConnection(this, c);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in EndConnection event handler", innerException);
			}
		}

		private void Server_BeforeSend(Server<C> server, C c, ServerAsyncEventArgs e)
		{
			try
			{
				if (this.BeforeSend != null)
				{
					this.BeforeSend(this, c, e);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in BeforeSend event handler", innerException);
			}
		}

		private void OnServerAdded(Server<C> server)
		{
			try
			{
				if (server.FakeEndPoint != null)
				{
					fakeServers.Add(server.FakeEndPoint, server);
					if (this.ServerAdded != null)
					{
						this.ServerAdded(this, new ServerChangeEventArgs(server.FakeEndPoint));
					}
				}
				if (this.ServerAdded != null)
				{
					this.ServerAdded(this, new ServerChangeEventArgs(server.LocalEndPoint));
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in ServerAdded event handler", innerException);
			}
		}

		private void OnServerRemoved(Server<C> server)
		{
			server.Dispose();
			try
			{
				if (server.FakeEndPoint != null)
				{
					fakeServers.Remove(server.FakeEndPoint, server);
					if (this.ServerRemoved != null)
					{
						this.ServerRemoved(this, new ServerChangeEventArgs(server.FakeEndPoint));
					}
				}
				if (this.ServerRemoved != null)
				{
					this.ServerRemoved(this, new ServerChangeEventArgs(server.LocalEndPoint));
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in ServerRemoved event handler", innerException);
			}
		}

		private void OnServerInfo(ServerInfoEventArgs e)
		{
			try
			{
				if (this.ServerInfo != null)
				{
					this.ServerInfo(this, e);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error in ServerInfo event handler", innerException);
			}
		}
	}
}
