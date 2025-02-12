namespace Turn.Message
{
	public class ReflectedFrom : AddressAttribute
	{
		public ReflectedFrom()
		{
			base.AttributeType = AttributeType.ReflectedFrom;
		}
	}
}
