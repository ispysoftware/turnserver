using System;

namespace Turn.Message
{
	public struct ConnectionId : IEquatable<ConnectionId>
	{
		public long Value1;

		public long Value2;

		public int Value3;

		public void GetBytes(byte[] bytes, ref int startIndex)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(Value1), 0, bytes, startIndex, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(Value2), 0, bytes, startIndex + 8, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(Value3), 0, bytes, startIndex + 16, 4);
			startIndex += 20;
		}

		public void Parse(byte[] bytes, ref int startIndex)
		{
			Value1 = BitConverter.ToInt64(bytes, startIndex);
			Value2 = BitConverter.ToInt64(bytes, startIndex + 8);
			Value3 = BitConverter.ToInt32(bytes, startIndex + 16);
			startIndex += 20;
		}

		public bool Equals(ConnectionId other)
		{
			return Value1 == other.Value1 && Value2 == other.Value2 && Value3 == other.Value3;
		}

		public static bool operator ==(ConnectionId id1, ConnectionId id2)
		{
			return id1.Equals(id2);
		}

		public static bool operator !=(ConnectionId id1, ConnectionId id2)
		{
			return !id1.Equals(id2);
		}

		public override bool Equals(object obj)
		{
			return obj != null && obj is ConnectionId && Equals((ConnectionId)obj);
		}

		public override int GetHashCode()
		{
			return Value1.GetHashCode() ^ Value2.GetHashCode() ^ Value3.GetHashCode();
		}

		public override string ToString()
		{
			return $"{Value1:x16}{Value2:x16}{Value3:x8}";
		}
	}
}
