using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Wire.Server.Router;

internal class HandlerData
{
	public Type ownerType;
	public object obj;
	public ObjectMethodExecutor executor;
	public MethodInfo methodInfo;
	public ParameterInfo[] parameterInfos;
	public HttpMethod httpMethod;
	public bool isAsync;
}