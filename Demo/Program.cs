using System.Text;
using Api;
using Wire;
using Wire.Server;
using Wire.Server.Middleware;
using Wire.Server.Router;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();
		
		server.router.staticFileManager.Index("./static", "*.html");
		
		server.router.AddDependency(new Counter());

		var requestCount = 0;

		server.Use(async (ctx, next) =>
		{
			requestCount++;
			Console.WriteLine($"{requestCount} requests made");
			next(ctx);
		});

		server.router.IndexHandlers();

		await server.Run();
	}
}