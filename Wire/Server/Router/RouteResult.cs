namespace Wire.Server.Router;

internal enum RouteResult
{
	Ok,
	UnclosedBrace,
	RouteNotFound,
	MethodNotFound,
}

internal static class RouteResultMethods
{
	public static string ToString(RouteResult result)
	{
		return result switch
		{
			RouteResult.Ok => "Ok",
			RouteResult.UnclosedBrace => "Unclosed brace found in route variable",
			RouteResult.RouteNotFound => "The requested route does not exist",
			RouteResult.MethodNotFound => "The requested route does not implement this method",
		};
	}

	public static HttpStatusCode TranslateToHttpStatus(RouteResult result)
	{
		return result switch
		{
			RouteResult.Ok => HttpStatusCode.OK,
			RouteResult.RouteNotFound => HttpStatusCode.NotFound,
			RouteResult.MethodNotFound => HttpStatusCode.MethodNotAllowed,
		};
	}
}