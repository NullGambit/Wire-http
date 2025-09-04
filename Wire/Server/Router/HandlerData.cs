using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Wire.Server.Router;

public class HandlerData
{
	public object obj;
	public ParameterInfo[] parameterInfos;
	public HttpMethod httpMethod;
	public bool isAsync;
	
	internal ObjectMethodExecutor executor;
}