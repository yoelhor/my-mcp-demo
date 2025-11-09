// Program.cs
using Microsoft.Extensions.Logging.AzureAppServices;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Add Azure stream log service
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "azure-diagnostics-";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});
// builder.Logging.AddFilter((provider, category, logLevel) =>
// {
//     return provider!.ToLower().Contains("");
// });

builder.Services.AddMcpServer()
    // Use HTTP transport
    .WithHttpTransport()

    // Register tools from the current assembly using the McpServerTool attribute
    .WithToolsFromAssembly();
var app = builder.Build();

// Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapMcp();

// Provide information about the server
app.MapGet("/info", () => "This is an anonymous MCP server.");

app.Run();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}