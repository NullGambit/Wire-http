namespace Wire;

public record Response
(
	HttpStatusCode status = HttpStatusCode.OK,
	Dictionary<string, string>? headers = null,
	byte[]? body = null,
	string? message = null
);