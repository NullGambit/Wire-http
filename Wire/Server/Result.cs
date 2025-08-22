using Wire.Common;

namespace Wire.Server;

public record struct Result<T>
(
	T value, 
	HttpStatusCode status = HttpStatusCode.OK, 
	string? statusMessage = null
);