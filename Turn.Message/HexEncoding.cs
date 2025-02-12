using System.Text;

public static class HexEncoding
{
	public static string ToHexString(this byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
		for (int i = 0; i < bytes.Length; i++)
		{
			stringBuilder.Append(((byte)(bytes[i] >> 4)).GetHexChar());
			stringBuilder.Append(bytes[i].GetHexChar());
		}
		return stringBuilder.ToString();
	}

	private static char GetHexChar(this byte b)
	{
		b = (byte)(b & 0xF);
		switch (b)
		{
		case 0:
			return '0';
		case 1:
			return '1';
		case 2:
			return '2';
		case 3:
			return '3';
		case 4:
			return '4';
		case 5:
			return '5';
		case 6:
			return '6';
		case 7:
			return '7';
		case 8:
			return '8';
		case 9:
			return '9';
		case 10:
			return 'a';
		case 11:
			return 'b';
		case 12:
			return 'c';
		case 13:
			return 'd';
		case 14:
			return 'e';
		default:
			return 'f';
		}
	}
}
