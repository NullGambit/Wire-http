using Wire.Server;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();
		
		server.IndexHandlers();
	
		var result = await server.Run();
	}
}