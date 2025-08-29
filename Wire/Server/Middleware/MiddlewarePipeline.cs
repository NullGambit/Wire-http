namespace Wire.Server.Middleware;

public class MiddlewarePipeline
{
	public delegate Task MiddlewareDelegate(MiddlewareContext ctx);
	public delegate Task Middleware(MiddlewareContext ctx, MiddlewareDelegate next);

	List<Middleware> _pipeline;

	public MiddlewarePipeline(List<Middleware>? pipeline = null)
	{
		if (pipeline != null)
		{
			_pipeline = new List<Middleware>(pipeline);
		}
		else
		{
			_pipeline = [];
		}
	}

	public MiddlewarePipeline Use(Middleware middleware)
	{
		_pipeline.Add(middleware);
		return this;
	}

	public MiddlewarePipeline Clone()
	{
		return new MiddlewarePipeline(_pipeline);
	}

	public MiddlewareDelegate Build()
	{
		MiddlewareDelegate next = _ => Task.CompletedTask;

		foreach (var middleware in _pipeline.AsEnumerable().Reverse())
		{
			var current = middleware;
			var prevNext = next;

			next = ctx => current(ctx, prevNext);
		}

		return next;
	}
}