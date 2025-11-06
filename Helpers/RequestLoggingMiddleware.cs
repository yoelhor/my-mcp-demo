using System.Text;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log Authorization header
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            // Mask sensitive parts of the token for security
            var maskedAuth = authHeader.ToString().Length > 20
                ? authHeader.ToString().Substring(0, 20) + "..."
                : authHeader.ToString();
            _logger.LogInformation("Authorization Header: {AuthHeader}", maskedAuth);
        }
        else
        {
            _logger.LogInformation("No Authorization header present");
        }

        // Log all request headers
        // foreach (var header in context.Request.Headers)
        // {
        //     _logger.LogInformation("Header: {HeaderName} = {HeaderValue}", header.Key, header.Value.ToString());
        // }

        // Log request body
        context.Request.EnableBuffering(); // Allow reading body multiple times

        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset stream position

            _logger.LogInformation("Request Body: {RequestBody}", body);
        }
        else
        {
            _logger.LogInformation("Request body is empty");
        }

        await _next(context);
    }
}
