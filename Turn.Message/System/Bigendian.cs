namespace System
{
	public static class Bigendian
	{
		public static ushort BigendianToUInt16(this byte[] bytes, int startIndex)
		{
			ushort num = 0;
			num = (ushort)(num | bytes[startIndex]);
			num = (ushort)(num << 8);
			return (ushort)(num | bytes[startIndex + 1]);
		}

		public static uint BigendianToUInt32(this byte[] bytes, int startIndex)
		{
			uint num = 0u;
			num |= bytes[startIndex];
			num <<= 8;
			num |= bytes[startIndex + 1];
			num <<= 8;
			num |= bytes[startIndex + 2];
			num <<= 8;
			return num | bytes[startIndex + 3];
		}

		public static ushort BigendianToUInt16(this byte[] bytes, ref int startIndex)
		{
			ushort result = bytes.BigendianToUInt16(startIndex);
			startIndex += 2;
			return result;
		}

		public static uint BigendianToUInt32(this byte[] bytes, ref int startIndex)
		{
			uint result = bytes.BigendianToUInt32(startIndex);
			startIndex += 4;
			return result;
		}

		private static byte[] Correct(byte[] data)
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(data);
			}
			return data;
		}

		public static byte[] GetBigendianBytes(this uint value)
		{
			return Correct(BitConverter.GetBytes(value));
		}

		public static byte[] GetBigendianBytes(this ushort value)
		{
			return Correct(BitConverter.GetBytes(value));
		}
	}
}
