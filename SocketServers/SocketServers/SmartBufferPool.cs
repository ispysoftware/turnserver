using System;
using System.Threading;

namespace SocketServers
{
	public class SmartBufferPool
	{
		public const long Kb = 1024L;

		public const long Mb = 1048576L;

		public const long Gb = 1073741824L;

		public const int MinSize = 1024;

		public const int MaxSize = 262144;

		public readonly long MaxMemoryUsage;

		public readonly long InitialMemoryUsage;

		public readonly long ExtraMemoryUsage;

		public readonly long MaxBuffersCount;

		private byte[][] buffers;

		private long indexOffset;

		private LockFreeItem<long>[] array;

		private LockFreeStack<long> empty;

		private LockFreeStack<long>[] ready;

		public SmartBufferPool(int maxMemoryUsageMb, int initialSizeMb, int extraBufferSizeMb)
		{
			InitialMemoryUsage = (long)initialSizeMb * 1048576L;
			ExtraMemoryUsage = (long)extraBufferSizeMb * 1048576L;
			MaxBuffersCount = ((long)maxMemoryUsageMb * 1048576L - InitialMemoryUsage) / ExtraMemoryUsage;
			MaxMemoryUsage = InitialMemoryUsage + ExtraMemoryUsage * MaxBuffersCount;
			array = new LockFreeItem<long>[MaxMemoryUsage / 1024];
			empty = new LockFreeStack<long>(array, 0, array.Length);
			int i;
			for (i = 0; 262144 >> i >= 1024; i++)
			{
			}
			ready = new LockFreeStack<long>[i];
			for (int j = 0; j < ready.Length; j++)
			{
				ready[j] = new LockFreeStack<long>(array, -1, -1);
			}
			buffers = new byte[MaxBuffersCount][];
			buffers[0] = NewBuffer(InitialMemoryUsage);
		}

		public ArraySegment<byte> Allocate(int size)
		{
			if (size > 262144)
			{
				throw new ArgumentOutOfRangeException("Too large size");
			}
			size = 1024 << GetBitOffset(size);
			if (!GetAllocated(size, out int index, out int offset))
			{
				long num;
				do
				{
					num = Interlocked.Read(ref indexOffset);
					offset = (int)num;
					index = (int)(num >> 32);
					while (buffers[index] == null)
					{
						Thread.Sleep(0);
					}
					if (buffers[index].Length - offset < size)
					{
						if (index + 1 >= buffers.Length)
						{
							throw new OutOfMemoryException("Source: BufferManager");
						}
						if (Interlocked.CompareExchange(ref indexOffset, (long)(index + 1) << 32, num) == num)
						{
							buffers[index + 1] = NewBuffer(ExtraMemoryUsage);
						}
					}
				}
				while (Interlocked.CompareExchange(ref indexOffset, num + size, num) != num);
			}
			return new ArraySegment<byte>(buffers[index], offset, size);
		}

		public void Free(ArraySegment<byte> segment)
		{
			int i;
			for (i = 0; i < buffers.Length && buffers[i] != segment.Array; i++)
			{
			}
			if (i >= buffers.Length)
			{
				throw new ArgumentException("SmartBufferPool.Free, segment.Array is invalid");
			}
			int num = empty.Pop();
			array[num].Value = ((long)i << 32) + segment.Offset;
			ready[GetBitOffset(segment.Count)].Push(num);
		}

		private bool GetAllocated(int size, out int index, out int offset)
		{
			int num = ready[GetBitOffset(size)].Pop();
			if (num >= 0)
			{
				index = (int)(array[num].Value >> 32);
				offset = (int)array[num].Value;
				empty.Push(num);
				return true;
			}
			index = -1;
			offset = -1;
			return false;
		}

		private int GetBitOffset(int size)
		{
			int i;
			for (i = 0; size >> i > 1024; i++)
			{
			}
			return i;
		}

		private static byte[] NewBuffer(long size)
		{
			return new byte[size];
		}
	}
}
