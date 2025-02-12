using Microsoft.Win32.Ssp;
using System;

namespace SocketServers
{
	internal class SspiContext : IDisposable
	{
		public bool Connected;

		public SafeCtxtHandle Handle;

		public SecBufferDescEx SecBufferDesc5;

		public SecBufferDescEx[] SecBufferDesc2;

		public SecPkgContext_StreamSizes StreamSizes;

		public readonly StreamBuffer Buffer;

		public SspiContext()
		{
			Handle = new SafeCtxtHandle();
			SecBufferDesc5 = new SecBufferDescEx(new SecBufferEx[5]);
			SecBufferDesc2 = new SecBufferDescEx[2]
			{
				new SecBufferDescEx(new SecBufferEx[2]),
				new SecBufferDescEx(new SecBufferEx[2])
			};
			Buffer = new StreamBuffer();
		}

		public void Dispose()
		{
			Handle.Dispose();
			Buffer.Dispose();
		}
	}
}
