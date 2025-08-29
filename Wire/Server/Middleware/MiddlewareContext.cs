using System.Net.Sockets;
using Wire.Server.Router;

namespace Wire.Server.Middleware;

public class MiddlewareContext
{
	public HandlerData handlerData;
	public Request request;
	public Dictionary<string, object> pipelineData;
	
	internal Dictionary<Type, object> _deps;
	internal NetworkStream _stream;
	internal Server _server;

	public T? GetDep<T>()
	{
		var exists = _deps.TryGetValue(typeof(T), out var value);

		return exists ? (T?)value : default;
	}

	public async Task SendResponse(Response response)
	{
		_server.SendResponse(_stream, response);
	}
}