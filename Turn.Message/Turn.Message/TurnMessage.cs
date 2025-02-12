using System;
using System.Security.Cryptography;
using System.Text;

namespace Turn.Message
{
	public class TurnMessage
	{
		private const int HeaderLength = 20;

		private int messageIntegrityStartOffset = -1;

		private int fingerprintStartOffset = -1;

		private byte[] storedBytes = null;

		private Attribute[] allAttributes = null;

		public MessageType MessageType
		{
			get;
			set;
		}

		public ushort MessageLength
		{
			get;
			set;
		}

		public TransactionId TransactionId
		{
			get;
			set;
		}

		public AlternateServer AlternateServer
		{
			get;
			set;
		}

		public Bandwidth Bandwidth
		{
			get;
			set;
		}

		public Data Data
		{
			get;
			set;
		}

		public DestinationAddress DestinationAddress
		{
			get;
			set;
		}

		public ErrorCodeAttribute ErrorCodeAttribute
		{
			get;
			set;
		}

		public Fingerprint Fingerprint
		{
			get;
			set;
		}

		public Lifetime Lifetime
		{
			get;
			set;
		}

		public MagicCookie MagicCookie
		{
			get;
			set;
		}

		public MappedAddress MappedAddress
		{
			get;
			set;
		}

		public MessageIntegrity MessageIntegrity
		{
			get;
			set;
		}

		public MsVersion MsVersion
		{
			get;
			set;
		}

		public MsSequenceNumber MsSequenceNumber
		{
			get;
			set;
		}

		public Nonce Nonce
		{
			get;
			set;
		}

		public Realm Realm
		{
			get;
			set;
		}

		public RemoteAddress RemoteAddress
		{
			get;
			set;
		}

		public Software Software
		{
			get;
			set;
		}

		public UnknownAttributes UnknownAttributes
		{
			get;
			set;
		}

		public Username Username
		{
			get;
			set;
		}

		public XorMappedAddress XorMappedAddress
		{
			get;
			set;
		}

		public MsUsername MsUsername
		{
			get;
			set;
		}

		public ChangedAddress ChangedAddress
		{
			get;
			set;
		}

		public ChangeRequest ChangeRequest
		{
			get;
			set;
		}

		public ResponseAddress ResponseAddress
		{
			get;
			set;
		}

		public SourceAddress SourceAddress
		{
			get;
			set;
		}

		public ReflectedFrom ReflectedFrom
		{
			get;
			set;
		}

		public Password Password
		{
			get;
			set;
		}

		public bool IsAttributePaddingDisabled
		{
			private get;
			set;
		}

		public int TotalMessageLength => MessageLength + 20;

		public static TurnMessage Parse(byte[] bytes, TurnMessageRfc rfc)
		{
			return Parse(bytes, 0, bytes.Length, rfc);
		}

		public static TurnMessage Parse(byte[] bytes, int length, TurnMessageRfc rfc)
		{
			return Parse(bytes, 0, length, rfc);
		}

		public static TurnMessage Parse(byte[] bytes, int startIndex, int length, TurnMessageRfc rfc)
		{
			TurnMessage turnMessage = new TurnMessage();
			turnMessage.ParseHeader(bytes, startIndex, length);
			turnMessage.ParseAttributes(bytes, startIndex, length, rfc);
			return turnMessage;
		}

		public static MessageType? SafeGetMessageType(byte[] bytes, int startIndex, int length)
		{
			if (length < 2)
			{
				return null;
			}
			ushort num = bytes.BigendianToUInt16(startIndex);
			if (!Enum.IsDefined(typeof(MessageType), (int)num))
			{
				return null;
			}
			return (MessageType)num;
		}

		public static TransactionId SafeGetTransactionId(byte[] bytes, int startIndex, int length)
		{
			TransactionId result = null;
			try
			{
				if (length >= 20)
				{
					result = new TransactionId(bytes, startIndex + 4);
				}
			}
			catch
			{
			}
			return result;
		}

		public static bool IsTurnMessage(byte[] bytes, int startIndex, int length)
		{
			if (length < 28)
			{
				return false;
			}
			if (bytes.BigendianToUInt16(20 + startIndex) != 15)
			{
				return false;
			}
			if (bytes.BigendianToUInt16(20 + startIndex + 2) != 4)
			{
				return false;
			}
			if (bytes.BigendianToUInt32(20 + startIndex + 4) != 1925598150)
			{
				return false;
			}
			return true;
		}

		public byte[] GetBytes(byte[] key2)
		{
			return GetBytes(null, 0, null, key2, CreditalsType.MsAvedgea, 0);
		}

		public void GetBytes(byte[] bytes, int startIndex, byte[] key2)
		{
			GetBytes(bytes, startIndex, null, key2, CreditalsType.MsAvedgea, 0);
		}

		public byte[] GetBytes(string password, bool longOrShortTerm)
		{
			return GetBytes(null, 0, password, null, (!longOrShortTerm) ? CreditalsType.ShortTerm : CreditalsType.LongTerm, 0);
		}

		public byte[] GetBytes(string password, bool longOrShortTerm, byte paddingByte)
		{
			return GetBytes(null, 0, password, null, (!longOrShortTerm) ? CreditalsType.ShortTerm : CreditalsType.LongTerm, paddingByte);
		}

		public bool IsMessageIntegrityValid(string password, bool longOrShortTerm)
		{
			if (MessageIntegrity == null)
			{
				return false;
			}
			byte[] array = ComputeMessageIntegrity(password, longOrShortTerm);
			return array.AreArraysEqual(MessageIntegrity.Value);
		}

		public bool IsValidMessageIntegrity(byte[] key2)
		{
			if (MessageIntegrity == null)
			{
				return false;
			}
			if (MsUsername == null)
			{
				return false;
			}
			byte[] array = ComputeMessageIntegrity(key2);
			return array.AreArraysEqual(MessageIntegrity.Value);
		}

		public bool IsValidMsUsername(byte[] key1)
		{
			if (MsUsername == null)
			{
				return false;
			}
			using (HMACSHA1 hMACSHA = new HMACSHA1(key1))
			{
				hMACSHA.ComputeHash(MsUsername.Value, 0, MsUsername.TokenBlobLength);
				return hMACSHA.Hash.AreArraysEqual(MsUsername.Value, MsUsername.TokenBlobLength, 20);
			}
		}

		public byte[] ComputeMsPasswordBytes(byte[] key2)
		{
			return ComputeMsPasswordBytes(key2, MsUsername.Value);
		}

		public static byte[] ComputeMsPasswordBytes(byte[] key2, byte[] msUsername)
		{
			using (HMACSHA1 hMACSHA = new HMACSHA1(key2))
			{
				hMACSHA.ComputeHash(msUsername);
				return hMACSHA.Hash;
			}
		}

		public byte[] ComputeMessageIntegrity(string password, bool longOrShortTerm)
		{
			if (longOrShortTerm)
			{
				return ComputeLongTermMessageIntegrity(storedBytes, messageIntegrityStartOffset, password);
			}
			return ComputeShortTermMessageIntegrity(storedBytes, messageIntegrityStartOffset, password);
		}

		public byte[] ComputeMessageIntegrity(byte[] key2)
		{
			return ComputeMsAvedgeaMessageIntegrity(storedBytes, 0, storedBytes.Length, key2, Realm.Value, MsUsername.Value);
		}

		public uint ComputeFingerprint()
		{
			return ComputeFingerprint(storedBytes, fingerprintStartOffset);
		}

		public bool IsRfc3489(AttributeType attributeType)
		{
			switch (attributeType)
			{
			case AttributeType.ResponseAddress:
			case AttributeType.ChangeRequest:
			case AttributeType.SourceAddress:
			case AttributeType.ChangedAddress:
			case AttributeType.Password:
			case AttributeType.ReflectedFrom:
				return true;
			default:
				return false;
			}
		}

		public bool IsAttributePaddingEnabled()
		{
			return MsVersion == null && !IsAttributePaddingDisabled;
		}

		private byte[] GetBytes(byte[] bytes, int startIndex, string password, byte[] key2, CreditalsType creditalsType, byte paddingByte)
		{
			if (bytes == null)
			{
				ComputeMessageLength();
				bytes = new byte[MessageLength + 20];
			}
			GetHeaderBytes(bytes, ref startIndex);
			GetAttributesBytes(bytes, ref startIndex, password, key2, creditalsType, paddingByte);
			return bytes;
		}

		private void CreateAttributesArray()
		{
			allAttributes = new Attribute[26]
			{
				MagicCookie,
				MsVersion,
				AlternateServer,
				Bandwidth,
				RemoteAddress,
				Data,
				DestinationAddress,
				ErrorCodeAttribute,
				Lifetime,
				MappedAddress,
				Software,
				UnknownAttributes,
				Username,
				MsUsername,
				Nonce,
				Realm,
				XorMappedAddress,
				MsSequenceNumber,
				ChangedAddress,
				ChangeRequest,
				ResponseAddress,
				SourceAddress,
				ReflectedFrom,
				Password,
				MessageIntegrity,
				Fingerprint
			};
		}

		public void ComputeMessageLength()
		{
			CreateAttributesArray();
			MessageLength = 0;
			Attribute[] array = allAttributes;
			foreach (Attribute attribute in array)
			{
				if (!(attribute?.Ignore ?? true))
				{
					MessageLength += attribute.TotalLength;
					if (IsAttributePaddingEnabled() && (int)MessageLength % 4 > 0)
					{
						MessageLength += (ushort)(4 - (int)MessageLength % 4);
					}
				}
			}
		}

		private void GetAttributesBytes(byte[] bytes, ref int startIndex, string password, byte[] key2, CreditalsType creditalsType, byte paddingByte)
		{
			int startIndex2 = startIndex - 20;
			Attribute[] array = allAttributes;
			foreach (Attribute attribute in array)
			{
				if (attribute?.Ignore ?? true)
				{
					continue;
				}
				if (attribute is MessageIntegrity)
				{
					switch (creditalsType)
					{
					case CreditalsType.ShortTerm:
						(attribute as MessageIntegrity).Value = ComputeShortTermMessageIntegrity(bytes, startIndex, password);
						break;
					case CreditalsType.LongTerm:
						(attribute as MessageIntegrity).Value = ComputeLongTermMessageIntegrity(bytes, startIndex, password);
						break;
					case CreditalsType.MsAvedgea:
						(attribute as MessageIntegrity).Value = ComputeMsAvedgeaMessageIntegrity(bytes, startIndex2, startIndex, key2, Realm.Value, MsUsername.Value);
						break;
					}
				}
				if (attribute is Fingerprint)
				{
					(attribute as Fingerprint).Value = ComputeFingerprint(bytes, startIndex);
				}
				if (attribute is XorMappedAddress)
				{
					(attribute as XorMappedAddress).GetBytes(bytes, ref startIndex, TransactionId);
				}
				else
				{
					attribute.GetBytes(bytes, ref startIndex);
				}
				if (IsAttributePaddingEnabled() && startIndex % 4 > 0)
				{
					if (paddingByte != 0)
					{
						PadAttribute(paddingByte, bytes, startIndex);
					}
					startIndex += 4 - startIndex % 4;
				}
			}
		}

		private void PadAttribute(byte paddingByte, byte[] bytes, int startIndex)
		{
			while (startIndex % 4 > 0)
			{
				bytes[startIndex] = paddingByte;
				startIndex++;
			}
		}

		private void GetHeaderBytes(byte[] bytes, ref int startIndex)
		{
			Array.Copy(((ushort)MessageType).GetBigendianBytes(), 0, bytes, startIndex, 2);
			Array.Copy(MessageLength.GetBigendianBytes(), 0, bytes, startIndex + 2, 2);
			Array.Copy(TransactionId.Value, 0, bytes, startIndex + 4, 16);
			startIndex += 20;
		}

		private void ParseHeader(byte[] bytes, int startIndex, int length)
		{
			if (length < 20)
			{
				throw new TurnMessageException(ErrorCode.BadRequest, "Too short message, less than 20 bytes (header size)");
			}
			ushort num = bytes.BigendianToUInt16(startIndex);
			if ((num & 0xC000) != 0)
			{
				throw new TurnMessageException(ErrorCode.BadRequest, "The most significant two bits of Message Type MUST be set to zero");
			}
			if (!Enum.IsDefined(typeof(MessageType), (int)num))
			{
				throw new TurnMessageException(ErrorCode.BadRequest, "Unknow message type");
			}
			MessageType = (MessageType)num;
			MessageLength = bytes.BigendianToUInt16(startIndex + 2);
			if (MessageLength != length - 20)
			{
				throw new TurnMessageException(ErrorCode.BadRequest, $"Wrong message length, wait for {length - 20} actual is {MessageLength}");
			}
			TransactionId = new TransactionId(bytes, startIndex + 4);
		}

		private void ParseAttributes(byte[] bytes, int startIndex, int length, TurnMessageRfc rfc)
		{
			int startIndex2 = startIndex + 20;
			int num = startIndex + length;
			while (true)
			{
				if (startIndex2 >= num)
				{
					return;
				}
				ushort num2 = bytes.BigendianToUInt16(startIndex2);
				if (!Enum.IsDefined(typeof(AttributeType), (int)num2))
				{
					throw new TurnMessageException(ErrorCode.UnknownAttribute);
				}
				AttributeType attributeType = (AttributeType)num2;
				if (rfc == TurnMessageRfc.Rfc3489 && IsRfc3489(attributeType))
				{
					break;
				}
				if (attributeType == AttributeType.Fingerprint)
				{
					if (Fingerprint == null)
					{
						if (storedBytes == null)
						{
							storedBytes = new byte[length];
							Array.Copy(bytes, startIndex, storedBytes, 0, length);
						}
						fingerprintStartOffset = startIndex2 - startIndex;
						Fingerprint = new Fingerprint();
						Fingerprint.Parse(bytes, ref startIndex2);
					}
					else
					{
						Attribute.Skip(bytes, ref startIndex2);
					}
				}
				else if (MessageIntegrity != null)
				{
					Attribute.Skip(bytes, ref startIndex2);
				}
				else
				{
					switch (attributeType)
					{
					case AttributeType.AlternateServer:
						AlternateServer = new AlternateServer();
						AlternateServer.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Bandwidth:
						Bandwidth = new Bandwidth();
						Bandwidth.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Data:
						Data = new Data();
						Data.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.DestinationAddress:
						DestinationAddress = new DestinationAddress();
						DestinationAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.ErrorCode:
						ErrorCodeAttribute = new ErrorCodeAttribute();
						ErrorCodeAttribute.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Lifetime:
						Lifetime = new Lifetime();
						Lifetime.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.MagicCookie:
						MagicCookie = new MagicCookie();
						MagicCookie.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.MappedAddress:
						MappedAddress = new MappedAddress();
						MappedAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.MessageIntegrity:
						messageIntegrityStartOffset = startIndex2 - startIndex;
						if (storedBytes == null)
						{
							if (rfc == TurnMessageRfc.MsTurn)
							{
								storedBytes = new byte[GetPadded64(messageIntegrityStartOffset)];
								Array.Copy(bytes, startIndex, storedBytes, 0, messageIntegrityStartOffset);
							}
							else
							{
								storedBytes = new byte[length];
								Array.Copy(bytes, startIndex, storedBytes, 0, length);
							}
						}
						MessageIntegrity = new MessageIntegrity();
						MessageIntegrity.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.MsVersion:
						MsVersion = new MsVersion();
						MsVersion.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.MsSequenceNumber:
						MsSequenceNumber = new MsSequenceNumber();
						MsSequenceNumber.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.RemoteAddress:
						RemoteAddress = new RemoteAddress();
						RemoteAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Software:
						Software = new Software();
						Software.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.UnknownAttributes:
						UnknownAttributes = new UnknownAttributes();
						UnknownAttributes.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Username:
						if (MsVersion != null)
						{
							MsUsername = new MsUsername();
							MsUsername.Parse(bytes, ref startIndex2);
						}
						else
						{
							Username = new Username();
							Username.Parse(bytes, ref startIndex2);
						}
						break;
					case AttributeType.Priority:
					case AttributeType.UseCandidate:
					case AttributeType.IceControlled:
					case AttributeType.IceControlling:
						Attribute.Skip(bytes, ref startIndex2);
						break;
					case AttributeType.ChangedAddress:
						ChangedAddress = new ChangedAddress();
						ChangedAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.ChangeRequest:
						ChangeRequest = new ChangeRequest();
						ChangeRequest.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.ResponseAddress:
						ResponseAddress = new ResponseAddress();
						ResponseAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.SourceAddress:
						SourceAddress = new SourceAddress();
						SourceAddress.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.ReflectedFrom:
						ReflectedFrom = new ReflectedFrom();
						ReflectedFrom.Parse(bytes, ref startIndex2);
						break;
					case AttributeType.Password:
						Password = new Password();
						Password.Parse(bytes, ref startIndex2);
						break;
					default:
						if (rfc == TurnMessageRfc.MsTurn)
						{
							switch (attributeType)
							{
							case AttributeType.Nonce:
								Nonce = new Nonce(rfc);
								Nonce.Parse(bytes, ref startIndex2);
								break;
							case AttributeType.Realm:
								Realm = new Realm(rfc);
								Realm.Parse(bytes, ref startIndex2);
								break;
							case AttributeType.XorMappedAddress:
								XorMappedAddress = new XorMappedAddress(rfc);
								XorMappedAddress.Parse(bytes, ref startIndex2, TransactionId);
								break;
							default:
								throw new NotImplementedException();
							}
						}
						else
						{
							switch (attributeType)
							{
							case AttributeType.Realm:
								Nonce = new Nonce(rfc);
								Nonce.Parse(bytes, ref startIndex2);
								break;
							case AttributeType.Nonce:
								Realm = new Realm(rfc);
								Realm.Parse(bytes, ref startIndex2);
								break;
							case AttributeType.XorMappedAddressStun:
								XorMappedAddress = new XorMappedAddress(rfc);
								XorMappedAddress.Parse(bytes, ref startIndex2, TransactionId);
								break;
							default:
								throw new NotImplementedException();
							}
						}
						break;
					case AttributeType.Fingerprint:
						break;
					}
				}
				if (rfc != TurnMessageRfc.MsTurn && startIndex2 % 4 > 0)
				{
					startIndex2 += 4 - startIndex2 % 4;
				}
			}
			throw new TurnMessageException(ErrorCode.UnknownAttribute);
		}

		protected static uint ComputeFingerprint(byte[] bytes, int length)
		{
			using (Crc32 crc = new Crc32())
			{
				crc.ComputeHash(bytes, 0, length);
				return crc.CrcValue ^ 0x5354554E;
			}
		}

		protected static byte[] ComputeMsAvedgeaMessageIntegrity(byte[] bytes, int startIndex, int stopIndex, byte[] key2, string realm1, byte[] msUsername)
		{
			UTF8Encoding uTF8Encoding = new UTF8Encoding();
			byte[] bytes2 = uTF8Encoding.GetBytes(":" + realm1 + ":");
			byte[] array = ComputeMsPasswordBytes(key2, msUsername);
			byte[] array2 = new byte[msUsername.Length + bytes2.Length + array.Length];
			Array.Copy(msUsername, 0, array2, 0, msUsername.Length);
			Array.Copy(bytes2, 0, array2, msUsername.Length, bytes2.Length);
			Array.Copy(array, 0, array2, array2.Length - array.Length, array.Length);
			using (MD5 mD = MD5.Create())
			{
				using (HMACSHA1 hMACSHA = new HMACSHA1(mD.ComputeHash(array2)))
				{
					int padded = GetPadded64(stopIndex - startIndex);
					byte[] array3;
					if (startIndex + padded <= bytes.Length)
					{
						for (int i = stopIndex; i < startIndex + padded; i++)
						{
							bytes[i] = 0;
						}
						array3 = bytes;
					}
					else
					{
						if (startIndex > 0)
						{
							throw new NotImplementedException();
						}
						array3 = new byte[GetPadded64(bytes.Length)];
						Array.Copy(bytes, array3, bytes.Length);
					}
					return hMACSHA.ComputeHash(array3, startIndex, padded);
				}
			}
		}

		protected byte[] ComputeShortTermMessageIntegrity(byte[] bytes, int length, string password)
		{
			UTF8Encoding uTF8Encoding = new UTF8Encoding();
			byte[] bytes2 = uTF8Encoding.GetBytes(password);
			return ComputeMessageIntegritySha1(bytes, length, bytes2);
		}

		protected byte[] ComputeLongTermMessageIntegrity(byte[] bytes, int length, string password)
		{
			UTF8Encoding uTF8Encoding = new UTF8Encoding();
			using (MD5 mD = MD5.Create())
			{
				byte[] sha1Key = mD.ComputeHash(uTF8Encoding.GetBytes(Username.Value + ":" + Realm.Value + ":" + password.SASLprep()));
				return ComputeMessageIntegritySha1(bytes, length, sha1Key);
			}
		}

		protected static byte[] ComputeMessageIntegritySha1(byte[] bytes, int length, byte[] sha1Key)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			if (bytes.Length < 4)
			{
				throw new ArgumentException("Too short array", "bytes");
			}
			byte b = bytes[2];
			byte b2 = bytes[3];
			try
			{
				((ushort)(length - 20 + 24)).GetBigendianBytes().CopyTo(bytes, 2);
				using (HMACSHA1 hMACSHA = new HMACSHA1(sha1Key))
				{
					return hMACSHA.ComputeHash(bytes, 0, length);
				}
			}
			finally
			{
				bytes[2] = b;
				bytes[3] = b2;
			}
		}

		private static int GetPadded64(int value)
		{
			return (value + 63) / 64 * 64;
		}
	}
}
