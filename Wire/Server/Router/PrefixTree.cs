namespace Wire.Server.Router;

// a prefix tree implementation for routing parameters that contain variables such as /api/user/{id}
// this implementation is not generic as i did not see any need to do so.

struct PrefixVar
{
	public string name;
	public string value;

	public void Deconstruct(out string name, out string value)
	{
		name = this.name;
		value = this.value;
	}
}

class PrefixResult
{
	public HandlerData value;
	public List<PrefixVar>? vars;
}

class PrefixParam
{
	public string name;
	public string? stoppingPoint;
}

class PrefixNode
{
	const int ALPHABET_SIZE = 93;
	
	public const int OFFSET = 33;
	
	public readonly PrefixNode[] children = new PrefixNode[ALPHABET_SIZE];
	public HandlerData[] value = new HandlerData[Enum.GetNames<HttpMethod>().Length];

	public List<PrefixParam>? @params;
}

internal class PrefixTree
{
	readonly PrefixNode _root = new ();

	static int GetIndex(char c) => c - PrefixNode.OFFSET;

	public RouteResult Add(string key, HandlerData value)
	{
		var node = _root;
		
		for (var i = 0; i < key.Length; i++)
		{
			var c = key[i];

			if (c == '{')
			{
				var param = new PrefixParam();
				
				var end = key.IndexOf('}', i);

				if (end == -1)
				{
					return RouteResult.UnclosedBrace;
				}
				
				param.name = key[(i + 1)..end];
				
				i = end;
				
				if (i < key.Length)
				{
					// in case there is another param stop at the next brace
					var stopIndex = key.IndexOf('{', i);
					param.stoppingPoint = key[(i + 1)..(stopIndex == -1 ? ^0 : stopIndex)];
				}
				
				node.@params ??= [];
				
				node.@params.Add(param);
			}
			else
			{
				var index = GetIndex(c);
				var next = node.children[index];
				
				if (next == null)
				{
					next = new PrefixNode();
					node.children[index] = next;
				}

				node = next;
			}
		}

		node.value[(int)value.httpMethod] = value;
		
		return RouteResult.Ok;
	}

	public (RouteResult, PrefixResult?) Get(string key, HttpMethod method)
	{
		var result = new PrefixResult();
		
		var node = _root;
		var foundAny = false;
		int i;
		
		for (i = 0; i < key.Length; i++)
		{
			if (node.@params != null && node.children[GetIndex(key[i])] == null)
			{
				PrefixParam param = null;
				var stopIndex = 0;

				foreach (var p in node.@params)
				{
					if (!string.IsNullOrEmpty(p.stoppingPoint))
					{
						stopIndex = key.IndexOf(p.stoppingPoint, i);
						
						if (stopIndex != -1)
						{
							param = p;
							break;
						}
					}
					else
					{
						param = p;
					}
				}
				
				var prefixVar = new PrefixVar
				{
					name = param.name
				};
			
				if (string.IsNullOrEmpty(param.stoppingPoint))
				{
					prefixVar.value = key[i..];
				}
				else
				{
					prefixVar.value = key[i..stopIndex];
					
					i = stopIndex;
				}
			
				result.vars ??= [];
				
				result.vars.Add(prefixVar);
				
				foundAny = true;
			}
			
			var next = node.children[GetIndex(key[i])];

			if (next == null)
			{
				break;
			}

			node = next;
		}
		
		result.value = node.value[(int)method];

		if (result.value == null)
		{
			return (RouteResult.MethodNotFound, null);
		}
		
		return foundAny || i == key.Length ? (RouteResult.Ok, result) : (RouteResult.RouteNotFound, null);
	}
}