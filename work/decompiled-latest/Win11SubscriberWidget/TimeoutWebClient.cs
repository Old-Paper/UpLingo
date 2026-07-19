using System;
using System.Net;

namespace Win11SubscriberWidget;

internal class TimeoutWebClient : WebClient
{
	public int TimeoutMilliseconds { get; set; }

	protected override WebRequest GetWebRequest(Uri address)
	{
		WebRequest webRequest = base.GetWebRequest(address);
		webRequest.Timeout = ((TimeoutMilliseconds <= 0) ? 10000 : TimeoutMilliseconds);
		if (webRequest is HttpWebRequest httpWebRequest)
		{
			httpWebRequest.ReadWriteTimeout = ((TimeoutMilliseconds <= 0) ? 10000 : TimeoutMilliseconds);
			httpWebRequest.KeepAlive = false;
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
		}
		return webRequest;
	}
}
