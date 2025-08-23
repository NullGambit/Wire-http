namespace Wire.Common;

public record Response
(
	HttpStatusCode status,
	Dictionary<string, string> headers,
	byte[]? body = null,
	string? message = null
);