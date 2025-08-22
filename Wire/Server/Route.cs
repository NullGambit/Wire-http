namespace Wire.Server;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RouteAttribute(string path) : Attribute
{
	public string path = path;
}