using System;

namespace Turn.Message
{
	public class UnknownAttributes : Attribute
	{
		public override ushort ValueLength
		{
			get
			{
				return (ushort)(Values.Length * 2);
			}
			protected set
			{
				throw new InvalidOperationException();
			}
		}

		public AttributeType[] Values
		{
			get;
			set;
		}

		public UnknownAttributes()
		{
			base.AttributeType = AttributeType.UnknownAttributes;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			AttributeType[] values = Values;
			foreach (AttributeType attributeType in values)
			{
				Attribute.CopyBytes(bytes, ref startIndex, ((ushort)attributeType).GetBigendianBytes());
			}
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			throw new NotImplementedException();
		}
	}
}
