using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pcap
{
	public class PcapWriter : IDisposable
	{
		private enum EtherType : ushort
		{
			None = ushort.MaxValue,
			IPv4 = 0x800,
			IPv6 = 34525
		}

		public const int IPv4HeaderLength = 20;

		public const int IPv6HeaderLength = 40;

		public const int EthernetLength = 14;

		public const int UdpLength = 8;

		public const int TcpLength = 20;

		public const int TlsLength = 5;

		public const int MaxRecordLength = 65535;

		private readonly object sync;

		private readonly Stream stream;

		private readonly DateTime nixTimeStart;

		private readonly byte[] mac1;

		private readonly byte[] mac2;

		[ThreadStatic]
		private static MemoryStream cacheStream;

		[ThreadStatic]
		private static BinaryWriter writter;

		public PcapWriter(Stream stream)
		{
			sync = new object();
			nixTimeStart = new DateTime(1970, 1, 1);
			mac1 = new byte[6]
			{
				128,
				128,
				128,
				128,
				128,
				128
			};
			mac2 = new byte[6]
			{
				144,
				144,
				144,
				144,
				144,
				144
			};
			this.stream = stream;
			CreateWritter();
			WriteGlobalHeader();
			WriteChangesToStream();
		}

		public void Dispose()
		{
			stream.Dispose();
		}

		public void Flush()
		{
			stream.Flush();
		}

		public void WriteComment(string comment)
		{
			CreateWritter();
			byte[] bytes = Encoding.UTF8.GetBytes(comment);
			WritePacketHeader(bytes.Length + 14);
			WriteEthernetHeader(EtherType.None);
			writter.Write(bytes);
			WriteChangesToStream();
		}

		public void Write(byte[] bytes, Protocol protocol, IPEndPoint source, IPEndPoint destination)
		{
			Write(bytes, 0, bytes.Length, protocol, source, destination);
		}

		public void Write(byte[] bytes, int offset, int length, Protocol protocol, IPEndPoint source, IPEndPoint destination)
		{
			CreateWritter();
			if (source.AddressFamily != destination.AddressFamily)
			{
				throw new ArgumentException("source.AddressFamily != destination.AddressFamily");
			}
			if (length > 65279)
			{
				length = 65279;
			}
			int num = (protocol == Protocol.Udp) ? 8 : (20 + ((protocol == Protocol.Tls) ? 5 : 0));
			if (source.AddressFamily == AddressFamily.InterNetwork)
			{
				WritePacketHeader(length + num + 20 + 14);
				WriteEthernetHeader(EtherType.IPv4);
				WriteIpV4Header(length + num, protocol != Protocol.Udp, source.Address, destination.Address);
			}
			else
			{
				if (source.AddressFamily != AddressFamily.InterNetworkV6)
				{
					throw new ArgumentOutOfRangeException("source.AddressFamily");
				}
				WritePacketHeader(length + num + 40 + 14);
				WriteEthernetHeader(EtherType.IPv6);
				WriteIpV6Header(length + num, protocol != Protocol.Udp, source.Address, destination.Address);
			}
			if (protocol == Protocol.Udp)
			{
				WriteUdpHeader(length, (short)source.Port, (short)destination.Port);
			}
			else
			{
				WriteTcpHeader(length + ((protocol == Protocol.Tls) ? 5 : 0), (short)source.Port, (short)destination.Port);
				if (protocol == Protocol.Tls)
				{
					WriteTlsHeader(length);
				}
			}
			writter.Write(bytes, offset, length);
			WriteChangesToStream();
		}

		private void CreateWritter()
		{
			if (writter == null)
			{
				cacheStream = new MemoryStream();
				writter = new BinaryWriter(cacheStream);
			}
		}

		private void WriteChangesToStream()
		{
			cacheStream.Flush();
			lock (sync)
			{
				stream.Write(cacheStream.GetBuffer(), 0, Math.Min((int)cacheStream.Length, 65535));
			}
			cacheStream.SetLength(0L);
		}

		private void WriteGlobalHeader()
		{
			writter.Write(2712847316u);
			writter.Write((ushort)2);
			writter.Write((ushort)4);
			writter.Write(0);
			writter.Write(0);
			writter.Write(65535);
			writter.Write(1);
		}

		private void WritePacketHeader(int length)
		{
			TimeSpan timeSpan = DateTime.UtcNow - nixTimeStart;
			writter.Write((int)timeSpan.TotalSeconds);
			writter.Write(timeSpan.Milliseconds);
			writter.Write(length);
			writter.Write(length);
		}

		private void WriteEthernetHeader(EtherType etherType)
		{
			writter.Write(mac1);
			writter.Write(mac2);
			writter.Write(IPAddress.HostToNetworkOrder((short)etherType));
		}

		private void WriteIpV4Header(int length, bool tcpUdp, IPAddress source, IPAddress destination)
		{
			writter.Write((ushort)5);
			writter.Write(IPAddress.HostToNetworkOrder((short)(length + 20)));
			writter.Write(0);
			writter.Write(byte.MaxValue);
			writter.Write((byte)(tcpUdp ? 6 : 17));
			writter.Write((short)0);
			writter.Write((int)source.Address);
			writter.Write((int)destination.Address);
		}

		private void WriteIpV6Header(int length, bool tcpUdp, IPAddress source, IPAddress destination)
		{
			writter.Write(96);
			writter.Write(IPAddress.HostToNetworkOrder((short)length));
			writter.Write((byte)(tcpUdp ? 6 : 17));
			writter.Write(byte.MaxValue);
			writter.Write(source.GetAddressBytes());
			writter.Write(destination.GetAddressBytes());
		}

		private void WriteUdpHeader(int length, short sourcePort, short destinationPort)
		{
			writter.Write(IPAddress.HostToNetworkOrder(sourcePort));
			writter.Write(IPAddress.HostToNetworkOrder(destinationPort));
			writter.Write(IPAddress.HostToNetworkOrder((short)(8 + length)));
			writter.Write((short)0);
		}

		private void WriteTcpHeader(int length, short sourcePort, short destinationPort)
		{
			writter.Write(IPAddress.HostToNetworkOrder(sourcePort));
			writter.Write(IPAddress.HostToNetworkOrder(destinationPort));
			writter.Write(IPAddress.HostToNetworkOrder(0));
			writter.Write(IPAddress.HostToNetworkOrder(0));
			writter.Write((byte)80);
			writter.Write((byte)2);
			writter.Write((ushort)16383);
			writter.Write((ushort)0);
			writter.Write((ushort)0);
		}

		private void WriteTlsHeader(int length)
		{
			writter.Write((byte)23);
			writter.Write((ushort)259);
			writter.Write(IPAddress.HostToNetworkOrder((short)length));
		}
	}
}
