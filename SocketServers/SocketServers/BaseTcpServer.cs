using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServers
{
	internal abstract class BaseTcpServer<C> : Server<C> where C : BaseConnection, IDisposable, new()
	{
		private readonly object sync;

		private Socket listener;

		private ThreadSafeDictionary<EndPoint, Connection<C>> connections;

		private readonly int receiveQueueSize;

		private bool socketReuseEnabled;

		private readonly int maxAcceptBacklog;

		private readonly int minAcceptBacklog;

		private int acceptBacklog;

		public BaseTcpServer(ServersManagerConfig config)
		{
			sync = new object();
			receiveQueueSize = config.TcpQueueSize;
			minAcceptBacklog = config.TcpMinAcceptBacklog;
			maxAcceptBacklog = config.TcpMaxAcceptBacklog;
			socketReuseEnabled = (minAcceptBacklog < maxAcceptBacklog);
		}

		public override void Start()
		{
			lock (sync)
			{
				isRunning = true;
				connections = new ThreadSafeDictionary<EndPoint, Connection<C>>();
				listener = new Socket(realEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				listener.Bind(realEndPoint);
				listener.Listen(0);
				ThreadPool.QueueUserWorkItem(EnqueueAsyncAccepts, null);
			}
		}

		private void EnqueueAsyncAccepts(object stateInfo)
		{
			lock (sync)
			{
				while (true)
				{
					int num = Thread.VolatileRead(ref acceptBacklog);
					if (!isRunning || num >= minAcceptBacklog)
					{
						break;
					}
					if (Interlocked.CompareExchange(ref acceptBacklog, num + 1, num) == num)
					{
						ServerAsyncEventArgs serverAsyncEventArgs = EventArgsManager.Get();
						serverAsyncEventArgs.FreeBuffer();
						listener.AcceptAsync(serverAsyncEventArgs, Accept_Completed);
					}
				}
			}
		}

		public override void Dispose()
		{
			isRunning = false;
			lock (sync)
			{
				if (listener != null)
				{
					connections.ForEach(EndTcpConnection);
					connections.Clear();
					listener.Close();
					listener = null;
				}
			}
		}

		protected abstract void OnNewTcpConnection(Connection<C> connection);

		protected abstract void OnEndTcpConnection(Connection<C> connection);

		protected abstract bool OnTcpReceived(Connection<C> connection, ref ServerAsyncEventArgs e);

		protected void SendAsync(Connection<C> connection, ServerAsyncEventArgs e)
		{
			if (connection == null)
			{
				if (e.ConnectionId == -1)
				{
					try
					{
						Socket socket = new Socket(realEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
						socket.Bind(realEndPoint);
						socket.ConnectAsync(e, Connect_Completed);
					}
					catch (SocketException ex)
					{
						e.Completed = base.Send_Completed;
						e.SocketError = ex.SocketErrorCode;
						e.OnCompleted(null);
					}
					return;
				}
				e.Completed = base.Send_Completed;
				e.SocketError = SocketError.NotConnected;
				e.OnCompleted(null);
			}
			else if (e.ConnectionId == -1 || e.ConnectionId == -2 || e.ConnectionId == connection.Id)
			{
				connection.Socket.SendAsync(e, base.Send_Completed);
			}
			else
			{
				e.Completed = base.Send_Completed;
				e.SocketError = SocketError.NotConnected;
				e.OnCompleted(null);
			}
		}

		private void Connect_Completed(Socket socket1, ServerAsyncEventArgs e)
		{
			bool flag = false;
			Connection<C> value = null;
			if (e.SocketError == SocketError.Success)
			{
				value = CreateConnection(socket1, e.SocketError);
				flag = (value != null);
			}
			else
			{
				while (e.SocketError == SocketError.AddressAlreadyInUse && !connections.TryGetValue(e.RemoteEndPoint, out value))
				{
					Thread.Sleep(0);
					if (socket1.ConnectAsync(e))
					{
						return;
					}
					if (e.SocketError == SocketError.Success)
					{
						value = CreateConnection(socket1, e.SocketError);
						flag = (value != null);
					}
				}
			}
			if (e.SocketError == SocketError.Success && flag)
			{
				NewTcpConnection(value);
			}
			e.Completed = base.Send_Completed;
			e.OnCompleted(socket1);
		}

		private Connection<C> CreateConnection(Socket socket, SocketError error)
		{
			Connection<C> connection = null;
			if (isRunning && error == SocketError.Success)
			{
				connection = new Connection<C>(socket, isSocketAccepted: true, receiveQueueSize);
				Connection<C> connection2 = connections.Replace(connection.RemoteEndPoint, connection);
				if (connection2 != null)
				{
					EndTcpConnection(connection2);
				}
			}
			else
			{
				socket?.SafeShutdownClose();
			}
			return connection;
		}

		private void Accept_Completed(Socket none, ServerAsyncEventArgs e)
		{
			Socket acceptSocket = e.AcceptSocket;
			SocketError socketError = e.SocketError;
			while (true)
			{
				int num = Thread.VolatileRead(ref acceptBacklog);
				if (isRunning && num <= minAcceptBacklog)
				{
					e.AcceptSocket = null;
					listener.AcceptAsync(e, Accept_Completed);
					break;
				}
				if (Interlocked.CompareExchange(ref acceptBacklog, num - 1, num) == num)
				{
					EventArgsManager.Put(e);
					break;
				}
			}
			Connection<C> connection = CreateConnection(acceptSocket, socketError);
			if (connection != null)
			{
				NewTcpConnection(connection);
			}
		}

		private void NewTcpConnection(Connection<C> connection)
		{
			OnNewTcpConnection(connection);
			ServerAsyncEventArgs e;
			for (int i = 0; i < receiveQueueSize; i++)
			{
				e = EventArgsManager.Get();
				if (!TcpReceiveAsync(connection, e))
				{
					connection.ReceiveQueue.Put(e);
				}
			}
			e = connection.ReceiveQueue.GetCurrent();
			if (e != null)
			{
				Receive_Completed(connection.Socket, e);
			}
		}

		private void EndTcpConnection(Connection<C> connection)
		{
			if (connection.Close())
			{
				OnEndTcpConnection(connection);
				if (connection.Socket.Connected)
				{
					try
					{
						connection.Socket.Shutdown(SocketShutdown.Both);
					}
					catch (SocketException)
					{
					}
				}
				if (connection.IsSocketAccepted && socketReuseEnabled)
				{
					try
					{
						try
						{
							ServerAsyncEventArgs serverAsyncEventArgs = EventArgsManager.Get();
							serverAsyncEventArgs.FreeBuffer();
							serverAsyncEventArgs.DisconnectReuseSocket = true;
							serverAsyncEventArgs.Completed = Disconnect_Completed;
							if (!connection.Socket.DisconnectAsync(serverAsyncEventArgs))
							{
								serverAsyncEventArgs.OnCompleted(connection.Socket);
							}
						}
						catch (SocketException)
						{
						}
					}
					catch (NotSupportedException)
					{
						socketReuseEnabled = false;
					}
				}
				if (!socketReuseEnabled)
				{
					connection.Socket.Close();
				}
			}
		}

		private void Disconnect_Completed(Socket socket, ServerAsyncEventArgs e)
		{
			while (true)
			{
				int num = Thread.VolatileRead(ref acceptBacklog);
				if (!isRunning || num >= maxAcceptBacklog)
				{
					break;
				}
				if (Interlocked.CompareExchange(ref acceptBacklog, num + 1, num) == num)
				{
					e.AcceptSocket = socket;
					try
					{
						listener.AcceptAsync(e, Accept_Completed);
					}
					catch
					{
						EventArgsManager.Put(e);
					}
					return;
				}
			}
			EventArgsManager.Put(e);
		}

		private void Receive_Completed(Socket socket, ServerAsyncEventArgs e)
		{
			try
			{
				connections.TryGetValue(e.RemoteEndPoint, out Connection<C> value);
				if (value != null && value.Socket == socket && value.Id == e.ConnectionId)
				{
					while (true)
					{
						if (e != null)
						{
							value.ReceiveQueue.Put(e);
							e = null;
						}
						e = value.ReceiveQueue.GetCurrent();
						if (e == null)
						{
							return;
						}
						bool flag = true;
						if (isRunning && e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
						{
							flag = !OnTcpReceived(value, ref e);
						}
						if (flag)
						{
							break;
						}
						value.ReceiveQueue.Next();
						if (e == null)
						{
							e = EventArgsManager.Get();
						}
						else
						{
							e.SetDefaultValue();
						}
						if (TcpReceiveAsync(value, e))
						{
							e = null;
						}
					}
					connections.Remove(value.RemoteEndPoint, value);
					EndTcpConnection(value);
				}
			}
			finally
			{
				if (e != null)
				{
					EventArgsManager.Put(ref e);
				}
			}
		}

		private bool TcpReceiveAsync(Connection<C> connection, ServerAsyncEventArgs e)
		{
			PrepareEventArgs(connection, e);
			try
			{
				connection.SpinLock.Enter();
				e.SequenceNumber = connection.ReceiveQueue.SequenceNumber;
				try
				{
					if (!connection.IsClosed)
					{
						bool result = connection.Socket.ReceiveAsync(e);
						connection.ReceiveQueue.SequenceNumber++;
						return result;
					}
				}
				finally
				{
					connection.SpinLock.Exit();
				}
			}
			catch (ObjectDisposedException)
			{
			}
			EventArgsManager.Put(ref e);
			return true;
		}

		protected void PrepareEventArgs(Connection<C> connection, ServerAsyncEventArgs e)
		{
			e.ConnectionId = connection.Id;
			e.RemoteEndPoint = connection.RemoteEndPoint;
			e.Completed = Receive_Completed;
		}

		protected Connection<C> GetTcpConnection(IPEndPoint remote)
		{
			Connection<C> value = null;
			if (connections.TryGetValue(remote, out value) && !value.Socket.Connected)
			{
				connections.Remove(remote, value);
				EndTcpConnection(value);
				value = null;
			}
			return value;
		}
	}
}
