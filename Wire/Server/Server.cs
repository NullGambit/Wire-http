using System.Buffers;
using Wire.Server.Middleware;
using Wire.Server.Router;


namespace Wire.Server;

using System;
using Net = System.Net;
using System.Net.Sockets;
using Wire;

public enum RunResult
{
	Ok,
	IpAddressParseError,
}

public class Server
{
	public static readonly Config DefaultConfig = new (port: 8080);
	public bool shouldRun = true;
	public readonly Router.Router router = new();
	public delegate Task OnRouteFailDelegate(RouteResult routeResult, Request request, NetworkStream stream);
	
	public OnRouteFailDelegate onRouteFail;
	
	TcpListener _server;
	Config _config;
	readonly MiddlewarePipeline _middlewarePipeline = new();

	public Server()
	{
		onRouteFail = async (result, _, stream) =>
		{
			var status = RouteResultMethods.TranslateToHttpStatus(result);
			await SendResponse(stream, new Response(status));
		};
	}
	
	// a helper over to shorten adding middlewares
	public Server Use(MiddlewarePipeline.Middleware middleware)
	{
		_middlewarePipeline.Use(middleware);
		return this;
	}

	// blocks and handles requests until server stops
	public async Task<RunResult> Run(Config? config = null)
	{
		_config = config ?? DefaultConfig;

		var ok = Net.IPAddress.TryParse("127.0.0.1", out var localAddress);

		if (!ok)
		{
			return RunResult.IpAddressParseError;
		}

		_server = new TcpListener(localAddress, _config.port);

		_server.Start();

		while (shouldRun)
		{
			Console.WriteLine("Listening for connections");

			var client = await _server.AcceptTcpClientAsync();
			
			HandleClient(client);
		}

		return RunResult.Ok;
	}

	async Task HandleClient(TcpClient c)
	{
		using var client = c;

		try
		{
			var stream = client.GetStream();

			while (true)
			{
				var request = await FrameParser.ParseRequestAsync(stream);
				
				// TODO send an error response
				if (request == null)
				{
					await SendResponse(stream, 
						new Response(HttpStatusCode.BadRequest, message: "Could not parse request frame"));
					return;
				}

				var (routeResult, routeObject) = await router.Route(request);
				
				if (routeResult != RouteResult.Ok)
				{
					await onRouteFail(routeResult, request, stream);
					return;
				}
				
				var ctx = new MiddlewareContext
				{
					request = request,
					pipelineData = [],
					_server = this,
					_stream = stream,
					_deps = router._handlerDeps
				};

				var pipeline = _middlewarePipeline.Clone();

				pipeline.Use(async (_, _) =>
				{
					var (callResult, response) = await router.CallHandler(routeObject, request);
				
					if (callResult != RouteResult.Ok)
					{
						await onRouteFail(routeResult, request, stream);
						return;
					}

					await SendResponse(stream, response);
				});

				var handler = pipeline.Build();

				await handler(ctx);

				return;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"Server caught an exception {e}");
		}
	}

	async public Task SendResponse(NetworkStream stream, Response response)
	{
		var responseMemory = await FrameWriter.WriteResponse(response);
				
		await stream.WriteAsync(responseMemory.GetBuffer()[ .. (Index)responseMemory.Length]);
	}
}