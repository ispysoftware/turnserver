using System;

namespace SocketServers
{
	public static class ServerProtocolHelper
	{
		public static bool TryConvertTo(this string protocolName, out ServerProtocol protocol)
		{
			if (string.Compare(protocolName, "udp", ignoreCase: true) == 0)
			{
				protocol = ServerProtocol.Udp;
				return true;
			}
			if (string.Compare(protocolName, "tcp", ignoreCase: true) == 0)
			{
				protocol = ServerProtocol.Tcp;
				return true;
			}
			if (string.Compare(protocolName, "tls", ignoreCase: true) == 0)
			{
				protocol = ServerProtocol.Tls;
				return true;
			}
			protocol = ServerProtocol.Udp;
			return false;
		}

		public static ServerProtocol ConvertTo(this string protocolName)
		{
			if (!protocolName.TryConvertTo(out ServerProtocol protocol))
			{
				throw new ArgumentOutOfRangeException("protocolName");
			}
			return protocol;
		}
	}
}
