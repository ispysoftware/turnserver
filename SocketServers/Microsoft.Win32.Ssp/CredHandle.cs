using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.Ssp
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CredHandle
	{
		private IntPtr dwLower;

		private IntPtr dwUpper;

		public bool IsInvalid
		{
			get
			{
				if (dwLower == IntPtr.Zero)
				{
					return dwUpper == IntPtr.Zero;
				}
				return false;
			}
		}
	}
}
