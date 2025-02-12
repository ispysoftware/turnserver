using System;

namespace Turn.Message
{
	public class ErrorCodeAttribute : UtfAttribute
	{
		public override ushort ValueLength
		{
			get
			{
				return (ushort)(base.Utf8Value.Length + 4);
			}
			protected set
			{
				throw new InvalidOperationException();
			}
		}

		public int ErrorCode
		{
			get;
			set;
		}

		public string ReasonPhrase
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

		public ErrorCodeAttribute()
		{
			base.AttributeType = AttributeType.ErrorCode;
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			base.GetBytes(bytes, ref startIndex);
			bytes[startIndex++] = 0;
			bytes[startIndex++] = 0;
			bytes[startIndex++] = (byte)(ErrorCode / 100);
			bytes[startIndex++] = (byte)(ErrorCode % 100);
			Attribute.CopyBytes(bytes, ref startIndex, base.Utf8Value);
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			int num = Attribute.ParseHeader(bytes, ref startIndex);
			startIndex += 2;
			ErrorCode = bytes[startIndex] * 100 + bytes[startIndex + 1];
			startIndex += 2;
			ParseUtf8String(bytes, ref startIndex, num - 4);
		}
	}
}
