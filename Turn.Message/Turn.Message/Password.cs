namespace Turn.Message
{
	public class Password : StringAttribute
	{
		public Password()
		{
			base.AttributeType = AttributeType.Password;
		}
	}
}
