namespace Xconnect.API.Middleware
{
    public class ClientIpMiddleware
    {
        private readonly RequestDelegate _next;


        public ClientIpMiddleware(RequestDelegate next)
        {
            _next = next;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Default to the IP address of the client
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            // Check for forwarded IP in case of reverse proxy (load balancer, etc.)
            var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp))
            {
                ipAddress = forwardedIp.Split(',')[0]; // Get the first IP in the chain
            }

            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Optionally, you can store the IP address in a custom header or context to access later
            context.Items["ClientIp"] = ipAddress;
            context.Items["UserAgent"] = userAgent;

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
