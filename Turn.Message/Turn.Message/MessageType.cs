namespace Turn.Message
{
	public enum MessageType
	{
		AllocateRequest = 3,
		AllocateResponse = 259,
		AllocateErrorResponse = 275,
		SendRequest = 4,
		DataIndication = 277,
		SetActiveDestinationRequest = 6,
		SetActiveDestinationResponse = 262,
		SetActiveDestinationErrorResponse = 278,
		SendRequestResponse = 260,
		SendRequestErrorResponse = 276,
		SharedSecretRequest = 2,
		SharedSecretResponse = 258,
		SharedSecretErrorResponse = 274,
		BindingRequest = 1,
		BindingResponse = 257,
		BindingErrorResponse = 273
	}
}
