using System.Threading;

namespace SocketServers
{
	internal class LockFreeStack<T>
	{
		private LockFreeStackVars s;

		private LockFreeItem<T>[] array;

		public int Length
		{
			get
			{
				int num = 0;
				for (int num2 = (int)s.Head; num2 >= 0; num2 = (int)array[num2].Next)
				{
					num++;
				}
				return num;
			}
		}

		public LockFreeStack(LockFreeItem<T>[] array1, int pushFrom, int pushCount)
		{
			array = array1;
			s.Head = pushFrom;
			for (int i = 0; i < pushCount - 1; i++)
			{
				array[i + pushFrom].Next = pushFrom + i + 1;
			}
			if (pushFrom >= 0)
			{
				array[pushFrom + pushCount - 1].Next = 4294967295L;
			}
		}

		public int Pop()
		{
			ulong num = (ulong)Interlocked.Read(ref s.Head);
			int num2;
			while (true)
			{
				num2 = (int)num;
				if (num2 < 0)
				{
					return -1;
				}
				ulong value = (ulong)((Thread.VolatileRead(ref array[num2].Next) & uint.MaxValue) | ((long)num & -4294967296L));
				ulong num3 = (ulong)Interlocked.CompareExchange(ref s.Head, (long)value, (long)num);
				if (num == num3)
				{
					break;
				}
				num = num3;
			}
			return num2;
		}

		public void Push(int index)
		{
			ulong num = (ulong)Interlocked.Read(ref s.Head);
			while (true)
			{
				array[index].Next = ((array[index].Next & -4294967296L) | (long)(num & uint.MaxValue));
				ulong value = (ulong)(((long)(num + 4294967296L) & -4294967296L) | (uint)index);
				ulong num2 = (ulong)Interlocked.CompareExchange(ref s.Head, (long)value, (long)num);
				if (num == num2)
				{
					break;
				}
				num = num2;
			}
		}
	}
}
