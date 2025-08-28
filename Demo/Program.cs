using Api;
using Wire.Server;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();
		
		server.router.AddDependancy(new Counter());
		
		server.router.IndexHandlers();
	
		var result = await server.Run();
	}
}