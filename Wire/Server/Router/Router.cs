using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Wire.Server.Router;

internal class Router
{
	PrefixTree _prefixTree = new ();

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
				
				var data = new HandlerData()
				{
					obj = obj,
					executor = executor,
					methodInfo = method,
					httpMethod = handlerAttr.method,
					isAsync = method.GetCustomAttribute<AsyncStateMachineAttribute>() != null,
				};

				var result = Index(fullPath, data);
				
				// TODO: integrate logging into the server
				if (result != RouteResult.Ok)
				{
					Console.WriteLine(RouteResultMethods.ToString(result));
				}
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
		List<object?>? parameters = null;

		foreach (var paramInfo in handler.methodInfo.GetParameters())
		{
			parameters ??= [];
			
			var prefixVar = result.vars.Find(pv => pv.name == paramInfo.Name);

			if (paramInfo.ParameterType == typeof(string))
			{
				parameters.Add(prefixVar.value);
			}
			else if (paramInfo.ParameterType == typeof(int))
			{
				var obj = Convert.ChangeType(prefixVar.value, paramInfo.ParameterType);
				parameters.Add(obj);
			}
		}

		if (handler.isAsync)
		{
			returnValue = await handler.executor.ExecuteAsync(handler.obj, parameters.ToArray());
		}
		else
		{
			returnValue = handler.executor.Execute(handler.obj, parameters.ToArray());
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