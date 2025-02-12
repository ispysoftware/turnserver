using System;
using System.Net;
using System.Net.Sockets;

namespace Turn.Message
{
	public abstract class AddressAttribute : Attribute
	{
		private IPEndPoint ipEndPoint;

		public override ushort ValueLength
		{
			get
			{
				return (ushort)(4 + ((IpAddress.AddressFamily == AddressFamily.InterNetwork) ? 4 : 16));
			}
			protected set
			{
				throw new InvalidOperationException();
			}
		}

		public IPEndPoint IpEndPoint
		{
			get
			{
				return ipEndPoint;
			}
			set
			{
				ipEndPoint.Address = value.Address;
				ipEndPoint.Port = value.Port;
			}
		}

		public ushort Port
		{
			get
			{
				return (ushort)IpEndPoint.Port;
			}
			set
			{
				IpEndPoint.Port = value;
			}
		}

		public IPAddress IpAddress
		{
			get
			{
				return IpEndPoint.Address;
			}
			set
			{
				IpEndPoint.Address = value;
			}
		}

		public AddressAttribute()
		{
			ipEndPoint = new IPEndPoint(IPAddress.None, 0);
		}

		public override void GetBytes(byte[] bytes, ref int startIndex)
		{
			GetBytes(bytes, ref startIndex, null);
		}

		public override void Parse(byte[] bytes, ref int startIndex)
		{
			Parse(bytes, ref startIndex, null);
		}

		protected void GetBytes(byte[] bytes, ref int startIndex, byte[] xorMask)
		{
			base.GetBytes(bytes, ref startIndex);
			bytes[startIndex++] = 0;
			bytes[startIndex++] = (byte)((IpAddress.AddressFamily == AddressFamily.InterNetwork) ? 1 : 2);
			Attribute.CopyBytes(bytes, ref startIndex, XorBytes(Port.GetBigendianBytes(), xorMask));
			Attribute.CopyBytes(bytes, ref startIndex, XorBytes(IpAddress.GetAddressBytes(), xorMask));
		}

		protected void Parse(byte[] bytes, ref int startIndex, byte[] xorMask)
		{
			ushort num = Attribute.ParseHeader(bytes, ref startIndex);
			startIndex++;
			byte b = bytes[startIndex++];
			Port = (ushort)(bytes.BigendianToUInt16(ref startIndex) ^ (xorMask?.BigendianToUInt16(0) ?? 0));
			switch (b)
			{
			case 1:
				if (num != 8)
				{
					throw new TurnMessageException(ErrorCode.BadRequest);
				}
				IpAddress = GetAddress(AddressFamily.InterNetwork, bytes, ref startIndex, xorMask);
				break;
			case 2:
				if (num != 20)
				{
					throw new TurnMessageException(ErrorCode.BadRequest);
				}
				IpAddress = GetAddress(AddressFamily.InterNetworkV6, bytes, ref startIndex, xorMask);
				break;
			default:
				throw new TurnMessageException(ErrorCode.BadRequest, "The address family of the attribute field MUST be set to 0x01 or 0x02.");
			}
		}

		private IPAddress GetAddress(AddressFamily addressFamily, byte[] bytes, ref int startIndex, byte[] xorMask)
		{
			byte[] array = new byte[(addressFamily == AddressFamily.InterNetwork) ? 4 : 16];
			Array.Copy(bytes, startIndex, array, 0, array.Length);
			startIndex += array.Length;
			XorBytes(array, xorMask);
			return new IPAddress(array);
		}

		private byte[] XorBytes(byte[] bytes, byte[] xorMask)
		{
			if (xorMask != null)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					bytes[i] ^= xorMask[i];
				}
			}
			return bytes;
		}
	}
}
