using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Wire.Server.Router;

public class Router
{
	PrefixTree _prefixTree = new ();
	internal Dictionary<Type, object> _handlerDeps = [];
	public readonly StaticFileManager staticFileManager = new();

	public void AddDependency<T>([NotNull] T obj) => _handlerDeps[typeof(T)] = obj;

	public void IndexHandlers()
	{
		var assembly = Assembly.GetEntryAssembly();
		
		var handlerTypes = assembly.GetTypes()
			.Where(t => t.GetCustomAttribute<EndpointAttribute>() != null);

		foreach (var type in handlerTypes)
		{
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
			
			foreach (var methodInfo in handlerMethods)
			{
				var handlerAttr = methodInfo.GetCustomAttribute<HandlerMethodAttribute>(true);

				if (handlerAttr == null)
				{
					continue;
				}

				string path;

				if (string.IsNullOrEmpty(handlerAttr.path))
				{
					path = methodInfo.Name;
				}
				else
				{
					path = handlerAttr.path;
				}

				path = CorrectPath(path);

				var fullPath = route + path;
				
				Console.WriteLine($"Registered {fullPath}");
				
				var ctorInfos = type.GetConstructors();
				
				List<object> deps = [];

				foreach (var ctorInfo in ctorInfos)
				{
					var ctorParams = ctorInfo.GetParameters();
					var foundAny = false;

					foreach (var paramInfo in ctorParams)
					{
						var exists = _handlerDeps.TryGetValue(paramInfo.ParameterType, out object dep);

						if (!exists)
						{
							foundAny = false;
							break;
						}

						foundAny = true;
						
						deps.Add(dep);
					}

					if (foundAny)
					{
						break;
					}
					
					deps.Clear();
				}
				
				var obj = Activator.CreateInstance(type, deps.ToArray());
				var executor = ObjectMethodExecutor.Create(methodInfo, type.GetTypeInfo());
				
				var data = new HandlerData()
				{
					obj = obj,
					executor = executor,
					httpMethod = handlerAttr.method,
					parameterInfos = methodInfo.GetParameters(),
					isAsync = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null,
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

	internal RouteResult Index(string route, HandlerData data) => _prefixTree.Add(route, data);

	internal async Task<(RouteResult, object?)> Route(Request request)
	{
		var staticFileContents = await staticFileManager.Get(request.path);

		if (staticFileContents != null)
		{
			return (RouteResult.Ok, new RouteObject(staticFileContents));
		}
		
		var (routeResult, prefixResult) = await Task.Run(() => _prefixTree.Get(request.path, request.method));

		if (routeResult != RouteResult.Ok)
		{
			return (routeResult, null);
		}
		
		return (RouteResult.Ok, prefixResult);
	}

	internal async Task<(RouteResult, Response?)> CallHandler(object result, Request request)
	{
		if (result.GetType() == typeof(PrefixResult))
		{
			return await DoCallHandler((PrefixResult)result, request);
		}
		else
		{
			var staticFileResult = (RouteObject)result;

			if (staticFileResult.content == null)
			{
				return (RouteResult.RouteNotFound, new Response(HttpStatusCode.NotFound));
			}

			return (RouteResult.Ok, new Response(body: staticFileResult.content));
		}
	}

	async Task<(RouteResult, Response?)> DoCallHandler(PrefixResult result, Request request)
	{
		var handler = result.value;
		
		object? returnValue;
		
		var parameters = new object[handler.parameterInfos.Length];
		
		for (var i = 0; i < handler.parameterInfos.Length; i++)
		{
			var paramInfo = handler.parameterInfos[i];
			
			var prefixVar = result.vars.Find(pv => pv.name == paramInfo.Name);

			if (paramInfo.ParameterType == typeof(string))
			{
				parameters[i] = prefixVar.value;
			}
			else if (paramInfo.ParameterType == typeof(int))
			{
				var obj = Convert.ChangeType(prefixVar.value, paramInfo.ParameterType);
				parameters[i] = obj;
			}
			else if (paramInfo.ParameterType == typeof(Request))
			{
				parameters[i] = request;
			}
		}

		if (handler.isAsync)
		{
			returnValue = await handler.executor.ExecuteAsync(handler.obj, parameters);
		}
		else
		{
			returnValue = handler.executor.Execute(handler.obj, parameters);
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