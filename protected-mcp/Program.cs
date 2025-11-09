var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Provide information about the server
// Provide information about the server
app.MapGet("/info", () =>
{
    var version = System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "Unknown";
    return new { Name = "Protected MCP Server", Version = version };
});

app.Run();
