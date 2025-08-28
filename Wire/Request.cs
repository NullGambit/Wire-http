namespace Wire;

public record Request
(
	HttpMethod method = HttpMethod.Get,
	string path = "",
	string version = "",
	Dictionary<string, string>? headers = null
);