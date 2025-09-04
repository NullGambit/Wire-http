# Wire

Wire is a fast and simple http server that is similar to asp.net.

## Features

* Middlewares
* static file serving with caching
* dependancy injection
* full async request handling
* dynamic routing

### Usage

Creating an endpoint
```csharp
// Route is optional. the server will create the route from the namespace if Route is missing
[Endpoint, Route("api/{endpoint}")]
public class UserEndpoint
{
	DataBase _db;
	
    // Database will be added through dependancy injection automatically by the server
	public UserEndpoint(Database db)
	{
		_db = db;
	}
    
    // handler methods are completely dynamic
    // the server will parse out the id and username into the correct types and pass it to the handler
    // a handler can also take a Request type and the server will pass along the request
    // a handler can return any arbitary type or a Response type for more control
	[Get("does_match/{id}_{username}")]
	public async Task<string> Get(int id, string username)
	{
        var user = _db.Get(id);
        
		user != null ? $"a user with id {id} and username {username} exists" : "Not found";
	}
}
```

setting up the server 
```csharp
var server = new Server();

// any class can be added as a dependency.
server.router.AddDependency(new Database());

// a simple server wide middleware.
var requestCount = 0;
server.Use(async (ctx, next) =>
{
    requestCount++;
    Console.WriteLine($"{requestCount} requests made");
    next(ctx);
});

// this must be called for the server to automatically index all classes with the Endpoint attribute
// must be called after your dependencys are added.
server.router.IndexHandlers();

// finally run the server. 
await server.Run();
```

Wire is still very much WIP and was mostly made for fun.