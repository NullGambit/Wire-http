namespace Wire;

public record Request
(
	HttpMethod method = HttpMethod.Get,
	string path = "",
	string version = "",
	Dictionary<string, string>? headers = null
)
{
	public ulong ContentLength 
	{
		get 
		{
			var str = headers["Content-Length"];

			ulong.TryParse(str, out var contentLength);

			return contentLength;
		}
	}
	
	public bool KeepAlive 
	{
		get 
		{
			var value = headers["Connection"];

			return value == "keep-alive";
		}
	}
	
	public string GetBody()
	{
		return null;
	}

	public byte[] GetBodyAsBytes()
	{
		return null;
	}
}