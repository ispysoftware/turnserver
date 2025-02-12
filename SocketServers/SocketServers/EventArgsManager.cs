namespace SocketServers
{
	public class EventArgsManager
	{
		private static ILockFreePool<ServerAsyncEventArgs> pool;

		public static int Queued => pool.Queued;

		public static int Created => pool.Created;

		internal static void Initialize()
		{
			pool = new LockFreePool<ServerAsyncEventArgs>((int)(BufferManager.MaxMemoryUsage / 2048));
		}

		internal static bool IsInitialized()
		{
			return pool != null;
		}

		public static ServerAsyncEventArgs Get()
		{
			return pool.Get();
		}

		public static void Put(ref ServerAsyncEventArgs value)
		{
			pool.Put(ref value);
		}

		public static void Put(ServerAsyncEventArgs value)
		{
			pool.Put(value);
		}
	}
}
