namespace Turn.Message
{
	internal static class Helpers
	{
		public static bool AreArraysEqual(this byte[] array1, byte[] array2)
		{
			return array1.AreArraysEqual(array2, 0, array2.Length);
		}

		public static bool AreArraysEqual(this byte[] array1, byte[] array2, int startIndex2, int length2)
		{
			if (array1.Length != length2)
			{
				return false;
			}
			for (int i = 0; i < array1.Length; i++)
			{
				if (array1[i] != array2[startIndex2 + i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
