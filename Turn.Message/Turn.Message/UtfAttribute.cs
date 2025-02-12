using System.Text;

namespace Turn.Message
{
	public abstract class UtfAttribute : Attribute
	{
		private byte[] utf8Value;

		private string stringValue;

		private UTF8Encoding utf8Encoding;

		protected string StringValue
		{
			get
			{
				return stringValue;
			}
			set
			{
				utf8Value = null;
				stringValue = value;
			}
		}

		protected byte[] Utf8Value
		{
			get
			{
				if (utf8Value == null)
				{
					utf8Value = Utf8Encoding.GetBytes(stringValue);
				}
				return utf8Value;
			}
		}

		protected UTF8Encoding Utf8Encoding
		{
			get
			{
				if (utf8Encoding == null)
				{
					utf8Encoding = new UTF8Encoding();
				}
				return utf8Encoding;
			}
		}

		public UtfAttribute()
		{
		}

		protected void ParseUtf8String(byte[] bytes, ref int startIndex, int length)
		{
			StringValue = Utf8Encoding.GetString(bytes, startIndex, length);
			startIndex += length;
		}
	}
}
