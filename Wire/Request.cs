using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace Wire;

public record Request
(
	HttpMethod method = HttpMethod.Get,
	string path = "",
	string version = "",
	Dictionary<string, string>? headers = null
)
{
	internal NetworkStream _stream;
	internal byte[] _bodyBuff;
	
	public int ContentLength 
	{
		get 
		{
			var str = headers["Content-Length"];

			int.TryParse(str, out var contentLength);

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
	
	public async Task<string> GetBody()
	{
		return Encoding.UTF8.GetString(await GetBodyAsBytes());
	}

	public async Task<byte[]> GetBodyAsBytes()
	{
		if (_bodyBuff.Length >= ContentLength)
		{
			return _bodyBuff[..ContentLength];
		}
		
		var diff = _bodyBuff.Length - ContentLength;

		diff = Math.Abs(diff);

		var buff = ArrayPool<byte>.Shared.Rent(ContentLength);
		
		_bodyBuff.CopyTo(buff, 0);

		await _stream.ReadAtLeastAsync(buff[_bodyBuff.Length..], diff);
		
		return buff;
	}
}