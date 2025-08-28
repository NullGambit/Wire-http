using Wire;
using Wire.Server;

namespace Api;

[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	Counter _counter;
	
	public UserEndpoint(Counter counter)
	{
		_counter = counter;
	}
	
	[Get("{id}_{username}")]
	public async Task<string> Get(int id, string username, Request request)
	{
		_counter.counter++;
		
		return $"got your id as {id} you are {username}. got request to {request.path}, req_count: {_counter.counter}";
	}
}