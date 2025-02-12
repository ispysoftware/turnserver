using Microsoft.Win32.Ssp;
using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SocketServers
{
	internal class SspiTlsServer<C> : BaseTcpServer<C> where C : BaseConnection, IDisposable, new()
	{
		private X509Certificate certificate;

		private SafeCredHandle credential;

		private int maxTokenSize;

		public SspiTlsServer(ServersManagerConfig config)
			: base(config)
		{
			certificate = config.TlsCertificate;
		}

		public override void Start()
		{
			Sspi.AcquireCredentialsHandle(CredentialUse.SECPKG_CRED_INBOUND, new SchannelCred(certificate, SchProtocols.TlsServer), out credential, out long _);
			GetMaxTokenSize();
			base.Start();
		}

		public override void Dispose()
		{
			credential.Dispose();
			base.Dispose();
		}

		public override void SendAsync(ServerAsyncEventArgs e)
		{
			try
			{
				Connection<C> tcpConnection = GetTcpConnection(e.RemoteEndPoint);
				OnBeforeSend(tcpConnection, e);
				if (tcpConnection == null)
				{
					e.Completed = base.Send_Completed;
					e.SocketError = SocketError.NotConnected;
					e.OnCompleted(null);
				}
				else
				{
					SspiContext sspiContext = tcpConnection.SspiContext;
					SecPkgContext_StreamSizes streamSizes = sspiContext.StreamSizes;
					int count = e.Count;
					if (e.OffsetOffset < streamSizes.cbHeader)
					{
						throw new NotImplementedException("Ineffective way not implemented. Need to move buffer for SECBUFFER_STREAM_HEADER.");
					}
					e.OffsetOffset -= streamSizes.cbHeader;
					e.Count = streamSizes.cbHeader + count + streamSizes.cbTrailer;
					e.ReAllocateBuffer(keepData: true);
					SecBufferDescEx message = new SecBufferDescEx(new SecBufferEx[4]
					{
						new SecBufferEx
						{
							BufferType = BufferType.SECBUFFER_STREAM_HEADER,
							Buffer = e.Buffer,
							Size = streamSizes.cbHeader,
							Offset = e.Offset
						},
						new SecBufferEx
						{
							BufferType = BufferType.SECBUFFER_DATA,
							Buffer = e.Buffer,
							Size = count,
							Offset = e.Offset + streamSizes.cbHeader
						},
						new SecBufferEx
						{
							BufferType = BufferType.SECBUFFER_STREAM_TRAILER,
							Buffer = e.Buffer,
							Size = streamSizes.cbTrailer,
							Offset = e.Offset + streamSizes.cbHeader + count
						},
						new SecBufferEx
						{
							BufferType = BufferType.SECBUFFER_VERSION
						}
					});
                    unsafe
					{
						Sspi.EncryptMessage(ref sspiContext.Handle, ref message, 0u, null);
					}
					e.Count = message.Buffers[0].Size + message.Buffers[1].Size + message.Buffers[2].Size;
					e.ReAllocateBuffer(keepData: true);
					SendAsync(tcpConnection, e);
				}
			}
			catch (SspiException error)
			{
				e.SocketError = SocketError.Fault;
				OnFailed(new ServerInfoEventArgs(realEndPoint, error));
			}
		}

		protected override void OnNewTcpConnection(Connection<C> connection)
		{
			connection.SspiContext.Connected = false;
			connection.SspiContext.Buffer.Resize(maxTokenSize);
		}

		protected override void OnEndTcpConnection(Connection<C> connection)
		{
			if (connection.SspiContext.Connected)
			{
				connection.SspiContext.Connected = false;
				OnEndConnection(connection);
			}
		}

		protected override bool OnTcpReceived(Connection<C> connection, ref ServerAsyncEventArgs e)
		{
			bool connected = connection.SspiContext.Connected;
			bool flag = (!connection.SspiContext.Connected) ? Handshake(e, connection) : DecryptData(ref e, connection);
			while (flag && connected != connection.SspiContext.Connected && connection.SspiContext.Buffer.IsValid)
			{
				connected = connection.SspiContext.Connected;
				ServerAsyncEventArgs e2 = null;
				flag = ((!connection.SspiContext.Connected) ? Handshake(e2, connection) : DecryptData(ref e2, connection));
			}
			return flag;
		}

		private bool DecryptData(ref ServerAsyncEventArgs e, Connection<C> connection)
		{
			SspiContext sspiContext = connection.SspiContext;
			SecBufferDescEx message = sspiContext.SecBufferDesc5;
			if (sspiContext.Buffer.IsValid && e != null && !sspiContext.Buffer.CopyTransferredFrom(e, 0))
			{
				return false;
			}
			int bufferIndex;
			do
			{
				message.Buffers[0].BufferType = BufferType.SECBUFFER_DATA;
				if (sspiContext.Buffer.IsValid)
				{
					SetSecBuffer(ref message.Buffers[0], sspiContext);
				}
				else
				{
					SetSecBuffer(ref message.Buffers[0], e);
				}
				message.Buffers[1].SetBufferEmpty();
				message.Buffers[2].SetBufferEmpty();
				message.Buffers[3].SetBufferEmpty();
				message.Buffers[4].SetBufferEmpty();
                unsafe
				{
					SecurityStatus securityStatus = Sspi.SafeDecryptMessage(ref sspiContext.Handle, ref message, 0u, null);
					bufferIndex = message.GetBufferIndex(BufferType.SECBUFFER_EXTRA, 0);
					int bufferIndex2 = message.GetBufferIndex(BufferType.SECBUFFER_DATA, 0);
					switch (securityStatus)
					{
						case SecurityStatus.SEC_E_OK:
							if (bufferIndex2 >= 0)
							{
								if (sspiContext.Buffer.IsInvalid)
								{
									if (bufferIndex >= 0 && !sspiContext.Buffer.CopyFrom(message.Buffers[bufferIndex]))
									{
										return false;
									}
									e.Offset = message.Buffers[bufferIndex2].Offset;
									e.BytesTransferred = message.Buffers[bufferIndex2].Size;
									e.SetMaxCount();
									if (!OnReceived(connection, ref e))
									{
										return false;
									}
									break;
								}
								ArraySegment<byte> buffer = sspiContext.Buffer.Detach();
								if (bufferIndex >= 0 && !sspiContext.Buffer.CopyFrom(message.Buffers[bufferIndex]))
								{
									return false;
								}
								ServerAsyncEventArgs e2 = EventArgsManager.Get();
								PrepareEventArgs(connection, e2);
								e2.AttachBuffer(buffer);
								e2.Offset = message.Buffers[bufferIndex2].Offset;
								e2.BytesTransferred = message.Buffers[bufferIndex2].Size;
								e2.SetMaxCount();
								bool flag = OnReceived(connection, ref e2);
								if (e2 != null)
								{
									EventArgsManager.Put(e2);
								}
								if (!flag)
								{
									return false;
								}
								break;
							}
							return false;
						case SecurityStatus.SEC_E_INCOMPLETE_MESSAGE:
							if (sspiContext.Buffer.IsInvalid && !sspiContext.Buffer.CopyTransferredFrom(e, 0))
							{
								return false;
							}
							return true;
						case SecurityStatus.SEC_I_RENEGOTIATE:
							return false;
						default:
							return false;
					}
				}
			}
			while (bufferIndex >= 0);
			return true;
		}

		private bool Handshake(ServerAsyncEventArgs ie, Connection<C> connection)
		{
			int contextAttr = 0;
			ServerAsyncEventArgs value = null;
			SspiContext sspiContext = connection.SspiContext;
			SecBufferDescEx input = sspiContext.SecBufferDesc2[0];
			SecBufferDescEx output = sspiContext.SecBufferDesc2[1];
			try
			{
				if (!sspiContext.Buffer.IsValid || ie == null || sspiContext.Buffer.CopyTransferredFrom(ie, 0))
				{
					while (true)
					{
						input.Buffers[0].BufferType = BufferType.SECBUFFER_TOKEN;
						if (sspiContext.Buffer.IsValid)
						{
							SetSecBuffer(ref input.Buffers[0], sspiContext);
						}
						else
						{
							SetSecBuffer(ref input.Buffers[0], ie);
						}
						input.Buffers[1].SetBufferEmpty();
						if (value == null)
						{
							value = EventArgsManager.Get();
						}
						value.AllocateBuffer();
						output.Buffers[0].BufferType = BufferType.SECBUFFER_TOKEN;
						output.Buffers[0].Size = value.Count;
						output.Buffers[0].Buffer = value.Buffer;
						output.Buffers[0].Offset = value.Offset;
						output.Buffers[1].SetBufferEmpty();
						int contextReq = 98332;
						SafeCtxtHandle newContext = sspiContext.Handle.IsInvalid ? new SafeCtxtHandle() : sspiContext.Handle;
						long timeStamp;
						SecurityStatus securityStatus = Sspi.SafeAcceptSecurityContext(ref credential, ref sspiContext.Handle, ref input, contextReq, TargetDataRep.SECURITY_NATIVE_DREP, ref newContext, ref output, out contextAttr, out timeStamp);
						if (sspiContext.Handle.IsInvalid)
						{
							sspiContext.Handle = newContext;
						}
						switch (securityStatus)
						{
						case SecurityStatus.SEC_E_INCOMPLETE_MESSAGE:
							if (sspiContext.Buffer.IsInvalid && !sspiContext.Buffer.CopyTransferredFrom(ie, 0))
							{
								return false;
							}
							return true;
						case SecurityStatus.SEC_E_BUFFER_TOO_SMALL:
							if (value.Count >= maxTokenSize)
							{
								return false;
							}
							value.Count = maxTokenSize;
							value.ReAllocateBuffer(keepData: false);
							break;
						default:
						{
							if ((securityStatus == SecurityStatus.SEC_I_CONTINUE_NEEDED || securityStatus == SecurityStatus.SEC_E_OK || (Sspi.Failed(securityStatus) && (contextAttr & 0x8000) != 0)) && output.Buffers[0].Size > 0)
							{
								value.Count = output.Buffers[0].Size;
								value.CopyAddressesFrom(ie);
								value.LocalEndPoint = GetLocalEndpoint(ie.RemoteEndPoint.Address);
								SendAsync(connection, value);
								value = null;
							}
							int bufferIndex = input.GetBufferIndex(BufferType.SECBUFFER_EXTRA, 0);
							if (bufferIndex < 0)
							{
								sspiContext.Buffer.Free();
							}
							else if (sspiContext.Buffer.IsInvalid)
							{
								if (!sspiContext.Buffer.CopyTransferredFrom(ie, ie.BytesTransferred - input.Buffers[bufferIndex].Size))
								{
									return false;
								}
							}
							else
							{
								sspiContext.Buffer.MoveToBegin(sspiContext.Buffer.BytesTransferred - input.Buffers[bufferIndex].Size, input.Buffers[bufferIndex].Size);
							}
							switch (securityStatus)
							{
							case SecurityStatus.SEC_E_OK:
								if (Sspi.SafeQueryContextAttributes(ref sspiContext.Handle, out sspiContext.StreamSizes) != 0)
								{
									return false;
								}
								sspiContext.Connected = true;
								OnNewConnection(connection);
								return true;
							case SecurityStatus.SEC_I_CONTINUE_NEEDED:
								if (bufferIndex < 0)
								{
									return true;
								}
								break;
							default:
								return false;
							}
							break;
						}
						}
					}
				}
				return false;
			}
			finally
			{
				if (value != null)
				{
					EventArgsManager.Put(ref value);
				}
			}
		}

		private void SetSecBuffer(ref SecBufferEx secBuffer, ServerAsyncEventArgs e)
		{
			secBuffer.Buffer = e.Buffer;
			secBuffer.Offset = e.Offset;
			secBuffer.Size = e.BytesTransferred;
		}

		public void SetSecBuffer(ref SecBufferEx secBuffer, SspiContext context)
		{
			secBuffer.Buffer = context.Buffer.Array;
			secBuffer.Offset = context.Buffer.Offset;
			secBuffer.Size = context.Buffer.BytesTransferred;
		}

		private void GetMaxTokenSize()
		{
			if (Sspi.EnumerateSecurityPackages(out int packages, out SafeContextBufferHandle secPkgInfos) != 0)
			{
				throw new Win32Exception("Failed to EnumerateSecurityPackages");
			}
			for (int i = 0; i < packages; i++)
			{
				SecPkgInfo item = secPkgInfos.GetItem<SecPkgInfo>(i);
				if (string.Compare(item.GetName(), "Schannel", ignoreCase: true) == 0)
				{
					maxTokenSize = item.cbMaxToken;
					break;
				}
			}
			if (maxTokenSize == 0)
			{
				throw new Exception("Failed to retrive cbMaxToken for Schannel");
			}
		}
	}
}
