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

class PrefixNode
{
	public const int ALPHABET_SIZE = 93;
	public const int OFFSET = 33;
	
	public PrefixNode[] children = new PrefixNode[ALPHABET_SIZE];
	public int value;
	public string? param;
	public string? stoppingPoint;
}

class PrefixTree
{
	public PrefixNode root = new ();

	int GetIndex(char c) => c - PrefixNode.OFFSET;

	public void Add(string key, int value)
	{
		var node = root;
		
		for (var i = 0; i < key.Length; i++)
		{
			var c = key[i];

			if (c == '{')
			{
				var end = key.IndexOf('}', i);
				
				node.param = key[(i + 1)..end];
				
				i += end - i + 1;

				if (i < key.Length)
				{
					node.stoppingPoint = key[i..];
				}

				// node.value = value;

				// Console.WriteLine(node.stoppingPoint);
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
	}

	public bool TryGet(string key, out PrefixResult result)
	{
		result = new PrefixResult();
		
		var node = root;
		var foundAny = false;
		
		for (var i = 0; i < key.Length; i++)
		{
			if (!string.IsNullOrEmpty(node.param))
			{
				var prefixVar = new PrefixVar();
				
				prefixVar.name = node.param;
				
				if (string.IsNullOrEmpty(node.stoppingPoint))
				{
					prefixVar.value = key[i..];
					
					result.value = node.value;
				}
				else
				{
					var stopIndex = key.IndexOf(node.stoppingPoint);
					
					if (stopIndex != -1)
					{
						prefixVar.value = key[i..stopIndex];
						i += stopIndex - i + 1;
					}
					// Console.WriteLine(prefixVar.value);
				}

				result.vars ??= [];
				
				result.vars.Add(prefixVar);

				foundAny = true;
			}
			
			var next = node.children[key[i] - PrefixNode.OFFSET];
			
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
		// pt.Add("/api/user/{id}_{name}", 3);
		
		var found = pt.TryGet("/api/user/10", out var result);
		
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