using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServers
{
	internal abstract class Server<C> : IDisposable where C : BaseConnection, IDisposable, new()
	{
		internal class Connection<C2> : IDisposable where C2 : IDisposable
		{
			internal class CyclicBuffer : IDisposable
			{
				private bool disposed;

				private int size;

				private volatile int dequeueIndex;

				private ServerAsyncEventArgs[] queue;

				public volatile int SequenceNumber;

				public CyclicBuffer(int size1)
				{
					disposed = false;
					size = size1;
					dequeueIndex = 0;
					SequenceNumber = 0;
					queue = new ServerAsyncEventArgs[size];
				}

				public void Dispose()
				{
					disposed = true;
					ServerAsyncEventArgs value = null;
					for (int i = 0; i < queue.Length; i++)
					{
						if (queue[i] != null)
						{
							value = Interlocked.Exchange(ref queue[i], null);
						}
						if (value != null)
						{
							EventArgsManager.Put(ref value);
						}
					}
				}

				public void Put(ServerAsyncEventArgs e)
				{
					int num = e.SequenceNumber % size;
					Interlocked.Exchange(ref queue[num], e);
					if (disposed && Interlocked.Exchange(ref queue[num], null) != null)
					{
						EventArgsManager.Put(e);
					}
				}

				public ServerAsyncEventArgs GetCurrent()
				{
					return Interlocked.Exchange(ref queue[dequeueIndex], null);
				}

				public void Next()
				{
					Interlocked.Exchange(ref dequeueIndex, (dequeueIndex + 1) % size);
				}
			}

			private static int connectionCount;

			private SspiContext sspiContext;

			private int closeCount;

			public readonly int Id;

			public readonly Socket Socket;

			public readonly bool IsSocketAccepted;

			public readonly SpinLock SpinLock;

			public readonly CyclicBuffer ReceiveQueue;

			public readonly IPEndPoint RemoteEndPoint;

			public C2 UserConnection;

			internal bool IsClosed => Thread.VolatileRead(ref closeCount) > 0;

			public SspiContext SspiContext
			{
				get
				{
					if (sspiContext == null)
					{
						sspiContext = new SspiContext();
					}
					return sspiContext;
				}
			}

			public Connection(Socket socket, bool isSocketAccepted, int receivedQueueSize)
			{
				Id = NewConnectionId();
				ReceiveQueue = new CyclicBuffer(receivedQueueSize);
				SpinLock = new SpinLock();
				Socket = socket;
				IsSocketAccepted = isSocketAccepted;
				RemoteEndPoint = (socket.RemoteEndPoint as IPEndPoint);
			}

			internal bool Close()
			{
				bool flag = Interlocked.Increment(ref closeCount) == 1;
				if (flag)
				{
					ReceiveQueue.Dispose();
					if (sspiContext != null)
					{
						sspiContext.Dispose();
					}
					if (UserConnection != null)
					{
						UserConnection.Dispose();
					}
				}
				return flag;
			}

			void IDisposable.Dispose()
			{
				if (Close())
				{
					Socket.SafeShutdownClose();
				}
			}

			private int NewConnectionId()
			{
				int num;
				do
				{
					num = Interlocked.Increment(ref connectionCount);
				}
				while (num == -1 || num == -2);
				return num;
			}
		}

		protected volatile bool isRunning;

		protected ServerEndPoint realEndPoint;

		private ServerEndPoint fakeEndPoint;

		private long ip4Mask;

		private long ip4Subnet;

		public ServerEventHandlerVal<Server<C>, ServerInfoEventArgs> Failed;

		public ServerEventHandlerRef<Server<C>, C, ServerAsyncEventArgs, bool> Received;

		public ServerEventHandlerVal<Server<C>, ServerAsyncEventArgs> Sent;

		public ServerEventHandlerVal<Server<C>, C, ServerAsyncEventArgs> BeforeSend;

		public ServerEventHandlerVal<Server<C>, C> NewConnection;

		public ServerEventHandlerVal<Server<C>, C> EndConnection;

		public ServerEndPoint LocalEndPoint => realEndPoint;

		public ServerEndPoint FakeEndPoint => fakeEndPoint;

		public Server()
		{
		}

		public abstract void Start();

		public abstract void Dispose();

		public abstract void SendAsync(ServerAsyncEventArgs e);

		protected void Send_Completed(Socket socket, ServerAsyncEventArgs e)
		{
			Sent(this, e);
		}

		protected virtual bool OnReceived(Connection<C> c, ref ServerAsyncEventArgs e)
		{
			e.LocalEndPoint = GetLocalEndpoint(e.RemoteEndPoint.Address);
			return Received(this, (c != null) ? c.UserConnection : null, ref e);
		}

		protected virtual void OnFailed(ServerInfoEventArgs e)
		{
			Failed(this, e);
		}

		protected virtual void OnNewConnection(Connection<C> connection)
		{
			C userConnection = new C();
			userConnection.LocalEndPoint = GetLocalEndpoint(connection.RemoteEndPoint.Address);
			userConnection.RemoteEndPoint = connection.RemoteEndPoint;
			userConnection.Id = connection.Id;
			connection.UserConnection = userConnection;
			NewConnection(this, connection.UserConnection);
		}

		protected virtual void OnEndConnection(Connection<C> connection)
		{
			EndConnection(this, connection.UserConnection);
		}

		protected void OnBeforeSend(Connection<C> connection, ServerAsyncEventArgs e)
		{
			BeforeSend(this, (connection == null) ? null : connection.UserConnection, e);
		}

		public static Server<C> Create(ServerEndPoint real, IPEndPoint ip4fake, IPAddress ip4mask, ServersManagerConfig config)
		{
			Server<C> server = null;
			if (real.Protocol == ServerProtocol.Tcp)
			{
				server = new TcpServer<C>(config);
			}
			else if (real.Protocol == ServerProtocol.Udp)
			{
				server = new UdpServer<C>(config);
			}
			else
			{
				if (real.Protocol != ServerProtocol.Tls)
				{
					throw new InvalidOperationException("Protocol is not supported.");
				}
				server = new SspiTlsServer<C>(config);
			}
			server.realEndPoint = real.Clone();
			if (ip4fake != null)
			{
				if (ip4mask == null)
				{
					throw new ArgumentNullException("ip4mask");
				}
				server.fakeEndPoint = new ServerEndPoint(server.realEndPoint.Protocol, ip4fake);
				server.ip4Mask = GetIPv4Long(ip4mask);
				server.ip4Subnet = (GetIPv4Long(real.Address) & server.ip4Mask);
			}
			return server;
		}

		public ServerEndPoint GetLocalEndpoint(IPAddress addr)
		{
			if (fakeEndPoint != null && !IPAddress.IsLoopback(addr))
			{
				long iPv4Long = GetIPv4Long(addr);
				if ((iPv4Long & ip4Mask) != ip4Subnet)
				{
					return fakeEndPoint;
				}
			}
			return realEndPoint;
		}

		private static long GetIPv4Long(IPAddress address)
		{
			return address.Address;
		}
	}
}
