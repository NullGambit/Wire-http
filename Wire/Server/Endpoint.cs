namespace Wire.Server;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EndpointAttribute : Attribute
{
	public bool persistent;
}
