using Api;
using Wire;
using Wire.Server;
using Wire.Server.Middleware;

namespace Demo;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new Server();
		
		server.router.AddDependency(new Counter());

		var requestCount = 0;

		server.Use(async (ctx, next) =>
		{
			requestCount++;
			Console.WriteLine($"{requestCount} requests made");
			await ctx.SendResponse(new Response(HttpStatusCode.OK));
			// next(ctx);
		});
		
		server.router.IndexHandlers();
		
		var result = await server.Run();

		// var pipeline = new MiddlewarePipeline();
		//
		// pipeline
		// 	.Use(async (ctx, next) =>
		// 	{
		// 		Console.WriteLine("A");
		// 		next(ctx);
		// 	})
		// 	.Use(async (ctx, next) =>
		// 	{
		// 		Console.WriteLine("B");
		// 		next(ctx);
		// 	})
		// 	.Use(async (ctx, next) =>
		// 	{
		// 		Console.WriteLine("C");
		// 		next(ctx);
		// 	});
		//
		// var handler = pipeline.Build();
		//
		// await handler(new MiddlewareContext());
	}
}