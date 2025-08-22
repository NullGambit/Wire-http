namespace Wire.Common;

public record Response
(
	HttpStatusCode status,
	string message,
	string version,
	Dictionary<string, string> headers
);