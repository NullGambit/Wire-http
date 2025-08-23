using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Wire.Server;

using System;
using System.Net;
using System.Net.Sockets;
using WireStatus = Wire.Common.HttpStatusCode;

using Common;

public enum RunResult
{
	Ok,
	IpAddressParseError,
}

internal struct HandlerData
{
	public Type ownerType;
	public object obj;
	public ObjectMethodExecutor executor;
	public MethodInfo methodInfo;
	public HttpMethod httpMethod;
	public bool isAsync;
}

public class Server
{
	public static readonly Config DefaultConfig = new (port: 8080);
	public bool shouldRun = true;

	TcpListener _server;
	Config _config;
	Dictionary<string, HandlerData> _handlers = [];

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
			
			HandleClient(client);
		}

		return RunResult.Ok;
	}

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
					await SimpleResponse(client, stream, WireStatus.BadRequest, "Could not parse request frame");
					return;
				}
				
				var r = request.Value;
				
				var exists = _handlers.TryGetValue(r.path, out var handler);
				
				// TODO send an error response
				if (!exists)
				{
					await SimpleResponse(client, stream, WireStatus.NotFound, $"Invalid path {r.path}");
					return;
				}

				if (handler.isAsync)
				{
					await handler.executor.ExecuteAsync(handler.obj, null);
				}
				else
				{
					handler.executor.Execute(handler.obj, null);
				}

				await SimpleResponse(client, stream, WireStatus.OK);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	async Task SimpleResponse(TcpClient client, NetworkStream stream, WireStatus status, string? message = null)
	{
		var headers = new Dictionary<string, string>
		{
			{ "Server", "Wire" }
		};
		
		var response = new Response(status, headers, message: message);

		var responseMemory = await FrameWriter.WriteResponse(response);
				
		Console.WriteLine(Encoding.UTF8.GetString(responseMemory.GetBuffer()));

		await stream.WriteAsync(responseMemory.GetBuffer());
	}

	public void IndexHandlers()
	{
		var assembly = Assembly.GetEntryAssembly();
		
		var handlerTypes = assembly.GetTypes()
			.Where(t => t.GetCustomAttribute<EndpointAttribute>() != null);

		foreach (var type in handlerTypes)
		{
			var endpointAttr = type.GetCustomAttribute<EndpointAttribute>();
			var routeAttr = type.GetCustomAttribute<RouteAttribute>();
			string route;

			if (routeAttr != null)
			{
				route = routeAttr.path.Replace("{endpoint}", GetFormattedTypeName(type.Name));
			}
			else
			{
				route = GetRouteFromType(type);
			}

			route = CorrectPath(route);
			
			var handlerMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(m => m.GetCustomAttribute<HandlerMethodAttribute>(true) != null);
			
			foreach (var method in handlerMethods)
			{
				var handlerAttr = method.GetCustomAttribute<HandlerMethodAttribute>(true);

				if (handlerAttr == null)
				{
					continue;
				}

				string path;

				if (string.IsNullOrEmpty(handlerAttr.path))
				{
					path = method.Name;
				}
				else
				{
					path = handlerAttr.path;
				}

				path = CorrectPath(path);

				var fullPath = route + path;
				
				Console.WriteLine($"Registered {fullPath}");
				
				var obj = Activator.CreateInstance(type);
				var executor = ObjectMethodExecutor.Create(method, type.GetTypeInfo());

				_handlers[fullPath] = new HandlerData()
				{
					obj = obj,
					executor = executor,
					methodInfo = method,
					httpMethod = handlerAttr.method,
					isAsync = method.GetCustomAttribute<AsyncStateMachineAttribute>() != null,
				};
			}
		}
	}

	string CorrectPath(string path)
	{
		path = path.ToLower();

		if (!path.StartsWith('/'))
		{
			path = "/" + path;
		}

		if (path.EndsWith('/'))
		{
			path = path[..^1];
		}

		return path;
	}

	string GetFormattedTypeName(string typeName)
	{
		var name = typeName;
		var endIndex = name.IndexOf("Endpoint");

		if (endIndex != -1)
		{
			name = name[..endIndex];
		}

		return name;
	}

	string GetRouteFromType(Type type)
	{
		var route = type.Namespace?.Replace(".", "/") ?? "";

		var name = GetFormattedTypeName(type.Name);
				
		route += $"/{name}";

		return route;
	}
}