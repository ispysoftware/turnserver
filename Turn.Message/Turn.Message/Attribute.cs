using System;

namespace Turn.Message
{
	public abstract class Attribute
	{
		public const ushort AttributeHeaderLength = 4;

		public AttributeType AttributeType
		{
			get;
			protected set;
		}

		public virtual ushort ValueLength
		{
			get;
			protected set;
		}

		public ushort TotalLength => (ushort)(ValueLength + 4);

		public bool Ignore
		{
			get;
			set;
		}

		public Attribute()
		{
			Ignore = false;
		}

		public virtual void GetBytes(byte[] bytes, ref int startIndex)
		{
			CopyBytes(bytes, ref startIndex, ((ushort)AttributeType).GetBigendianBytes());
			CopyBytes(bytes, ref startIndex, ValueLength.GetBigendianBytes());
		}

		public abstract void Parse(byte[] bytes, ref int startIndex);

		public static void Skip(byte[] bytes, ref int startIndex)
		{
			ushort num = ParseHeader(bytes, ref startIndex);
			startIndex += num;
		}

		protected static void CopyBytes(byte[] target, ref int startIndex, byte[] source)
		{
			CopyBytes(target, ref startIndex, source, 0, source.Length);
		}

		protected static void CopyBytes(byte[] target, ref int startIndex, byte[] source, int offset, int length)
		{
			Array.Copy(source, offset, target, startIndex, length);
			startIndex += source.Length;
		}

		protected static ushort ParseHeader(byte[] bytes, ref int startIndex)
		{
			startIndex += 2;
			return bytes.BigendianToUInt16(ref startIndex);
		}

		protected void ParseValidateHeader(byte[] bytes, ref int startIndex)
		{
			if (ParseHeader(bytes, ref startIndex) != ValueLength)
			{
				throw new TurnMessageException(ErrorCode.BadRequest, "Invalid attribute length - " + AttributeType.ToString());
			}
		}
	}
}
