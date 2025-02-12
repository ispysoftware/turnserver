using System;

namespace Turn.Message
{
	public class MsSequenceNumber : Attribute
	{
		public ConnectionId ConnectionId;

		public uint SequenceNumber
		{
			get;
			set;
		}

		public MsSequenceNumber()
		{
			base.AttributeType = AttributeType.MsSequenceNumber;
			ValueLength = 24;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			ConnectionId.GetBytes(bytes, ref startIndex);
			Attribute.CopyBytes(bytes, ref startIndex, SequenceNumber.GetBigendianBytes());
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			ParseValidateHeader(bytes, ref startIndex);
			ConnectionId.Parse(bytes, ref startIndex);
			SequenceNumber = bytes.BigendianToUInt32(ref startIndex);
		}
	}
}
