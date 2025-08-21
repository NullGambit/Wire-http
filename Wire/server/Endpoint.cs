namespace Wire.server;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class Endpoint : Attribute
{
	public string? path;
	public bool persistent;
}
