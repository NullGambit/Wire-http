using Wire.Server;

namespace Demo;

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

struct PrefixResult
{
	public int value;
	public List<PrefixVar>? vars;
}

class PrefixParam
{
	public string name;
	public string? stoppingPoint;
}

class PrefixNode
{
	public const int ALPHABET_SIZE = 93;
	public const int OFFSET = 33;
	
	public readonly PrefixNode[] children = new PrefixNode[ALPHABET_SIZE];
	public int value;

	public List<PrefixParam>? @params;
}

enum PrefixTreeAddResult
{
	Ok,
	UnclosedBrace,
}

class PrefixTree
{
	public PrefixNode root = new ();

	int GetIndex(char c) => c - PrefixNode.OFFSET;

	public PrefixTreeAddResult Add(string key, int value)
	{
		var node = root;
		
		for (var i = 0; i < key.Length; i++)
		{
			var c = key[i];

			if (c == '{')
			{
				var param = new PrefixParam();
				
				var end = key.IndexOf('}', i);

				if (end == -1)
				{
					return PrefixTreeAddResult.UnclosedBrace;
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

		node.value = value;
		
		return PrefixTreeAddResult.Ok;
	}

	public bool TryGet(string key, out PrefixResult result)
	{
		result = new PrefixResult();
		
		var node = root;
		var foundAny = false;
		
		for (var i = 0; i < key.Length; i++)
		{
			if (node.@params != null)
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
				
				result.value = node.value;
			
				foundAny = true;
			}
			
			var next = node.children[GetIndex(key[i])];

			if (next == null)
			{
				return foundAny;
			}

			node = next;
		}
		
		result.value = node.value;
		
		return foundAny;
	}
}

class Program
{
	// static async Task Main(string[] args)
	// {
	// 	var server = new Server();
	// 	
	// 	server.IndexHandlers();
	//
	// 	var result = await server.Run();
	// }
	
	static void Main(string[] args)
	{
		var pt = new PrefixTree();
		
		pt.Add("/api/user/{id}", 1);
		pt.Add("/api/user/{id}-post", 2);
		pt.Add("/api/user/{id}_{name}", 3);
		
		var found = pt.TryGet("/api/user/10-post", out var result);
		
		if (found)
		{
			Console.WriteLine(result.value);

			foreach (var (name, value) in result.vars)
			{
				Console.WriteLine($"{name} = {value}");
			}
		}
		
		// pt.Add("hello", 0);
		// pt.Add("herro", 1);
		// pt.Add("yes", 20);
		//
		// var found = pt.TryGet("yes", out var value);
		//
		// if (found)
		// {
		// 	Console.WriteLine(value);
		// }
	}
}