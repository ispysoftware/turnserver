using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.Ssp
{
	[SuppressUnmanagedCodeSecurity]
	public class SafeCredHandle : SafeHandle
	{
		internal CredHandle Handle;

		public override bool IsInvalid => Handle.IsInvalid;

		public SafeCredHandle(CredHandle credHandle)
			: base(IntPtr.Zero, ownsHandle: true)
		{
			Handle = credHandle;
		}

		protected override bool ReleaseHandle()
		{
			return Secur32Dll.FreeCredentialsHandle(ref Handle) == 0;
		}
	}
}
