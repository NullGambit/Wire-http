using System.Text;
using Wire;
using Wire.Server;

namespace Api;

[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	readonly Dictionary<string, string> _userTable = [];
	
	[Get("{username}")]
	public async Task<Response> Get(string username)
	{
		foreach (var (key, v) in _userTable)
		{
			Console.WriteLine($"{key} = {v}");
		}
		
		var exists = _userTable.TryGetValue(username, out var value);

		if (!exists)
		{
			return new Response(HttpStatusCode.BadRequest, message: "user not found");
		}

		return new Response(body: Encoding.UTF8.GetBytes(value));
	}

	[Post("create/{username}")]
	async Task<Response> CreateUser(string username, Request request)
	{
		if (_userTable.ContainsKey(username))
		{
			return new Response(HttpStatusCode.BadRequest, message: "a user by that username already exists");
		}
		
		_userTable[username] = await request.GetBody();
		
		return new Response(message: "created new user");
	}
}