using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SocketServers
{
	public class ServerAsyncEventArgs : EventArgs, ILockFreePoolItem, IDisposable
	{
		internal delegate void CompletedEventHandler(Socket socket, ServerAsyncEventArgs e);

		public const int AnyNewConnectionId = -1;

		public const int AnyConnectionId = -2;

		public const int DefaultSize = 2048;

		public static int DefaultOffsetOffset;

		internal int SequenceNumber;

		private bool isPooled;

		private int count;

		private int offsetOffset;

		private int bytesTransferred;

		private ArraySegment<byte> segment;

		private SocketAsyncEventArgs socketArgs;

		internal CompletedEventHandler Completed;

		bool ILockFreePoolItem.IsPooled
		{
			set
			{
				isPooled = value;
			}
		}

		public int UserTokenForSending
		{
			get;
			set;
		}

		public int UserTokenForSending2
		{
			get;
			set;
		}

		internal Socket AcceptSocket
		{
			get
			{
				return socketArgs.AcceptSocket;
			}
			set
			{
				socketArgs.AcceptSocket = value;
			}
		}

		public SocketError SocketError
		{
			get
			{
				return socketArgs.SocketError;
			}
			internal set
			{
				socketArgs.SocketError = value;
			}
		}

		public bool DisconnectReuseSocket
		{
			get
			{
				return socketArgs.DisconnectReuseSocket;
			}
			set
			{
				socketArgs.DisconnectReuseSocket = value;
			}
		}

		public IPEndPoint RemoteEndPoint
		{
			get
			{
				return socketArgs.RemoteEndPoint as IPEndPoint;
			}
			set
			{
				if (!(socketArgs.RemoteEndPoint as IPEndPoint).Equals(value))
				{
					(socketArgs.RemoteEndPoint as IPEndPoint).Address = new IPAddress(value.Address.GetAddressBytes());
					(socketArgs.RemoteEndPoint as IPEndPoint).Port = value.Port;
				}
			}
		}

		public ServerEndPoint LocalEndPoint
		{
			get;
			set;
		}

		public int ConnectionId
		{
			get;
			set;
		}

		public int OffsetOffset
		{
			get
			{
				return offsetOffset;
			}
			set
			{
				offsetOffset = value;
			}
		}

		public int Offset
		{
			get
			{
				return segment.Offset + offsetOffset;
			}
			set
			{
				offsetOffset = value - segment.Offset;
			}
		}

		public int Count
		{
			get
			{
				return count;
			}
			set
			{
				count = value;
			}
		}

		public int BytesTransferred
		{
			get
			{
				return bytesTransferred;
			}
			set
			{
				bytesTransferred = value;
			}
		}

		public byte[] Buffer
		{
			get
			{
				if (offsetOffset + count > segment.Count)
				{
					ReAllocateBuffer(keepData: false);
				}
				return segment.Array;
			}
		}

		public int MinimumRequredOffsetOffset
		{
			get
			{
				if (LocalEndPoint == null)
				{
					throw new ArgumentException("You MUST set LocalEndPoint before this action.");
				}
				if (LocalEndPoint.Protocol != ServerProtocol.Tls)
				{
					return 0;
				}
				return 256;
			}
		}

		public ArraySegment<byte> BufferSegment => segment;

		public ArraySegment<byte> TransferredData => new ArraySegment<byte>(Buffer, Offset, BytesTransferred);

		public ArraySegment<byte> IncomingData => TransferredData;

		public ArraySegment<byte> OutgoingData => new ArraySegment<byte>(Buffer, Offset, Count);

		public ServerAsyncEventArgs()
		{
			socketArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0),
				UserToken = this
			};
			socketArgs.Completed += SocketArgs_Completed;
			SetDefaultValue();
		}

		public void Dispose()
		{
			if (isPooled)
			{
				BufferManager.Free(ref segment);
				socketArgs.Dispose();
			}
			else
			{
				EventArgsManager.Put(this);
			}
		}

		public void SetDefaultValue()
		{
			ConnectionId = -1;
			LocalEndPoint = null;
			Completed = null;
			AcceptSocket = null;
			if (segment.Array != null && segment.Count != 2048)
			{
				BufferManager.Free(ref segment);
			}
			offsetOffset = DefaultOffsetOffset;
			count = 2048 - DefaultOffsetOffset;
			bytesTransferred = 0;
			UserTokenForSending = 0;
		}

		public ServerAsyncEventArgs CreateDeepCopy()
		{
			ServerAsyncEventArgs serverAsyncEventArgs = EventArgsManager.Get();
			serverAsyncEventArgs.CopyAddressesFrom(this);
			serverAsyncEventArgs.offsetOffset = offsetOffset;
			serverAsyncEventArgs.count = count;
			serverAsyncEventArgs.AllocateBuffer();
			serverAsyncEventArgs.bytesTransferred = bytesTransferred;
			serverAsyncEventArgs.UserTokenForSending = UserTokenForSending;
			System.Buffer.BlockCopy(Buffer, Offset, serverAsyncEventArgs.Buffer, serverAsyncEventArgs.Offset, serverAsyncEventArgs.Count);
			return serverAsyncEventArgs;
		}

		public static implicit operator SocketAsyncEventArgs(ServerAsyncEventArgs serverArgs)
		{
			if (serverArgs.Count > 0)
			{
				serverArgs.AllocateBuffer();
				serverArgs.socketArgs.SetBuffer(serverArgs.Buffer, serverArgs.Offset, serverArgs.Count);
			}
			else
			{
				serverArgs.socketArgs.SetBuffer(null, -1, -1);
			}
			return serverArgs.socketArgs;
		}

		public void CopyAddressesFrom(ServerAsyncEventArgs e)
		{
			ConnectionId = e.ConnectionId;
			LocalEndPoint = e.LocalEndPoint;
			RemoteEndPoint = e.RemoteEndPoint;
		}

		public void CopyAddressesFrom(BaseConnection c)
		{
			ConnectionId = c.Id;
			LocalEndPoint = c.LocalEndPoint;
			RemoteEndPoint = c.RemoteEndPoint;
		}

		public void SetAnyRemote(AddressFamily family)
		{
			if (family == AddressFamily.InterNetwork)
			{
				RemoteEndPoint.Address = IPAddress.Any;
			}
			else
			{
				RemoteEndPoint.Address = IPAddress.IPv6Any;
			}
			RemoteEndPoint.Port = 0;
		}

		public void SetMaxCount()
		{
			count = segment.Count - offsetOffset;
		}

		public void AllocateBuffer()
		{
			ReAllocateBuffer(keepData: false);
		}

		public void AllocateBuffer(int applicationOffsetOffset, int count)
		{
			OffsetOffset = MinimumRequredOffsetOffset + applicationOffsetOffset;
			Count = count;
			ReAllocateBuffer(keepData: false);
		}

		public void ReAllocateBuffer(bool keepData)
		{
			if (offsetOffset + count > segment.Count)
			{
				ArraySegment<byte> buffer = BufferManager.Allocate(offsetOffset + count);
				if (keepData && segment.IsValid())
				{
					System.Buffer.BlockCopy(segment.Array, segment.Offset, buffer.Array, buffer.Offset, segment.Count);
				}
				AttachBuffer(buffer);
			}
		}

		public void FreeBuffer()
		{
			BufferManager.Free(ref segment);
			Count = 0;
		}

		public void BlockCopyFrom(ArraySegment<byte> data)
		{
			if (data.Count > Count)
			{
				throw new ArgumentOutOfRangeException("BlockCopyFrom: data.Count > Count");
			}
			System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Offset, data.Count);
		}

		public void BlockCopyFrom(int offsetOffset, ArraySegment<byte> data)
		{
			if (data.Count > Count)
			{
				throw new ArgumentOutOfRangeException("BlockCopyFrom: data.Count > Count");
			}
			System.Buffer.BlockCopy(data.Array, data.Offset, Buffer, Offset + offsetOffset, data.Count);
		}

		public void AttachBuffer(ArraySegment<byte> buffer)
		{
			BufferManager.Free(segment);
			segment = buffer;
		}

		public void AttachBuffer(StreamBuffer buffer)
		{
			OffsetOffset = 0;
			BytesTransferred = buffer.BytesTransferred;
			AttachBuffer(buffer.Detach());
			Count = segment.Count;
		}

		public ArraySegment<byte> DetachBuffer()
		{
			ArraySegment<byte> result = segment;
			segment = default(ArraySegment<byte>);
			count = 2048;
			offsetOffset = 0;
			bytesTransferred = 0;
			return result;
		}

		internal void OnCompleted(Socket socket)
		{
			if (Completed != null)
			{
				Completed(socket, this);
			}
		}

		private static void SocketArgs_Completed(object sender, SocketAsyncEventArgs e)
		{
			ServerAsyncEventArgs serverAsyncEventArgs = e.UserToken as ServerAsyncEventArgs;
			serverAsyncEventArgs.bytesTransferred = e.BytesTransferred;
			serverAsyncEventArgs.Completed(sender as Socket, serverAsyncEventArgs);
		}

		[Conditional("DEBUG")]
		internal void ValidateBufferSettings()
		{
			if (Offset < segment.Offset)
			{
				throw new ArgumentOutOfRangeException("Offset is below than segment.Offset value");
			}
			if (OffsetOffset >= segment.Count)
			{
				throw new ArgumentOutOfRangeException("OffsetOffset is bigger than segment.Count");
			}
			if (BytesTransferred >= segment.Count)
			{
				throw new ArgumentOutOfRangeException("BytesTransferred is bigger than segment.Count");
			}
			if (OffsetOffset + Count > segment.Count)
			{
				throw new ArgumentOutOfRangeException("Invalid buffer settings: OffsetOffset + Count is bigger than segment.Count");
			}
		}

		[Conditional("EVENTARGS_TRACING")]
		public void Trace()
		{
		}

		[Conditional("EVENTARGS_TRACING")]
		public void ResetTracing()
		{
		}

		public string GetTracingPath()
		{
			return "NO TRACING";
		}
	}
}
