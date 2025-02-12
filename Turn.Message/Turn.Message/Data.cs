namespace Turn.Message
{
	public class Data : RawData
	{
		public Data()
			: base(copyValue1: false)
		{
			base.AttributeType = AttributeType.Data;
		}
	}
}
