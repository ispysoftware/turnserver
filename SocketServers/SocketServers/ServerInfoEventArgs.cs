using System;
using System.Net.Sockets;

namespace SocketServers
{
	public class ServerInfoEventArgs : ServerChangeEventArgs
	{
		public SocketError SocketError
		{
			get;
			private set;
		}

		public Exception Exception
		{
			get;
			private set;
		}

		public string Error
		{
			get;
			private set;
		}

		internal ServerInfoEventArgs(ServerEndPoint serverEndPoint, SocketError error)
			: base(serverEndPoint)
		{
			SocketError = error;
		}

		internal ServerInfoEventArgs(ServerEndPoint serverEndPoint, Exception error)
			: base(serverEndPoint)
		{
			Exception = error;
		}

		internal ServerInfoEventArgs(ServerEndPoint serverEndPoint, SocketException error)
			: base(serverEndPoint)
		{
			SocketError = error.SocketErrorCode;
			Exception = error;
		}

		internal ServerInfoEventArgs(ServerEndPoint serverEndPoint, string error)
			: base(serverEndPoint)
		{
			Error = error;
		}

		internal ServerInfoEventArgs(ServerEndPoint serverEndPoint, string api, string function, uint error)
			: base(serverEndPoint)
		{
			Error = Format(api, function, error);
		}

		internal static string Format(string api, string function, uint error)
		{
			return $"{api} error, function call {function} return 0x{error:x8}";
		}

		public override string ToString()
		{
			if (Error != null)
			{
				return Error;
			}
			if (Exception != null)
			{
				return Exception.ToString();
			}
			return "SocketError :" + SocketError.ToString();
		}
	}
}
