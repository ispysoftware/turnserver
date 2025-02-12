namespace Turn.Message
{
	public class ChangedAddress : AddressAttribute
	{
		public ChangedAddress()
		{
			base.AttributeType = AttributeType.ChangedAddress;
		}
	}
}
