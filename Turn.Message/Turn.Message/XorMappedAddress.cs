using System;

namespace Turn.Message
{
	public class XorMappedAddress : AddressAttribute
	{
		public XorMappedAddress(TurnMessageRfc rfc)
		{
			if (rfc == TurnMessageRfc.MsTurn)
			{
				base.AttributeType = AttributeType.XorMappedAddress;
			}
			else
			{
				base.AttributeType = AttributeType.XorMappedAddressStun;
			}
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			throw new NotSupportedException();
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			throw new NotSupportedException();
		}

		public void GetBytes(byte[] bytes, ref int startIndex, TransactionId transactionId)
		{
			GetBytes(bytes, ref startIndex, transactionId.Value);
		}

		public void Parse(byte[] bytes, ref int startIndex, TransactionId transactionId)
		{
			Parse(bytes, ref startIndex, transactionId.Value);
		}
	}
}
