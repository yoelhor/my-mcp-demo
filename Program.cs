// Program.cs
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    // Use HTTP transport
    .WithHttpTransport()

    // Register tools from the current assembly using the McpServerTool attribute
    .WithToolsFromAssembly();
var app = builder.Build();

app.MapMcp();

// Optional: A simple HTTP endpoint for testing
app.MapGet("/info", () => "Hello World!");

app.Run();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}