using System.Collections.Generic;

namespace Wire;

internal class Defaults
{
	internal static Dictionary<string, string> DefaultResponseHeader = new ()
	{
		{ "Server", "Wire" }
	};
}