using System.ComponentModel;

namespace Microsoft.Win32.Ssp
{
	public class SspiException : Win32Exception
	{
		public SecurityStatus SecurityStatus => Sspi.Convert(base.ErrorCode);

		public SspiException(int error, string function)
			: base(error, $"SSPI error, function call {function} return 0x{error:x8}")
		{
		}
	}
}
