using System.Buffers;

namespace Wire.server;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public enum RunResult
{
	Ok,
	IpAddressParseError,
}

public class Server
{
	public static readonly Config DefaultConfig = new Config(port: 8080);
	public bool shouldRun = true;

	TcpListener _server;
	Config _config;
	
	// blocks and handles requests until server stops
	public async Task<RunResult> Run(Config? config = null)
	{
		_config = config ?? DefaultConfig;

		var ok = IPAddress.TryParse("127.0.0.1", out var localAddress);

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

			_ = HandleClient(client);
		}

		return RunResult.Ok;
	}

	async Task HandleClient(TcpClient c)
	{
		Task.Delay(500);
		
		using var client = c;

		var buffer = ArrayPool<byte>.Shared.Rent(_config.bufferSize);
		
		try
		{
			var stream = client.GetStream();

			var bytesRead = await stream.ReadAsync(buffer);

			if (bytesRead == 0)
			{
				return;
			}
			
			var txt = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			
			Console.WriteLine(txt);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}