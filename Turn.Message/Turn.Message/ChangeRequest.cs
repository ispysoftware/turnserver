namespace Turn.Message
{
	public class ChangeRequest : Attribute
	{
		public bool ChangeIp
		{
			get;
			set;
		}

		public bool ChangePort
		{
			get;
			set;
		}

		public ChangeRequest()
		{
			base.AttributeType = AttributeType.ChangeRequest;
			ValueLength = 4;
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			ParseValidateHeader(bytes, ref startIndex);
			ChangeIp = ((bytes[startIndex + 3] & 4) != 0);
			ChangePort = ((bytes[startIndex + 3] & 2) != 0);
			startIndex += 4;
		}
	}
}
