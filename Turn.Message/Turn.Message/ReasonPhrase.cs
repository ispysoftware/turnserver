namespace Turn.Message
{
	public static class ReasonPhrase
	{
		public static string GetReasonPhrase(this ErrorCode errorCode)
		{
			switch (errorCode)
			{
			case ErrorCode.BadRequest:
				return "Bad Request";
			case ErrorCode.Unauthorized:
				return "Unauthorized";
			case ErrorCode.UnknownAttribute:
				return "Unknown Attribute";
			case ErrorCode.StaleCredentials:
				return "Stale Credentials";
			case ErrorCode.IntegrityCheckFailure:
				return "Integrity Check Failure";
			case ErrorCode.MissingUsername:
				return "Missing Username";
			case ErrorCode.MissingRealm:
				return "Missing Realm";
			case ErrorCode.MissingNonce:
				return "Missing Nonce";
			case ErrorCode.UnknownUsername:
				return "Unknown Username";
			case ErrorCode.NoBinding:
				return "No Binding";
			case ErrorCode.StaleNonce:
				return "Stale Nonce";
			case ErrorCode.Transitioning:
				return "Transitioning";
			case ErrorCode.NoDestination:
				return "No Destination";
			case ErrorCode.WrongUsername:
				return "Wrong Username";
			case ErrorCode.UseTLS:
				return "Use TLS";
			case ErrorCode.ServerError:
				return "Server Error";
			case ErrorCode.GlobalFailure:
				return "Global Failure";
			default:
				return "";
			}
		}
	}
}
