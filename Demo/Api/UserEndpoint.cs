using Wire;
using Wire.Server;

namespace Api;

[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	[Get("{id}")]
	public async Task<string> Get(int id)
	{
		return $"got your id as {id}";
	}
}