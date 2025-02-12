using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.Ssp
{
	public class SecBufferDescEx
	{
		internal SecBufferDesc SecBufferDesc;

		internal SecBuffer[] SecBuffers;

		private GCHandle[] Handles;

		private GCHandle DescHandle;

		public SecBufferEx[] Buffers;

		public SecBufferDescEx(SecBufferEx[] buffers)
		{
			SecBufferDesc.ulVersion = 0;
			SecBufferDesc.cBuffers = 0;
			SecBufferDesc.pBuffers = IntPtr.Zero;
			Handles = null;
			SecBuffers = null;
			Buffers = buffers;
		}

		public int GetBufferIndex(BufferType type, int from)
		{
			for (int i = from; i < Buffers.Length; i++)
			{
				if (Buffers[i].BufferType == type)
				{
					return i;
				}
			}
			return -1;
		}

		internal void Pin()
		{
			if (SecBuffers == null || SecBuffers.Length != Buffers.Length)
			{
				SecBuffers = new SecBuffer[Buffers.Length];
				Handles = new GCHandle[Buffers.Length];
			}
			for (int i = 0; i < Buffers.Length; i++)
			{
				if (Buffers[i].Buffer != null)
				{
					Handles[i] = GCHandle.Alloc(Buffers[i].Buffer, GCHandleType.Pinned);
				}
				SecBuffers[i].BufferType = (int)Buffers[i].BufferType;
				SecBuffers[i].cbBuffer = Buffers[i].Size;
				if (Buffers[i].Buffer == null)
				{
					SecBuffers[i].pvBuffer = IntPtr.Zero;
				}
				else
				{
					SecBuffers[i].pvBuffer = AddToPtr(Handles[i].AddrOfPinnedObject(), Buffers[i].Offset);
				}
			}
			DescHandle = GCHandle.Alloc(SecBuffers, GCHandleType.Pinned);
			SecBufferDesc.ulVersion = 0;
			SecBufferDesc.cBuffers = SecBuffers.Length;
			SecBufferDesc.pBuffers = DescHandle.AddrOfPinnedObject();
		}

		internal void Free()
		{
			object buffer = Buffers[0].Buffer;
			IntPtr begin = Handles[0].AddrOfPinnedObject();
			for (int i = 0; i < Buffers.Length; i++)
			{
				Buffers[i].BufferType = (BufferType)SecBuffers[i].BufferType;
				Buffers[i].Size = SecBuffers[i].cbBuffer;
				if (Buffers[i].Size == 0 || Buffers[i].BufferType == BufferType.SECBUFFER_VERSION)
				{
					Buffers[i].Buffer = null;
					Buffers[i].Offset = 0;
					continue;
				}
				Buffers[i].Buffer = buffer;
				if (SecBuffers[i].pvBuffer != IntPtr.Zero)
				{
					Buffers[i].Offset = SubPtr(begin, SecBuffers[i].pvBuffer);
				}
			}
			for (int j = 0; j < Buffers.Length; j++)
			{
				if (Handles[j].IsAllocated)
				{
					Handles[j].Free();
				}
			}
			DescHandle.Free();
		}

		private int SubPtr(IntPtr begin, IntPtr current)
		{
			return (int)((long)current - (long)begin);
		}

		private IntPtr AddToPtr(IntPtr begin, int offset)
		{
			return (IntPtr)((long)begin + offset);
		}
	}
}
