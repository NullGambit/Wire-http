namespace Wire.Server;

public record Config
(
	int port,
	int bufferSize = 1024
);