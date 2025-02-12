namespace Microsoft.Win32.Ssp
{
	public struct SecBufferEx
	{
		public BufferType BufferType;

		public int Size;

		public int Offset;

		public object Buffer;

		public void SetBuffer(BufferType type, byte[] bytes)
		{
			BufferType = type;
			Buffer = bytes;
			Offset = 0;
			Size = bytes.Length;
		}

		public void SetBuffer(BufferType type, byte[] bytes, int offset, int size)
		{
			BufferType = type;
			Buffer = bytes;
			Offset = offset;
			Size = size;
		}

		public void SetBufferEmpty()
		{
			BufferType = BufferType.SECBUFFER_VERSION;
			Buffer = null;
			Offset = 0;
			Size = 0;
		}
	}
}
