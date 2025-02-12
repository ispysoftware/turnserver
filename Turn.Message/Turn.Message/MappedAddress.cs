namespace Turn.Message
{
	public class MappedAddress : AddressAttribute
	{
		public MappedAddress()
		{
			base.AttributeType = AttributeType.MappedAddress;
		}
	}
}
