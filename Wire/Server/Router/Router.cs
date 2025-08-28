using System.Text;

namespace Wire.Server.Router;

internal class Router
{
	PrefixTree _prefixTree = new ();

	public RouteResult Index(string route, HandlerData data) => _prefixTree.Add(route, data);

	public async Task<(RouteResult, Response?)> RouteAndCall(Request request)
	{
		var routeResult = _prefixTree.Get(request.path, request.method, out var result);

		if (routeResult != RouteResult.Ok)
		{
			return (routeResult, null);
		}

		var handler = result.value;
		
		object? returnValue;

		if (handler.isAsync)
		{
			returnValue = await handler.executor.ExecuteAsync(handler.obj, null);
		}
		else
		{
			returnValue = handler.executor.Execute(handler.obj, null);
		}
				
		if (returnValue == null)
		{
			return (RouteResult.Ok, new Response());
		}
		
		Response response;

		if (returnValue.GetType() == typeof(Response))
		{
			response = (Response)returnValue;
		}
		else
		{
			var str = returnValue.ToString();

			var bytes = Encoding.UTF8.GetBytes(str);
					
			response = new Response(body: bytes);
		}

		return (RouteResult.Ok, response);
	}
}