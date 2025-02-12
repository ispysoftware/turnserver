using System;
using System.Net.Sockets;
using System.Threading;

namespace SocketServers
{
	internal class UdpServer<C> : Server<C> where C : BaseConnection, IDisposable, new()
	{
		private const int SIO_UDP_CONNRESET = -1744830452;

		private object sync;

		private Socket socket;

		private int queueSize;

		public UdpServer(ServersManagerConfig config)
		{
			sync = new object();
			queueSize = ((config.UdpQueueSize > 0) ? config.UdpQueueSize : 16);
		}

		public override void Start()
		{
			lock (sync)
			{
				isRunning = true;
				socket = new Socket(realEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				socket.Bind(realEndPoint);
				socket.IOControl(-1744830452, new byte[4], null);
				ThreadPool.QueueUserWorkItem(EnqueueBuffers, queueSize);
			}
		}

		public override void Dispose()
		{
			isRunning = false;
			lock (sync)
			{
				if (socket != null)
				{
					socket.SafeShutdownClose();
					socket = null;
				}
			}
		}

		public override void SendAsync(ServerAsyncEventArgs e)
		{
			OnBeforeSend(null, e);
			e.Completed = base.Send_Completed;
			if (!socket.SendToAsync(e))
			{
				e.OnCompleted(socket);
			}
		}

		private void EnqueueBuffers(object stateInfo)
		{
			int num = (int)stateInfo;
			lock (sync)
			{
				if (socket != null)
				{
					for (int i = 0; i < num; i++)
					{
						if (!isRunning)
						{
							break;
						}
						ServerAsyncEventArgs serverAsyncEventArgs = EventArgsManager.Get();
						PrepareBuffer(serverAsyncEventArgs);
						if (!socket.ReceiveFromAsync(serverAsyncEventArgs))
						{
							serverAsyncEventArgs.OnCompleted(socket);
						}
					}
				}
			}
		}

		private void ReceiveFrom_Completed(Socket socket, ServerAsyncEventArgs e)
		{
			while (isRunning)
			{
				if (e.SocketError == SocketError.Success)
				{
					OnReceived(null, ref e);
					if (e == null)
					{
						e = EventArgsManager.Get();
					}
					PrepareBuffer(e);
					try
					{
						if (socket.ReceiveFromAsync(e))
						{
							e = null;
							break;
						}
					}
					catch (ObjectDisposedException)
					{
					}
				}
				else if (isRunning)
				{
					Dispose();
					OnFailed(new ServerInfoEventArgs(realEndPoint, e.SocketError));
				}
			}
			if (e != null)
			{
				EventArgsManager.Put(e);
			}
		}

		private void PrepareBuffer(ServerAsyncEventArgs e)
		{
			e.Completed = ReceiveFrom_Completed;
			e.SetAnyRemote(realEndPoint.AddressFamily);
			e.AllocateBuffer();
		}
	}
}
