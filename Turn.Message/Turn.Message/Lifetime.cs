namespace Turn.Message
{
	public class Lifetime : UInt32Attribute
	{
		public Lifetime()
		{
			base.AttributeType = AttributeType.Lifetime;
		}
	}
}
