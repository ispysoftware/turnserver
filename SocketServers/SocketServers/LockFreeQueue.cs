using System;
using System.Threading;

namespace SocketServers
{
	internal class LockFreeQueue<T>
	{
		private LockFreeItem<T>[] array;

		private LockFreeQueueVars q;

		private Predicate<T> dequeuePredicate;

		public Predicate<T> DequeuePredicate
		{
			get
			{
				return dequeuePredicate;
			}
			set
			{
				dequeuePredicate = value;
				q.HasDequeuePredicate = (dequeuePredicate != null);
			}
		}

		public LockFreeQueue(LockFreeItem<T>[] array1, int enqueueFromDummy, int enqueueCount)
		{
			if (enqueueCount <= 0)
			{
				throw new ArgumentOutOfRangeException("enqueueCount", "Queue must include at least one dummy element");
			}
			array = array1;
			q.Head = enqueueFromDummy;
			q.Tail = enqueueFromDummy + enqueueCount - 1;
			for (int i = 0; i < enqueueCount - 1; i++)
			{
				array[i + enqueueFromDummy].Next = enqueueFromDummy + i + 1;
			}
			array[q.Tail].Next = 4294967295L;
		}

		public void Enqueue(int index)
		{
			array[index].Next |= 4294967295L;
			ulong num;
			ulong value;
			while (true)
			{
				num = (ulong)Interlocked.Read(ref q.Tail);
				ulong num2 = (ulong)Interlocked.Read(ref array[num & uint.MaxValue].Next);
				if (num != (ulong)q.Tail)
				{
					continue;
				}
				if ((num2 & uint.MaxValue) == uint.MaxValue)
				{
					value = (ulong)(((long)(num2 + 4294967296L) & -4294967296L) | (uint)index);
					ulong num3 = (ulong)Interlocked.CompareExchange(ref array[num & uint.MaxValue].Next, (long)value, (long)num2);
					if (num3 == num2)
					{
						break;
					}
				}
				else
				{
					value = (ulong)(((long)(num + 4294967296L) & -4294967296L) | (long)(num2 & uint.MaxValue));
					Interlocked.CompareExchange(ref q.Tail, (long)value, (long)num);
				}
			}
			value = (ulong)(((long)(num + 4294967296L) & -4294967296L) | (uint)index);
			Interlocked.CompareExchange(ref q.Tail, (long)value, (long)num);
		}

		public int Dequeue()
		{
			ulong num;
			T value2;
			while (true)
			{
				num = (ulong)Interlocked.Read(ref q.Head);
				ulong num2 = (ulong)Interlocked.Read(ref q.Tail);
				ulong num3 = (ulong)Interlocked.Read(ref array[num & uint.MaxValue].Next);
				if (num != (ulong)q.Head)
				{
					continue;
				}
				ulong value;
				if ((num & uint.MaxValue) == (num2 & uint.MaxValue))
				{
					if ((num3 & uint.MaxValue) == uint.MaxValue)
					{
						return -1;
					}
					value = (ulong)(((long)(num2 + 4294967296L) & -4294967296L) | (long)(num3 & uint.MaxValue));
					Interlocked.CompareExchange(ref q.Tail, (long)value, (long)num2);
					continue;
				}
				value2 = array[num3 & uint.MaxValue].Value;
				if (q.HasDequeuePredicate && !DequeuePredicate(value2))
				{
					return -1;
				}
				value = (ulong)(((long)(num + 4294967296L) & -4294967296L) | (long)(num3 & uint.MaxValue));
				ulong num4 = (ulong)Interlocked.CompareExchange(ref q.Head, (long)value, (long)num);
				if (num4 == num)
				{
					break;
				}
			}
			int num5 = (int)(num & uint.MaxValue);
			array[num5].Value = value2;
			return num5;
		}
	}
}
