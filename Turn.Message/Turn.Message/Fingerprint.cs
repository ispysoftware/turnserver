namespace Turn.Message
{
	public class Fingerprint : UInt32Attribute
	{
		public Fingerprint()
		{
			base.AttributeType = AttributeType.Fingerprint;
		}
	}
}
