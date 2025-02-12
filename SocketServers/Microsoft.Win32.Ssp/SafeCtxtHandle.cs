using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.Ssp
{
	[SuppressUnmanagedCodeSecurity]
	public class SafeCtxtHandle : SafeHandle
	{
		internal CtxtHandle Handle;

		public override bool IsInvalid => Handle.IsInvalid;

		public SafeCtxtHandle()
			: base(IntPtr.Zero, ownsHandle: true)
		{
		}

		public SafeCtxtHandle(CtxtHandle ctxtHandle)
			: base(IntPtr.Zero, ownsHandle: true)
		{
			Handle = ctxtHandle;
		}

		protected override bool ReleaseHandle()
		{
			return Secur32Dll.DeleteSecurityContext(ref Handle) == 0;
		}
	}
}
