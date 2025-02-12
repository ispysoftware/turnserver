namespace Turn.Message
{
	public class Realm : StringAttribute
	{
		public Realm(TurnMessageRfc rfc)
		{
			if (rfc == TurnMessageRfc.MsTurn)
			{
				base.AttributeType = AttributeType.Realm;
			}
			else
			{
				base.AttributeType = AttributeType.Nonce;
			}
		}
	}
}
