using System.Text;

namespace Wire;

// a parser that can parse a single http request or response
public struct FrameParser(byte[] memory)
{
	public byte[] _mem = memory;

	int _cursor = 0;

	public static async Task<Request?> ParseRequestAsync(byte[] memory)
	{
		return await Task.Run(() => new FrameParser(memory).ParseRequest());
	}

	public Request? ParseRequest()
	{
		var methodStr = GetWord();
		
		var ok = Enum.TryParse(typeof(HttpMethod), methodStr, ignoreCase: true, out var method);

		if (!ok)
		{
			return null;
		}

		var path = GetWord();
		var version = GetLine();
		
		var headers = new Dictionary<string, string>();

		while (!AtEnd() && !IsHeaderEnd())
		{
			var key = Encoding.UTF8.GetString(ReadUntil(':'));
			
			SkipBlank();
		
			var value = GetLine();
		
			headers[key] = value;
		}
		
		var request = new Request(method: (HttpMethod)method, path: path, version: version, headers: headers);

		return request;
	}

	void SkipBlank()
	{
		while (!AtEnd() && Match(' '))
		{
		}
	}
	
	// TODO: avoid an allocation thats possibly made here
	string? GetWord()
	{
		var bytes = ReadUntil(' ');

		if (bytes == null)
		{
			return null;
		}
			
		return Encoding.UTF8.GetString(bytes);
	}

	string GetLine()
	{
		var line = ReadUntil('\r');

		Match('\n');

		return Encoding.UTF8.GetString(line);
	}

	byte[]? ReadUntil(char c)
	{
		var start = _cursor;
		
		while (!AtEnd())
		{
			var next = Advance();

			if (c == (char)next)
			{
				return _mem[start .. (_cursor - 1)];
			}
		}

		return null;
	}

	bool IsHeaderEnd()
	{
		if (Peak() == '\r' && PeakNext() == '\n')
		{
			_cursor += 2;
			return true;
		}

		return false;
	}

	byte Advance()
	{
		if (_cursor + 1 >= _mem.Length)
		{
			return 0;
		}

		return _mem[_cursor++];
	}

	bool Match(char c)
	{
		if (_mem[_cursor] == c)
		{
			_cursor++;
			return true;
		}

		return false;
	}

	byte PeakNext()
	{
		if (_cursor + 1 >= _mem.Length)
		{
			return 0;
		}

		return _mem[_cursor + 1];
	}
	
	byte Peak()
	{
		if (_cursor >= _mem.Length)
		{
			return 0;
		}

		return _mem[_cursor];
	}

	bool AtEnd() => _cursor >= _mem.Length;
}