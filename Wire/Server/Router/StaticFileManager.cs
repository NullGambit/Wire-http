namespace Wire.Server.Router;

class StaticFileData
{
    public byte[]? contents;
    public string osPath;
    public CachePolicy cachePolicy;
}

public enum CachePolicy
{
    AlwaysCache,
    EvictLast,
    EvictOldest,
}

public class RouteObject(byte[]? content)
{
    internal byte[]? content = content;
}

public class StaticFileManager
{
    public int cacheLimit = (int)3.2e+7; // 32 megabytes
        
    Dictionary<string, StaticFileData> _staticFiles = [];
    List<StaticFileData> _cache = [];
    int _cacheUsed;
    
    public void Index(string root, string pattern, CachePolicy CachePolicy = CachePolicy.EvictLast)
    {
        var files = Directory.GetFiles(root, pattern, SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var formatted = file;

            if (formatted.StartsWith("."))
            {
                formatted = formatted[1..];
            }
            
            Console.WriteLine(formatted);
            
            var data = new StaticFileData
            {
                osPath = file,
                cachePolicy = CachePolicy,
            };

            if (CachePolicy == CachePolicy.AlwaysCache)
            {
                data.contents = File.ReadAllBytes(file);
            }

            _staticFiles[formatted] = data;
        }
    }

    public async Task<byte[]?> Get(string route)
    {
        var exists = _staticFiles.TryGetValue(route, out var data);

        if (!exists)
        {
            return null;
        }

        if (data.contents == null)
        {
            data.contents = await File.ReadAllBytesAsync(data.osPath);
            
            _cacheUsed += data.contents.Length;

            while (_cacheUsed >= cacheLimit && _cache.Count > 0)
            {
                StaticFileData? entry = null;

                if (data.cachePolicy == CachePolicy.EvictLast)
                {
                    entry = _cache.Last();
                    
                    _cache.RemoveAt(_cache.Count);
                }
                else if (data.cachePolicy == CachePolicy.EvictOldest)
                {
                    entry = _cache.First();

                    var temp = _cache.Last();

                    _cache[0] = temp;

                    _cache[^0] = entry;
                    
                    _cache.RemoveAt(_cache.Count);
                }

                _cacheUsed -= entry != null ? entry.contents.Length : 0;

                entry.contents = null;
            }
            
            _cache.Add(data);
        }
        
        return data.contents;
    }

    public async Task<Response> Serve(string route)
    {
        var content = await Get(route);

        var status = content == null ? HttpStatusCode.NotFound : HttpStatusCode.OK;

        return new Response(status: status, body: content);
    }
}