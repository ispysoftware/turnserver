using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Win32.Ssp
{
	public struct SchannelCred
	{
		[Flags]
		public enum Flags
		{
			NoDefaultCred = 0x10,
			NoNameCheck = 0x4,
			NoSystemMapper = 0x2,
			ValidateAuto = 0x20,
			ValidateManual = 0x8,
			Zero = 0x0
		}

		public const int CurrentVersion = 4;

		public int version;

		public int cCreds;

		public IntPtr paCreds1;

		private readonly IntPtr rootStore;

		public int cMappers;

		private readonly IntPtr phMappers;

		public int cSupportedAlgs;

		private readonly IntPtr palgSupportedAlgs;

		public SchProtocols grbitEnabledProtocols;

		public int dwMinimumCipherStrength;

		public int dwMaximumCipherStrength;

		public int dwSessionLifespan;

		public Flags dwFlags;

		public int reserved;

		public SchannelCred(X509Certificate certificate, SchProtocols protocols)
		{
			this = new SchannelCred(4, certificate, Flags.Zero, protocols);
		}

		public SchannelCred(int version1, X509Certificate certificate, Flags flags, SchProtocols protocols)
		{
			paCreds1 = IntPtr.Zero;
			rootStore = (phMappers = (palgSupportedAlgs = IntPtr.Zero));
			cCreds = (cMappers = (cSupportedAlgs = 0));
			dwMinimumCipherStrength = (dwMaximumCipherStrength = 0);
			dwSessionLifespan = (reserved = 0);
			version = version1;
			dwFlags = flags;
			grbitEnabledProtocols = protocols;
			if (certificate != null)
			{
				paCreds1 = certificate.Handle;
				cCreds = 1;
			}
		}
	}
}
