using System;
using System.Threading;

namespace SocketServers
{
	public class LockFreeFastPool<T> : ILockFreePool<T>, IDisposable where T : class, ILockFreePoolItem, ILockFreePoolItemIndex, IDisposable, new()
	{
		private LockFreeItem<T>[] array;

		private LockFreeStack<T> full;

		private int created;

		public int Queued => full.Length;

		public int Created => created;

		public LockFreeFastPool(int size)
		{
			array = new LockFreeItem<T>[size];
			full = new LockFreeStack<T>(array, -1, -1);
		}

		public void Dispose()
		{
			while (true)
			{
				int num = full.Pop();
				if (num < 0)
				{
					break;
				}
				array[num].Value.Dispose();
				array[num].Value = null;
			}
		}

		public T Get()
		{
			T val = null;
			int num = full.Pop();
			if (num >= 0)
			{
				val = array[num].Value;
				array[num].Value = null;
			}
			else
			{
				val = new T();
				val.SetDefaultValue();
				val.Index = -1;
				if (created < array.Length)
				{
					int num2 = Interlocked.Increment(ref created) - 1;
					if (num2 < array.Length)
					{
						val.Index = num2;
					}
				}
			}
			val.IsPooled = false;
			return val;
		}

		public T GetIfSpaceAvailable()
		{
			T val = null;
			int num = full.Pop();
			if (num >= 0)
			{
				val = array[num].Value;
				array[num].Value = null;
			}
			else
			{
				if (created >= array.Length)
				{
					return null;
				}
				int num2 = Interlocked.Increment(ref created) - 1;
				if (num2 >= array.Length)
				{
					return null;
				}
				val = new T();
				val.SetDefaultValue();
				val.Index = num2;
			}
			val.IsPooled = false;
			return val;
		}

		public void Put(ref T value)
		{
			Put(value);
			value = null;
		}

		public void Put(T value)
		{
			value.IsPooled = true;
			int index = value.Index;
			if (index >= 0)
			{
				value.SetDefaultValue();
				array[index].Value = value;
				full.Push(index);
			}
			else
			{
				value.Dispose();
			}
		}
	}
}
