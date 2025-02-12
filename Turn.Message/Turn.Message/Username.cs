namespace Turn.Message
{
	public class Username : StringAttribute
	{
		public Username()
		{
			base.AttributeType = AttributeType.Username;
		}
	}
}
