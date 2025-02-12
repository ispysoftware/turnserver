using System;

namespace SocketServers
{
	internal class TcpServer<C> : BaseTcpServer<C> where C : BaseConnection, IDisposable, new()
	{
		public TcpServer(ServersManagerConfig config)
			: base(config)
		{
		}

		protected override void OnNewTcpConnection(Connection<C> connection)
		{
			OnNewConnection(connection);
		}

		protected override void OnEndTcpConnection(Connection<C> connection)
		{
			OnEndConnection(connection);
		}

		protected override bool OnTcpReceived(Connection<C> connection, ref ServerAsyncEventArgs e)
		{
			return OnReceived(connection, ref e);
		}

		public override void SendAsync(ServerAsyncEventArgs e)
		{
			Connection<C> tcpConnection = GetTcpConnection(e.RemoteEndPoint);
			OnBeforeSend(tcpConnection, e);
			SendAsync(tcpConnection, e);
		}
	}
}
