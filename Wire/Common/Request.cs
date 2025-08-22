namespace Wire.Common;

public record struct Request
(
	HttpMethod method = HttpMethod.Get,
	string path = "",
	string version = "",
	Dictionary<string, string>? headers = null
);