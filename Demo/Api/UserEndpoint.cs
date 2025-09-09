using Wire;
using Wire.Server;

namespace Api;

[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	Counter _counter;
	Dictionary<string, string> _userTable;
	
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

	[Post("create/{username}")]
	async Task<Response> CreateUser(string username, Request request)
	{
		if (_userTable.ContainsKey(username))
		{
			return new Response(HttpStatusCode.BadRequest, message: "a user by that username already exists");
		}
		
		

		return new Response(message: "created new user");
	}
}