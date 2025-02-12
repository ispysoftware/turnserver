namespace Turn.Message
{
	public class MsVersion : UInt32Attribute
	{
		public MsVersion()
		{
			base.AttributeType = AttributeType.MsVersion;
		}
	}
}
