namespace Wire.server;

public record Config
(
	int port,
	int bufferSize = 1024
);