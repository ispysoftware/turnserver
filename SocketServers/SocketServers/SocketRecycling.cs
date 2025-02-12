using System;
using System.Net.Sockets;

namespace SocketServers
{
	public class SocketRecycling : IDisposable
	{
		private LockFreeItem<Socket>[] array;

		private LockFreeStack<Socket> empty;

		private LockFreeStack<Socket> full4;

		private LockFreeStack<Socket> full6;

		private bool isEnabled;

		public bool IsEnabled => isEnabled;

		public int RecyclingCount
		{
			get
			{
				if (!isEnabled)
				{
					return 0;
				}
				return full4.Length + full6.Length;
			}
		}

		public SocketRecycling(int maxSocket)
		{
			if (maxSocket > 0)
			{
				isEnabled = true;
				array = new LockFreeItem<Socket>[maxSocket];
				empty = new LockFreeStack<Socket>(array, 0, maxSocket);
				full4 = new LockFreeStack<Socket>(array, -1, -1);
				full6 = new LockFreeStack<Socket>(array, -1, -1);
			}
		}

		public void Dispose()
		{
			if (isEnabled)
			{
				isEnabled = false;
				int num;
				while ((num = full4.Pop()) >= 0)
				{
					array[num].Value.Close();
					empty.Push(num);
				}
				while ((num = full6.Pop()) >= 0)
				{
					array[num].Value.Close();
					empty.Push(num);
				}
			}
		}

		public Socket Get(AddressFamily family)
		{
			if (isEnabled)
			{
				int num = GetFull(family).Pop();
				if (num >= 0)
				{
					Socket value = array[num].Value;
					array[num].Value = null;
					empty.Push(num);
					return value;
				}
			}
			return null;
		}

		public bool Recycle(Socket socket, AddressFamily family)
		{
			if (isEnabled)
			{
				int num = empty.Pop();
				if (num >= 0)
				{
					array[num].Value = socket;
					GetFull(family).Push(num);
					return true;
				}
			}
			return false;
		}

		private LockFreeStack<Socket> GetFull(AddressFamily family)
		{
			switch (family)
			{
			case AddressFamily.InterNetwork:
				return full4;
			case AddressFamily.InterNetworkV6:
				return full6;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}
