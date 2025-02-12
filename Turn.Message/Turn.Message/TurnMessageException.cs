using System;

namespace Turn.Message
{
	public class TurnMessageException : Exception
	{
		public ErrorCode ErrorCode
		{
			get;
			set;
		}

		public TurnMessageException(ErrorCode errorCode)
		{
			ErrorCode = errorCode;
		}

		public TurnMessageException(ErrorCode errorCode, string message)
			: base(message)
		{
			ErrorCode = errorCode;
		}
	}
}
