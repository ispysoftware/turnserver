using System;

namespace Turn.Message
{
	public class MessageIntegrity : Attribute
	{
		public byte[] Value
		{
			get;
			set;
		}

		public MessageIntegrity()
		{
			base.AttributeType = AttributeType.MessageIntegrity;
			ValueLength = 20;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			Attribute.CopyBytes(bytes, ref startIndex, Value);
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			ParseValidateHeader(bytes, ref startIndex);
			Value = new byte[ValueLength];
			Array.Copy(bytes, startIndex, Value, 0, ValueLength);
			startIndex += ValueLength;
		}
	}
}
