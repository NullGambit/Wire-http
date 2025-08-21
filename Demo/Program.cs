using Wire.server;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();

		var result = await server.Run();
		
		
	}
}