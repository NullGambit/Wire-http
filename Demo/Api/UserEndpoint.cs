using Wire;
using Wire.Server;

namespace Api;

[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	// [Get(path = "{id}")]
	// public object Get(string id)
	// {
	// 	if (id == "john123")
	// 	{
	// 		return "user is john123";
	// 	}
	//
	// 	return new Result<string>("user not found", HttpStatusCode.BadRequest);
	// }
	
	// [Get]
	// public async Task Test()
	// {
	// 	await Task.Run(() => Console.WriteLine("test called!"));
	// }
	
	[Get]
	public async Task<Response> Test()
	{
		return new Response(HttpStatusCode.Accepted);
	}
}