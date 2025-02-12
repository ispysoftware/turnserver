using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.Ssp
{
	[SuppressUnmanagedCodeSecurity]
	public class SafeContextBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeContextBufferHandle()
			: base(ownsHandle: true)
		{
		}

		public SafeContextBufferHandle(IntPtr handle)
			: base(ownsHandle: true)
		{
			SetHandle(handle);
		}

		protected override bool ReleaseHandle()
		{
			return Secur32Dll.FreeContextBuffer(handle) == 0;
		}

		public SecPkgInfo GetItem<T>(int index)
		{
			IntPtr ptr = (IntPtr)(DangerousGetHandle().ToInt64() + Marshal.SizeOf(typeof(T)) * index);
			return (SecPkgInfo)Marshal.PtrToStructure(ptr, typeof(T));
		}
	}
}
