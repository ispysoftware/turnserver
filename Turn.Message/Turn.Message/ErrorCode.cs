namespace Turn.Message
{
	public enum ErrorCode
	{
		BadRequest = 400,
		Unauthorized = 401,
		UnknownAttribute = 420,
		StaleCredentials = 430,
		IntegrityCheckFailure = 431,
		MissingUsername = 432,
		MissingRealm = 434,
		MissingNonce = 435,
		UnknownUsername = 436,
		NoBinding = 437,
		StaleNonce = 438,
		Transitioning = 439,
		NoDestination = 440,
		WrongUsername = 441,
		UseTLS = 433,
		ServerError = 500,
		GlobalFailure = 600
	}
}
