using System.Net;

public static class IpEndPointExt
{
	public static void CopyFrom(this IPEndPoint to, IPEndPoint from)
	{
		to.Address = from.Address;
		to.Port = from.Port;
	}

	public static IPEndPoint MakeCopy(this IPEndPoint from)
	{
		return new IPEndPoint(from.Address, from.Port);
	}

	public static bool IsEqual(this IPEndPoint ip1, IPEndPoint ip2)
	{
		return ip1.AddressFamily == ip2.AddressFamily && ip1.Port == ip2.Port && ip1.Address.Equals(ip2.Address);
	}
}
