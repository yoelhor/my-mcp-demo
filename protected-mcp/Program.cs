var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Provide information about the server
app.MapGet("/info", () => "This is a protected MCP server.");

app.Run();
