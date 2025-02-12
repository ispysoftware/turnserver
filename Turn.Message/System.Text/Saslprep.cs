namespace System.Text
{
	public static class Saslprep
	{
		public static string SASLprep(this string s)
		{
			string text = "";
			foreach (char c in s)
			{
				if (!c.IsCommonlyMappedToNothing())
				{
					text = ((!c.IsNonAsciiSpace()) ? (text + c) : (text + ' '));
				}
			}
			return text.Normalize(NormalizationForm.FormKC);
		}

		public static bool IsNonAsciiSpace(this char c)
		{
			switch (c)
			{
			case '\u00a0':
			case '\u1680':
			case '\u2000':
			case '\u2001':
			case '\u2002':
			case '\u2003':
			case '\u2004':
			case '\u2005':
			case '\u2006':
			case '\u2007':
			case '\u2008':
			case '\u2009':
			case '\u200a':
			case '\u200b':
			case '\u202f':
			case '\u205f':
			case '\u3000':
				return true;
			default:
				return false;
			}
		}

		public static bool IsCommonlyMappedToNothing(this char c)
		{
			switch (c)
			{
			case '­':
			case '\u034f':
			case '᠆':
			case '\u180b':
			case '\u180c':
			case '\u180d':
			case '\u200b':
			case '\u200c':
			case '\u200d':
			case '\u2060':
			case '\ufe00':
			case '\ufe01':
			case '\ufe02':
			case '\ufe03':
			case '\ufe04':
			case '\ufe05':
			case '\ufe06':
			case '\ufe07':
			case '\ufe08':
			case '\ufe09':
			case '\ufe0a':
			case '\ufe0b':
			case '\ufe0c':
			case '\ufe0d':
			case '\ufe0e':
			case '\ufe0f':
			case '\ufeff':
				return true;
			default:
				return false;
			}
		}
	}
}
