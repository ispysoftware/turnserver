namespace Turn.Message
{
	public enum AttributeType
	{
		MappedAddress = 1,
		Username = 6,
		MessageIntegrity = 8,
		ErrorCode = 9,
		UnknownAttributes = 10,
		Lifetime = 13,
		AlternateServer = 14,
		MagicCookie = 0xF,
		Bandwidth = 0x10,
		DestinationAddress = 17,
		RemoteAddress = 18,
		Data = 19,
		Nonce = 20,
		Realm = 21,
		XorMappedAddress = 32800,
		XorMappedAddressStun = 0x20,
		RealmStun = 20,
		NonceStun = 21,
		MsVersion = 32776,
		MsSequenceNumber = 32848,
		MsServiceQuality = 32853,
		Software = 32802,
		Fingerprint = 32808,
		Priority = 36,
		UseCandidate = 37,
		IceControlled = 32809,
		IceControlling = 32810,
		ResponseAddress = 2,
		ChangeRequest = 3,
		SourceAddress = 4,
		ChangedAddress = 5,
		Password = 7,
		ReflectedFrom = 11
	}
}
