namespace Turn.Message
{
	public class DestinationAddress : AddressAttribute
	{
		public DestinationAddress()
		{
			base.AttributeType = AttributeType.DestinationAddress;
		}
	}
}
