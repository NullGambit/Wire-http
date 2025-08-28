using Api;
using Wire.Server;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();
		
		server.router.AddDependency(new Counter());
		
		server.router.IndexHandlers();
	
		var result = await server.Run();
	}
}