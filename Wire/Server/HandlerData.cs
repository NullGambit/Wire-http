using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Wire.Server;

internal class HandlerData
{
	public Type ownerType;
	public object obj;
	public ObjectMethodExecutor executor;
	public MethodInfo methodInfo;
	public HttpMethod httpMethod;
	public bool isAsync;
}