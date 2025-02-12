using System;

namespace Turn.Message
{
	public abstract class StringAttribute : UtfAttribute
	{
		public override ushort ValueLength
		{
			get
			{
				return (ushort)base.Utf8Value.Length;
			}
			protected set
			{
				throw new InvalidOperationException();
			}
		}

		public string Value
		{
			get
			{
				return base.StringValue;
			}
			set
			{
				base.StringValue = value;
			}
		}

		public StringAttribute()
		{
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			Attribute.CopyBytes(bytes, ref startIndex, base.Utf8Value);
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			int length = Attribute.ParseHeader(bytes, ref startIndex);
			ParseUtf8String(bytes, ref startIndex, length);
		}
	}
}
