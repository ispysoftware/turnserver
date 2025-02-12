using System;
using System.Threading;

namespace SocketServers
{
	public class LockFreePool<T> : ILockFreePool<T>, IDisposable where T : class, ILockFreePoolItem, IDisposable, new()
	{
		private LockFreeItem<T>[] array;

		private LockFreeStack<T> empty;

		private LockFreeStack<T> full;

		private int created;

		public int Queued => full.Length;

		public int Created => created;

		public LockFreePool(int size)
		{
			array = new LockFreeItem<T>[size];
			full = new LockFreeStack<T>(array, -1, -1);
			empty = new LockFreeStack<T>(array, 0, array.Length);
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
				empty.Push(num);
			}
			else
			{
				val = new T();
				val.SetDefaultValue();
				Interlocked.Increment(ref created);
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
			int num = empty.Pop();
			if (num >= 0)
			{
				value.SetDefaultValue();
				array[num].Value = value;
				full.Push(num);
			}
			else
			{
				value.Dispose();
			}
		}
	}
}
