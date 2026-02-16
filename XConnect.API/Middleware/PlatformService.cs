namespace Xconnect.API.Middleware
{
    public class PlatformService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PlatformService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserPlatform(string customerIpAddress)
        {
            // Get the HttpRequest from IHttpContextAccessor
            var request = _httpContextAccessor.HttpContext.Request;
            string ua = request.Headers["User-Agent"].ToString();
            string browser = GetBrowserInfo(request);
            string ip = GetIPAddress();

            // If customerIpAddress is null or invalid, use the current request's IP
            if (string.IsNullOrEmpty(customerIpAddress) || customerIpAddress == "::1")
            {
                customerIpAddress = ip;
            }

            // If the User-Agent is empty, set a default value
            if (string.IsNullOrEmpty(ua))
            {
                ua = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36";
            }

            // Basic return with User-Agent and IP Address
            return ua + "$$" + customerIpAddress;

            #region Unrechable Code
            // Platform-specific checks based on User-Agent string
            if (ua.Contains("Android"))
                return browser + "(" + string.Format("Android {0}", GetMobileVersion(ua, "Android")) + ")";

            if (ua.Contains("iPad"))
                return browser + "(" + string.Format("iPad OS {0}", GetMobileVersion(ua, "OS")) + ")";

            if (ua.Contains("iPhone"))
                return browser + "(" + string.Format("iPhone OS {0}", GetMobileVersion(ua, "OS")) + ")";

            if (ua.Contains("Linux") && ua.Contains("KFAPWI"))
                return browser + "(" + "Kindle Fire" + ")";

            if (ua.Contains("RIM Tablet") || (ua.Contains("BB") && ua.Contains("Mobile")))
                return browser + "(" + "BlackBerry" + ")";

            if (ua.Contains("Windows Phone"))
                return browser + "(" + string.Format("Windows Phone {0}", GetMobileVersion(ua, "Windows Phone")) + ")";

            if (ua.Contains("Mac OS"))
                return browser + "(" + "Mac OS" + ")";

            // Check for different versions of Windows
            if (ua.Contains("Windows NT 5.1") || ua.Contains("Windows NT 5.2"))
                return browser + "(" + "Windows XP" + ")";

            if (ua.Contains("Windows NT 6.0"))
                return browser + "(" + "Windows Vista" + ")";

            if (ua.Contains("Windows NT 6.1"))
                return browser + "(" + "Windows 7" + ")";

            if (ua.Contains("Windows NT 6.2"))
                return browser + "(" + "Windows 8" + ")";

            if (ua.Contains("Windows NT 6.3"))
                return browser + "(" + "Windows 8.1" + ")";

            if (ua.Contains("Windows NT 10"))
                return browser + "(" + "Windows 10" + ")";

            // Fallback: Return browser and platform (if available)
            return "";
            #endregion

        }

        public string GetHostName()
        {
            var host = _httpContextAccessor.HttpContext?.Request.Host.ToString();
            return host ?? "Unknown Host";

        }

        public string GetHostNameNew()
        {
            var path = _httpContextAccessor.HttpContext?.Request.Path.ToString();
            string apiName = null;

            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 2)
                {
                    apiName = segments[2];
                }
            }
            return apiName;
        }

        public string GetBrowserInfo(HttpRequest request)
        {
            // You can manually retrieve the browser info from the User-Agent
            string userAgent = request.Headers["User-Agent"].ToString();
            // Logic for extracting browser information from User-Agent string.
            return userAgent.Contains("Chrome") ? "Chrome" : "Unknown";
        }

        public HttpRequest GetRequest()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            // You can manually retrieve the browser info from the User-Agent
            string userAgent = request.Headers["User-Agent"].ToString();
            // Logic for extracting browser information from User-Agent string.
            return _httpContextAccessor.HttpContext.Request;
        }

        public string GetIPAddressOLD()
        {
            // Access the IP Address from the HttpContext Connection
            //var remoteIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            //return remoteIpAddress;


            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "Unknown";

            string ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (string.IsNullOrEmpty(ip))
                ip = httpContext.Connection.RemoteIpAddress?.ToString();

            return ip ?? "Unknown";
        }

        public string GetIPAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "Unknown";

            // X-Forwarded-For હેડર માંથી પ્રથમ IP લેજો (comma થી અલગ) //Client IP
            string ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

            // જો X-Forwarded-For ન હોય, તો RemoteIpAddress થી લેજો
            if (string.IsNullOrEmpty(ip))
                ip = httpContext.Connection.RemoteIpAddress?.ToString();

            return ip ?? "Unknown";
        }

        private string GetMobileVersion(string userAgent, string platform)
        {
            // Your logic for extracting mobile version information from User-Agent
            // This can be simplified or enhanced depending on your needs.
            return "Unknown Version";
        }
    }
}
