namespace SocketServers
{
	public struct ProtocolPort
	{
		public int Port;

		public ServerProtocol Protocol;

		public ProtocolPort(ServerProtocol protocol, int port)
		{
			Protocol = protocol;
			Port = port;
		}
	}
}
