namespace Turn.Message
{
	public class MagicCookie : UInt32Attribute
	{
		public const uint MagicCookieValue = 1925598150u;

		public MagicCookie()
		{
			base.AttributeType = AttributeType.MagicCookie;
			base.Value = 1925598150u;
		}
	}
}
