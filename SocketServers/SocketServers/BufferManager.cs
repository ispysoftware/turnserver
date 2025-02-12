using System;

namespace SocketServers
{
	public class BufferManager
	{
		private static SmartBufferPool pool;

		public static long MaxMemoryUsage => pool.MaxMemoryUsage;

		public static int MaxSize => 262144;

		public static void Initialize(int maxMemoryUsageMb, int initialSizeMb, int extraBufferSizeMb)
		{
			pool = new SmartBufferPool(maxMemoryUsageMb, initialSizeMb, extraBufferSizeMb);
		}

		public static void Initialize(int maxMemoryUsageMb)
		{
			pool = new SmartBufferPool(maxMemoryUsageMb, maxMemoryUsageMb / 8, maxMemoryUsageMb / 16);
		}

		public static bool IsInitialized()
		{
			return pool != null;
		}

		public static ArraySegment<byte> Allocate(int size)
		{
			return pool.Allocate(size);
		}

		public static void Free(ref ArraySegment<byte> segment)
		{
			if (segment.IsValid())
			{
				pool.Free(segment);
				segment = default(ArraySegment<byte>);
			}
		}

		internal static void Free(ArraySegment<byte> segment)
		{
			if (segment.IsValid())
			{
				pool.Free(segment);
			}
		}
	}
}
