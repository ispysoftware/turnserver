using System;
using System.Threading;

namespace SocketServers
{
	public class SpinLock
	{
		private const int LockStateFree = 0;

		private const int LockStateOwned = 1;

		private int _lockState;

		private static readonly bool _isSingleCpuMachine = Environment.ProcessorCount == 1;

		public bool IsLockHeld => _lockState == 1;

		private static void StallThread()
		{
			if (_isSingleCpuMachine)
			{
				Thread.Sleep(0);
			}
			else
			{
				Thread.SpinWait(1);
			}
		}

		public void Enter()
		{
			Thread.BeginCriticalRegion();
			while (Interlocked.Exchange(ref _lockState, 1) != 0)
			{
				while (Thread.VolatileRead(ref _lockState) == 1)
				{
					StallThread();
				}
			}
		}

		public void Exit()
		{
			Interlocked.Exchange(ref _lockState, 0);
			Thread.EndCriticalRegion();
		}
	}
}
