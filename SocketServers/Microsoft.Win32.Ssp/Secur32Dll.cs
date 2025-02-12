using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.Ssp
{
	[SuppressUnmanagedCodeSecurity]
	internal static class Secur32Dll
	{
		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static extern int FreeContextBuffer([In] IntPtr pvContextBuffer);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static extern int FreeCredentialsHandle([In] ref CredHandle phCredential);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int EnumerateSecurityPackagesA(out int pcPackages, out SafeContextBufferHandle ppPackageInfo);

		[DllImport("secur32.dll", BestFitMapping = false, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
		public unsafe static extern int AcquireCredentialsHandleA([In] [MarshalAs(UnmanagedType.LPStr)] string pszPrincipal, [In] [MarshalAs(UnmanagedType.LPStr)] string pszPackage, [In] int fCredentialUse, [In] void* pvLogonID, [In] void* pAuthData, [In] void* pGetKeyFn, [In] void* pvGetKeyArgument, out CredHandle phCredential, out long ptsExpiry);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public unsafe static extern int AcceptSecurityContext([In] ref CredHandle phCredential, [In] [Out] void* phContext, [In] ref SecBufferDesc pInput, [In] int fContextReq, [In] int TargetDataRep, [In] [Out] ref CtxtHandle phNewContext, [In] [Out] ref SecBufferDesc pOutput, out int pfContextAttr, out long ptsTimeStamp);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static extern int DeleteSecurityContext([In] ref CtxtHandle phContext);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public unsafe static extern int QueryContextAttributesA([In] ref CtxtHandle phContext, [In] uint ulAttribute, [Out] void* pBuffer);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public unsafe static extern int DecryptMessage([In] ref CtxtHandle phContext, [In] [Out] ref SecBufferDesc pMessage, [In] uint MessageSeqNo, [Out] void* pfQOP);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public unsafe static extern int EncryptMessage([In] ref CtxtHandle phContext, [Out] void* pfQOP, [In] [Out] ref SecBufferDesc pMessage, [In] uint MessageSeqNo);
	}
}
