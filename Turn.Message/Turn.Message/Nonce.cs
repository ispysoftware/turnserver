namespace Turn.Message
{
	public class Nonce : StringAttribute
	{
		public Nonce(TurnMessageRfc rfc)
		{
			if (rfc == TurnMessageRfc.MsTurn)
			{
				base.AttributeType = AttributeType.Nonce;
			}
			else
			{
				base.AttributeType = AttributeType.Realm;
			}
		}
	}
}
