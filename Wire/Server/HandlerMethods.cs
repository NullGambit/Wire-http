using Wire;

namespace Wire.Server;

[AttributeUsage(AttributeTargets.Method)]
public class HandlerMethodAttribute : Attribute
{
	public string? path;
	public HttpMethod method;
}

[AttributeUsage(AttributeTargets.Method)]
public class GetAttribute : HandlerMethodAttribute
{
	public GetAttribute(string? path = null)
	{
		this.path = path;
		method = HttpMethod.Get;
	}
}

[AttributeUsage(AttributeTargets.Method)]
public class PutAttribute : HandlerMethodAttribute
{
	public PutAttribute(string? path = null)
	{
		this.path = path;
		method = HttpMethod.Put;
	}
}

[AttributeUsage(AttributeTargets.Method)]
public class PostAttribute : HandlerMethodAttribute
{
	public PostAttribute(string? path = null)
	{
		this.path = path;
		method = HttpMethod.Post;
	}
}

[AttributeUsage(AttributeTargets.Method)]
public class DeleteAttribute : HandlerMethodAttribute
{
	public DeleteAttribute(string? path = null)
	{
		this.path = path;
		method = HttpMethod.Delete;
	}
}

[AttributeUsage(AttributeTargets.Method)]
public class PatchAttribute : HandlerMethodAttribute
{
	public PatchAttribute(string? path = null)
	{
		this.path = path;
		method = HttpMethod.Patch;
	}
}
