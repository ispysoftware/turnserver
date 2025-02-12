using System;
using System.Net;

namespace SocketServers
{
	public class ServerEndPoint : IPEndPoint, IEquatable<ServerEndPoint>
	{
		public static ServerEndPoint NoneEndPoint = new ServerEndPoint(ServerProtocol.Tcp, IPAddress.None, 0);

		public ServerProtocol Protocol
		{
			get;
			set;
		}

		public ProtocolPort ProtocolPort => new ProtocolPort(Protocol, base.Port);

		public ServerEndPoint(ProtocolPort protocolPort, IPAddress address)
			: base(address, protocolPort.Port)
		{
			Protocol = protocolPort.Protocol;
		}

		public ServerEndPoint(ServerProtocol protocol, IPAddress address, int port)
			: base(address, port)
		{
			Protocol = protocol;
		}

		public ServerEndPoint(ServerProtocol protocol, IPEndPoint endpoint)
			: base(endpoint.Address, endpoint.Port)
		{
			Protocol = protocol;
		}

		public new bool Equals(object x)
		{
			if (x is ServerEndPoint)
			{
				return Equals(x as ServerEndPoint);
			}
			return false;
		}

		public bool Equals(ServerEndPoint p)
		{
			if (AddressFamily == p.AddressFamily && base.Port == p.Port && base.Address.Equals(p.Address))
			{
				return Protocol == p.Protocol;
			}
			return false;
		}

		public bool Equals(ServerProtocol protocol, IPEndPoint endpoint)
		{
			if (AddressFamily == endpoint.AddressFamily && base.Port == endpoint.Port && base.Address.Equals(endpoint.Address))
			{
				return Protocol == protocol;
			}
			return false;
		}

		public new string ToString()
		{
			return $"{Protocol.ToString()}:{base.ToString()}";
		}

		public ServerEndPoint Clone()
		{
			return new ServerEndPoint(Protocol, base.Address, base.Port);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() ^ (int)Protocol;
		}
	}
}
