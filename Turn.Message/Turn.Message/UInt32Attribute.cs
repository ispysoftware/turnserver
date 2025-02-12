using System;

namespace Turn.Message
{
	public abstract class UInt32Attribute : Attribute
	{
		public uint Value
		{
			get;
			set;
		}

		public UInt32Attribute()
		{
			ValueLength = 4;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			Attribute.CopyBytes(bytes, ref startIndex, Value.GetBigendianBytes());
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			ParseValidateHeader(bytes, ref startIndex);
			Value = bytes.BigendianToUInt32(ref startIndex);
		}
	}
}
