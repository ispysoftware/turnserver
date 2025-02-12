using System;

namespace Turn.Message
{
	public class TransactionId
	{
		public const int DefaultStartIndex = 4;

		public const int Length = 16;

		public byte[] Value;

		public TransactionId()
		{
		}

		public TransactionId(byte[] value, int startIndex)
		{
			Value = new byte[16];
			Array.Copy(value, startIndex, Value, 0, 16);
		}

		public static TransactionId Generate()
		{
			TransactionId transactionId = new TransactionId();
			transactionId.Value = new byte[16];
			TransactionId transactionId2 = transactionId;
			new Random(Environment.TickCount).NextBytes(transactionId2.Value);
			return transactionId2;
		}

		public override bool Equals(object obj)
		{
			if (Value == null)
			{
				return false;
			}
			if (obj == null)
			{
				return false;
			}
			byte[] array;
			if (obj is TransactionId)
			{
				array = (obj as TransactionId).Value;
			}
			else
			{
				if (!(obj is byte[]))
				{
					return false;
				}
				array = (obj as byte[]);
			}
			if (array == null)
			{
				return false;
			}
			if (Value.Length != array.Length)
			{
				return false;
			}
			for (int i = 0; i < Value.Length; i++)
			{
				if (Value[i] != array[i])
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = 0;
			int i;
			for (i = 0; Value.Length - i >= 4; i += 4)
			{
				num ^= BitConverter.ToInt32(Value, i);
			}
			if (Value.Length - i >= 2)
			{
				num ^= BitConverter.ToInt16(Value, i);
				i += 2;
			}
			if (i < Value.Length)
			{
				num ^= Value[i++] << 16;
			}
			return num;
		}

		public static bool operator ==(TransactionId id1, TransactionId id2)
		{
			return object.Equals(id1, id2);
		}

		public static bool operator !=(TransactionId id1, TransactionId id2)
		{
			return !object.Equals(id1, id2);
		}

		public override string ToString()
		{
			return Value.ToHexString();
		}
	}
}
