using System;

namespace SocketServers
{
	public abstract class HeaderContentConnection : BaseConnection
	{
		private enum StreamState
		{
			WaitingHeaders,
			WaitingHeadersContinue,
			WaitingMicroBody,
			WaitingSmallBody,
			WaitingBigBody
		}

		private enum Storage
		{
			None,
			E,
			E1,
			Buffer1,
			Buffer2
		}

		protected enum ParseCode
		{
			NotEnoughData,
			HeaderDone,
			Error,
			Skip
		}

		protected struct ParseResult
		{
			public ParseCode ParseCode;

			public int HeaderLength;

			public int ContentLength;

			public int Count => HeaderLength;

			public ParseResult(ParseCode parseCode, int headerLength, int contentLength)
			{
				ParseCode = parseCode;
				HeaderLength = headerLength;
				ContentLength = contentLength;
			}

			public static ParseResult HeaderDone(int headerLength, int contentLength)
			{
				return new ParseResult(ParseCode.HeaderDone, headerLength, contentLength);
			}

			public static ParseResult Skip(int count)
			{
				return new ParseResult(ParseCode.Skip, count, 0);
			}

			public static ParseResult Error()
			{
				ParseResult result = default(ParseResult);
				result.ParseCode = ParseCode.Error;
				return result;
			}

			public static ParseResult NotEnoughData()
			{
				ParseResult result = default(ParseResult);
				result.ParseCode = ParseCode.NotEnoughData;
				return result;
			}
		}

		protected enum ResetReason
		{
			NotEnoughData,
			ResetStateCalled
		}

		public const int MaximumHeadersSize = 8192;

		private const int MinimumBuffer1Size = 4096;

		private ServerAsyncEventArgs e1;

		private StreamBuffer buffer1;

		private StreamBuffer buffer2;

		private StreamState state;

		private int expectedContentLength;

		private int receivedContentLength;

		private int buffer1UnusedCount;

		private ArraySegment<byte> headerData;

		private ArraySegment<byte> contentData;

		private int bytesProccessed;

		private bool ready;

		private Storage readerStorage;

		private Storage contentStorage;

		private int keepAliveRecived;

		private StreamBuffer Buffer1
		{
			get
			{
				if (buffer1 == null)
				{
					buffer1 = new StreamBuffer();
				}
				return buffer1;
			}
		}

		private StreamBuffer Buffer2
		{
			get
			{
				if (buffer2 == null)
				{
					buffer2 = new StreamBuffer();
				}
				return buffer2;
			}
		}

		public ArraySegment<byte> Header => headerData;

		public bool IsMessageReady => ready;

		public ArraySegment<byte> Content => contentData;

		public HeaderContentConnection()
		{
			state = StreamState.WaitingHeaders;
		}

		public new void Dispose()
		{
			base.Dispose();
			if (buffer1 != null)
			{
				buffer1.Dispose();
			}
			if (buffer2 != null)
			{
				buffer2.Dispose();
			}
			if (e1 != null)
			{
				e1.Dispose();
				e1 = null;
			}
		}

		public void ResetState()
		{
			ready = false;
			headerData = default(ArraySegment<byte>);
			expectedContentLength = 0;
			receivedContentLength = 0;
			contentData = default(ArraySegment<byte>);
			readerStorage = Storage.None;
			contentStorage = Storage.None;
			ResetParser(ResetReason.ResetStateCalled);
			if (buffer1 != null)
			{
				buffer1UnusedCount = ((buffer1.Count <= 0) ? (buffer1UnusedCount + 1) : 0);
				if (buffer1.Capacity <= 8192 && buffer1UnusedCount < 8)
				{
					buffer1.Clear();
				}
				else
				{
					buffer1.Free();
				}
			}
			if (buffer2 != null)
			{
				buffer2.Free();
			}
			if (e1 != null)
			{
				e1.Dispose();
				e1 = null;
			}
			keepAliveRecived = 0;
			state = StreamState.WaitingHeaders;
		}

		public bool Proccess(ref ServerAsyncEventArgs e, out bool closeConnection)
		{
			closeConnection = false;
			switch (state)
			{
			case StreamState.WaitingHeaders:
			{
				int num2 = bytesProccessed;
				ArraySegment<byte> data = new ArraySegment<byte>(e.Buffer, e.Offset + bytesProccessed, e.BytesTransferred - bytesProccessed);
				PreProcessRaw(data);
				ParseResult parseResult = Parse(data);
				switch (parseResult.ParseCode)
				{
				case ParseCode.NotEnoughData:
					bytesProccessed += data.Count;
					ResetParser(ResetReason.NotEnoughData);
					Buffer1.Resize(8192);
					Buffer1.CopyTransferredFrom(e, num2);
					state = StreamState.WaitingHeadersContinue;
					break;
				case ParseCode.Error:
					closeConnection = true;
					break;
				case ParseCode.Skip:
					bytesProccessed += parseResult.Count;
					break;
				case ParseCode.HeaderDone:
				{
					bytesProccessed += parseResult.HeaderLength;
					SetReaderStorage(Storage.E, e.Buffer, e.Offset + num2, parseResult.HeaderLength);
					expectedContentLength = parseResult.ContentLength;
					if (expectedContentLength <= 0)
					{
						SetReady();
						break;
					}
					int num3 = e.BytesTransferred - bytesProccessed;
					if (num3 >= expectedContentLength)
					{
						SetReady(Storage.E, e.Buffer, e.Offset + bytesProccessed, expectedContentLength);
						bytesProccessed += expectedContentLength;
						break;
					}
					if (expectedContentLength <= e.Count - e.BytesTransferred)
					{
						state = StreamState.WaitingMicroBody;
					}
					else if (expectedContentLength < 8192)
					{
						if ((Buffer1.IsInvalid || Buffer1.Capacity < expectedContentLength) && !Buffer1.Resize(Math.Max(expectedContentLength, 4096)))
						{
							closeConnection = true;
						}
						if (!closeConnection)
						{
							Buffer1.CopyTransferredFrom(e, bytesProccessed);
							state = StreamState.WaitingSmallBody;
						}
					}
					else if (!Buffer2.Resize(expectedContentLength))
					{
						closeConnection = true;
					}
					else
					{
						Buffer2.CopyTransferredFrom(e, bytesProccessed);
						state = StreamState.WaitingBigBody;
					}
					if (!closeConnection)
					{
						e1 = e;
						e = null;
						readerStorage = Storage.E1;
					}
					bytesProccessed += num3;
					receivedContentLength += num3;
					break;
				}
				}
				break;
			}
			case StreamState.WaitingHeadersContinue:
			{
				int num5 = Math.Min(e.BytesTransferred - bytesProccessed, Buffer1.FreeSize);
				PreProcessRaw(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred - bytesProccessed));
				Buffer.BlockCopy(e.Buffer, e.Offset, Buffer1.Array, Buffer1.Offset + Buffer1.Count, num5);
				ArraySegment<byte> data2 = new ArraySegment<byte>(Buffer1.Array, Buffer1.Offset, Buffer1.Count + num5);
				ParseResult parseResult2 = Parse(data2);
				switch (parseResult2.ParseCode)
				{
				case ParseCode.NotEnoughData:
					ResetParser(ResetReason.NotEnoughData);
					if (data2.Count < Buffer1.Capacity)
					{
						Buffer1.AddCount(num5);
						bytesProccessed += num5;
					}
					else
					{
						closeConnection = true;
					}
					break;
				case ParseCode.Error:
					closeConnection = true;
					break;
				case ParseCode.Skip:
					throw new NotImplementedException();
				case ParseCode.HeaderDone:
				{
					int num6 = parseResult2.HeaderLength - Buffer1.Count;
					Buffer1.AddCount(num6);
					bytesProccessed += num6;
					SetReaderStorage(Storage.Buffer1, Buffer1.Array, Buffer1.Offset, parseResult2.HeaderLength);
					expectedContentLength = parseResult2.ContentLength;
					if (expectedContentLength <= 0)
					{
						SetReady();
						break;
					}
					int num7 = e.BytesTransferred - bytesProccessed;
					if (num7 >= expectedContentLength)
					{
						SetReady(Storage.E, e.Buffer, e.Offset + bytesProccessed, expectedContentLength);
						bytesProccessed += expectedContentLength;
						break;
					}
					if (expectedContentLength < Buffer1.FreeSize)
					{
						Buffer1.AddCount(num7);
						state = StreamState.WaitingSmallBody;
					}
					else
					{
						if (!Buffer2.Resize(expectedContentLength))
						{
							closeConnection = true;
						}
						Buffer2.CopyTransferredFrom(e, bytesProccessed);
						state = StreamState.WaitingBigBody;
					}
					bytesProccessed += num7;
					receivedContentLength += num7;
					break;
				}
				}
				break;
			}
			case StreamState.WaitingMicroBody:
			{
				int num8 = Math.Min(e.BytesTransferred - bytesProccessed, expectedContentLength - receivedContentLength);
				ArraySegment<byte> data3 = new ArraySegment<byte>(e.Buffer, e.Offset + bytesProccessed, num8);
				PreProcessRaw(data3);
				Buffer.BlockCopy(data3.Array, data3.Offset, e1.Buffer, e1.Offset + e1.BytesTransferred, data3.Count);
				e1.BytesTransferred += num8;
				receivedContentLength += num8;
				bytesProccessed += num8;
				if (receivedContentLength == expectedContentLength)
				{
					SetReady(Storage.E1, e1.Buffer, e1.Offset + e1.BytesTransferred - receivedContentLength, receivedContentLength);
				}
				break;
			}
			case StreamState.WaitingSmallBody:
			{
				int num4 = Math.Min(e.BytesTransferred - bytesProccessed, expectedContentLength - receivedContentLength);
				ArraySegment<byte> arraySegment2 = new ArraySegment<byte>(e.Buffer, e.Offset + bytesProccessed, num4);
				PreProcessRaw(arraySegment2);
				Buffer1.CopyFrom(arraySegment2);
				receivedContentLength += num4;
				bytesProccessed += num4;
				if (receivedContentLength == expectedContentLength)
				{
					SetReady(Storage.Buffer1, Buffer1.Array, Buffer1.Offset + Buffer1.Count - receivedContentLength, receivedContentLength);
				}
				break;
			}
			case StreamState.WaitingBigBody:
			{
				int num = Math.Min(e.BytesTransferred - bytesProccessed, expectedContentLength - receivedContentLength);
				ArraySegment<byte> arraySegment = new ArraySegment<byte>(e.Buffer, e.Offset + bytesProccessed, num);
				PreProcessRaw(arraySegment);
				Buffer2.CopyFrom(arraySegment);
				receivedContentLength += num;
				bytesProccessed += num;
				if (receivedContentLength == expectedContentLength)
				{
					SetReady(Storage.Buffer2, Buffer2.Array, Buffer2.Offset + Buffer2.Count - receivedContentLength, receivedContentLength);
				}
				break;
			}
			}
			bool flag = !closeConnection && e != null && bytesProccessed < e.BytesTransferred;
			if (!flag)
			{
				bytesProccessed = 0;
			}
			return flag;
		}

		public void Dettach(ref ServerAsyncEventArgs e, out ArraySegment<byte> segment1, out ArraySegment<byte> segment2)
		{
			if (readerStorage == Storage.E)
			{
				int num = headerData.Count;
				if (contentStorage == readerStorage)
				{
					num += contentData.Count;
				}
				segment1 = Detach(ref e, num);
				segment2 = ((contentStorage != readerStorage) ? Dettach(contentStorage) : default(ArraySegment<byte>));
				return;
			}
			segment1 = Dettach(readerStorage);
			if (contentStorage != readerStorage)
			{
				if (contentStorage == Storage.E)
				{
					segment2 = Detach(ref e, contentData.Count);
				}
				else
				{
					segment2 = Dettach(contentStorage);
				}
			}
			else
			{
				segment2 = default(ArraySegment<byte>);
			}
		}

		private ArraySegment<byte> Detach(ref ServerAsyncEventArgs e, int size)
		{
			ServerAsyncEventArgs serverAsyncEventArgs = null;
			if (e.BytesTransferred > size)
			{
				serverAsyncEventArgs = e.CreateDeepCopy();
			}
			ArraySegment<byte> result = e.DetachBuffer();
			EventArgsManager.Put(ref e);
			if (serverAsyncEventArgs != null)
			{
				e = serverAsyncEventArgs;
			}
			return result;
		}

		private ArraySegment<byte> Dettach(Storage storage)
		{
			switch (storage)
			{
			case Storage.E1:
				return e1.DetachBuffer();
			case Storage.Buffer1:
				return buffer1.Detach();
			case Storage.Buffer2:
				return buffer2.Detach();
			case Storage.None:
				return default(ArraySegment<byte>);
			default:
				throw new ArgumentException();
			}
		}

		protected abstract void ResetParser(ResetReason reason);

		protected abstract void MessageReady();

		protected abstract ParseResult Parse(ArraySegment<byte> data);

		protected abstract void PreProcessRaw(ArraySegment<byte> data);

		private void SetReaderStorage(Storage readerStorage1, byte[] buffer, int offset, int count)
		{
			readerStorage = readerStorage1;
			headerData = new ArraySegment<byte>(buffer, offset, count);
		}

		private void SetReady(Storage contentStorage1, byte[] buffer, int offset, int count)
		{
			contentStorage = contentStorage1;
			contentData = new ArraySegment<byte>(buffer, offset, count);
			ready = true;
			MessageReady();
		}

		private void SetReady()
		{
			contentStorage = Storage.None;
			ready = true;
			MessageReady();
		}
	}
}
