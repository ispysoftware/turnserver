using Microsoft.Win32.Ssp;
using System;

namespace SocketServers
{
	public class StreamBuffer : IDisposable
	{
		private ArraySegment<byte> segment;

		public byte[] Array => segment.Array;

		public int Offset => segment.Offset;

		public int Count
		{
			get;
			private set;
		}

		public int Capacity
		{
			get;
			private set;
		}

		public int FreeSize => Capacity - Count;

		public int BytesTransferred => Count;

		public bool IsValid
		{
			get
			{
				if (segment.Array != null && segment.Offset >= 0)
				{
					return segment.Count > 0;
				}
				return false;
			}
		}

		public bool IsInvalid
		{
			get
			{
				if (segment.Array != null && segment.Offset >= 0)
				{
					return segment.Count <= 0;
				}
				return true;
			}
		}

		public void AddCount(int offset)
		{
			Count += offset;
		}

		public bool Resize(int capacity)
		{
			if (capacity > BufferManager.MaxSize)
			{
				return false;
			}
			if (capacity < Count)
			{
				return false;
			}
			if (Capacity != capacity)
			{
				Capacity = capacity;
				if (capacity > segment.Count)
				{
					ArraySegment<byte> arraySegment = segment;
					segment = BufferManager.Allocate(capacity);
					if (arraySegment.IsValid())
					{
						if (Count > 0)
						{
							Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset, segment.Array, segment.Offset, Count);
						}
						BufferManager.Free(ref arraySegment);
					}
				}
			}
			return true;
		}

		public void Free()
		{
			Count = 0;
			BufferManager.Free(ref segment);
		}

		public void Clear()
		{
			Count = 0;
		}

		public void Dispose()
		{
			Free();
		}

		public bool CopyTransferredFrom(ServerAsyncEventArgs e, int skipBytes)
		{
			return CopyFrom(e.Buffer, e.Offset + skipBytes, e.BytesTransferred - skipBytes);
		}

		public bool CopyFrom(ArraySegment<byte> segment1, int skipBytes)
		{
			return CopyFrom(segment1.Array, segment1.Offset + skipBytes, segment1.Count - skipBytes);
		}

		public bool CopyFrom(ArraySegment<byte> segmnet)
		{
			return CopyFrom(segmnet.Array, segmnet.Offset, segmnet.Count);
		}

		internal bool CopyFrom(SecBufferEx secBuffer)
		{
			return CopyFrom(secBuffer.Buffer as byte[], secBuffer.Offset, secBuffer.Size);
		}

		public bool CopyFrom(byte[] array, int offset, int count)
		{
			if (count > Capacity - Count)
			{
				return false;
			}
			if (count == 0)
			{
				return true;
			}
			Create();
			Buffer.BlockCopy(array, offset, segment.Array, segment.Offset + Count, count);
			Count += count;
			return true;
		}

		public void MoveToBegin(int offsetOffset)
		{
			MoveToBegin(offsetOffset, Count - offsetOffset);
		}

		public void MoveToBegin(int offsetOffset, int count)
		{
			Buffer.BlockCopy(segment.Array, segment.Offset + offsetOffset, segment.Array, segment.Offset, count);
			Count = count;
		}

		public ArraySegment<byte> Detach()
		{
			ArraySegment<byte> result = segment;
			segment = default(ArraySegment<byte>);
			Count = 0;
			return result;
		}

		private void Create()
		{
			if (segment.IsInvalid())
			{
				Count = 0;
				segment = BufferManager.Allocate(Capacity);
			}
		}
	}
}
