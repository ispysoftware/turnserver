using Pcap;
using System;
using System.IO;

namespace SocketServers
{
	public class Logger : IDisposable
	{
		private object sync;

		private PcapWriter writer;

		public bool IsEnabled
		{
			get;
			private set;
		}

		public Logger()
		{
			sync = new object();
		}

		internal void Dispose()
		{
			((IDisposable)this).Dispose();
		}

		void IDisposable.Dispose()
		{
			if (writer != null)
			{
				writer.Dispose();
			}
		}

		public void Enable(string filename)
		{
			Enable(File.Create(filename));
		}

		public void Enable(Stream stream)
		{
			lock (sync)
			{
				if (IsEnabled)
				{
					Disable();
				}
				writer = new PcapWriter(stream);
				IsEnabled = true;
			}
		}

		public void Disable()
		{
			lock (sync)
			{
				IsEnabled = false;
				if (writer != null)
				{
					writer.Dispose();
				}
				writer = null;
			}
		}

		public void Flush()
		{
			try
			{
				writer?.Flush();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public void WriteComment(string comment)
		{
			try
			{
				writer?.WriteComment(comment);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		internal void Write(ServerAsyncEventArgs e, bool incomingOutgoing)
		{
			try
			{
				writer?.Write(e.Buffer, e.Offset, incomingOutgoing ? e.BytesTransferred : e.Count, Convert(e.LocalEndPoint.Protocol), incomingOutgoing ? e.RemoteEndPoint : e.LocalEndPoint, incomingOutgoing ? e.LocalEndPoint : e.RemoteEndPoint);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private Protocol Convert(ServerProtocol source)
		{
			switch (source)
			{
			case ServerProtocol.Udp:
				return Protocol.Udp;
			case ServerProtocol.Tls:
				return Protocol.Tls;
			default:
				return Protocol.Tcp;
			}
		}
	}
}
