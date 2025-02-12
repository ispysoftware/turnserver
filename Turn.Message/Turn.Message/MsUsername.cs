namespace Turn.Message
{
	public class MsUsername : RawData
	{
		public const int HashOfTokenBlobLength = 20;

		public int TokenBlobLength => base.Value.Length - 20;

		public MsUsername()
			: base(copyValue1: true)
		{
			base.AttributeType = AttributeType.Username;
		}
	}
}
