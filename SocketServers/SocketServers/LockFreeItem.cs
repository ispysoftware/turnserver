namespace SocketServers
{
	internal struct LockFreeItem<T>
	{
		public long Next;

		public T Value;

		public new string ToString()
		{
			return string.Format("Next: {0}, Count: {1}, Value: {2}", (int)Next, (uint)(Next >> 32), (Value == null) ? "null" : "full");
		}
	}
}
