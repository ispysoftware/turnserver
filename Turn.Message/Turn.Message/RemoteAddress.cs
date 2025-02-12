namespace Turn.Message
{
	public class RemoteAddress : AddressAttribute
	{
		public RemoteAddress()
		{
			base.AttributeType = AttributeType.RemoteAddress;
		}
	}
}
