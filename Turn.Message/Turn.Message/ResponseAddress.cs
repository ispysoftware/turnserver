namespace Turn.Message
{
	public class ResponseAddress : AddressAttribute
	{
		public ResponseAddress()
		{
			base.AttributeType = AttributeType.ResponseAddress;
		}
	}
}
