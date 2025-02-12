using System;

namespace Turn.Message
{
	public abstract class RawData : Attribute
	{
		private bool copyValue;

		public byte[] Value
		{
			get
			{
				return ValueRef;
			}
			set
			{
				ValueRef = value;
				ValueRefOffset = 0;
				ValueRefLength = ((ValueRef != null) ? ValueRef.Length : 0);
			}
		}

		public byte[] ValueRef
		{
			get;
			set;
		}

		public int ValueRefOffset
		{
			get;
			set;
		}

		public int ValueRefLength
		{
			get
			{
				return ValueLength;
			}
			set
			{
				ValueLength = (ushort)value;
			}
		}

		public RawData(bool copyValue1)
		{
			copyValue = copyValue1;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			Attribute.CopyBytes(bytes, ref startIndex, ValueRef, ValueRefOffset, ValueRefLength);
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			int num = Attribute.ParseHeader(bytes, ref startIndex);
			if (copyValue)
			{
				Value = new byte[num];
				Array.Copy(bytes, startIndex, Value, 0, num);
			}
			else
			{
				Value = null;
				ValueRef = bytes;
				ValueRefOffset = startIndex;
				ValueRefLength = num;
			}
			startIndex += num;
		}
	}
}
