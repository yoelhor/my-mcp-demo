// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add Azure stream log service
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "azure-diagnostics-";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

// Read authentication settings from configuration
var authority = builder.Configuration["Authentication:Authority"] ?? "https://login.microsoftonline.com/common";
var audience = builder.Configuration["Authentication:Audience"] ?? "https://my-protected-mcp";
var scopes = builder.Configuration.GetSection("Authentication:Scopes").Get<string[]>() ?? ["mcp:tools"];

// We first configure JWT Bearer authentication to validate incoming tokens
// issued by our OAuth 2.0 authorization server (Microsoft Entra ID)
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure to validate tokens from our in-memory OAuth server
    options.Authority = authority;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = audience,
        ValidIssuer = authority,
        NameClaimType = "name",
        RoleClaimType = "roles"
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated for: {Name} ({Email})", context.Principal?.Identity?.Name ?? "unknown", context.Principal?.FindFirstValue("preferred_username") ?? "unknown");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication failed: {ErrorMessage}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Challenging client to authenticate with Entra ID");
            return Task.CompletedTask;
        }
    };
})

// Configure MCP (Model Context Protocol) authentication
// This registers the MCP authentication handler and provides OAuth 2.0 metadata
// that clients can use to discover authentication requirements
// URL: https://localhost:7221/.well-known/oauth-protected-resource
.AddMcp(options =>
{
    // ResourceMetadata contains OAuth 2.0 discovery information for MCP clients
    options.ResourceMetadata = new()
    {
        // The resource URI that identifies this protected API (known as "audience" in OAuth 2.0)
        Resource = new Uri(audience),

        // Documentation URL for API consumers
        ResourceDocumentation = new Uri("https://docs.example.com"),

        // Authorization server(s) where clients can obtain access tokens (known as "issuer" in OAuth 2.0)
        AuthorizationServers = { new Uri(authority) },

        // OAuth 2.0 scopes required to access this server's tools
        ScopesSupported = [.. scopes],
    };
});

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
app.MapGet("/info", () =>
{
    var version = System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "Unknown";
    return new { Name = "Protected MCP Server", Version = version };
});

app.Run();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool, Description("Returns the length of a message.")]
    public static string ContentLength(string message) => $"Your message is {message.Length} characters long.";

    [McpServerTool, Description("Returns the MCP version.")]
    public static string GetVersion() => $"MCP (protected) version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
}