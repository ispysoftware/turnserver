using System;

namespace Turn.Message
{
	public static class MessageTypeHelpers
	{
		public static MessageType GetErrorResponseType(this MessageType requestType)
		{
			switch (requestType)
			{
			case MessageType.AllocateRequest:
				return MessageType.AllocateErrorResponse;
			case MessageType.SendRequest:
				return MessageType.SendRequestErrorResponse;
			case MessageType.BindingRequest:
				return MessageType.BindingErrorResponse;
			case MessageType.SetActiveDestinationRequest:
				return MessageType.SetActiveDestinationErrorResponse;
			case MessageType.SharedSecretRequest:
				return MessageType.SharedSecretErrorResponse;
			default:
				throw new NotImplementedException();
			}
		}
	}
}
