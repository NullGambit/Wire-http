using System.Buffers;
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

	TcpListener _server;
	Config _config;
	Router.Router _router = new();

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

	public void IndexHandlers() => _router.IndexHandlers();

	async Task HandleClient(TcpClient c)
	{
		using var client = c;

		var buffer = ArrayPool<byte>.Shared.Rent(_config.bufferSize);
		
		try
		{
			var stream = client.GetStream();

			while (true)
			{
				var bytesRead = await stream.ReadAsync(buffer);

				if (bytesRead == 0)
				{
					break;
				}

				var request = await FrameParser.ParseRequestAsync(buffer[..bytesRead]);
				
				// TODO send an error response
				if (!request.HasValue)
				{
					SendResponse(client, stream, 
						new Response(HttpStatusCode.BadRequest, message: "Could not parse request frame"));
					return;
				}
				
				var r = request.Value;

				var (result, response) = await _router.RouteAndCall(r);
				
				if (result != RouteResult.Ok)
				{
					var status = RouteResultMethods.TranslateToHttpStatus(result);
					SendResponse(client, stream, new Response(status));
					return;
				}

				SendResponse(client, stream, response);

				return;
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	async Task SendResponse(TcpClient client, NetworkStream stream, Response response)
	{
		var responseMemory = await FrameWriter.WriteResponse(response);
				
		await stream.WriteAsync(responseMemory.GetBuffer()[ .. (Index)responseMemory.Length]);
		
		client.Dispose();
	}
}