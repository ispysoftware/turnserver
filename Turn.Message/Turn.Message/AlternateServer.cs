namespace Turn.Message
{
	public class AlternateServer : AddressAttribute
	{
		public AlternateServer()
		{
			base.AttributeType = AttributeType.AlternateServer;
		}
	}
}
