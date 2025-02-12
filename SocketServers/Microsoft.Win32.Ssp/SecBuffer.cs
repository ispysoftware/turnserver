using System;

namespace Microsoft.Win32.Ssp
{
	internal struct SecBuffer
	{
		internal int cbBuffer;

		internal int BufferType;

		internal IntPtr pvBuffer;

		public SecBuffer(BufferType type, int count, IntPtr buffer)
		{
			BufferType = (int)type;
			cbBuffer = count;
			pvBuffer = buffer;
		}
	}
}
