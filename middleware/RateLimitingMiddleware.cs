using System.Collections.Concurrent;

namespace Jollicow.middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // In-memory storage cho rate limiting (có thể thay bằng Redis trong production)
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _requestCounts = new();

        // Cấu hình rate limit
        private const int MaxRequestsPerMinute = 60; // 60 requests/phút
        private const int MaxRequestsPerSecond = 10;  // 10 requests/giây
        private const int WindowSizeInSeconds = 60;   // Window 60 giây

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);

            if (IsRateLimited(clientId))
            {
                _logger.LogWarning($"Rate limit exceeded for client: {clientId}");

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    error = "Rate limit exceeded",
                    message = "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
                    retryAfter = GetRetryAfterSeconds(clientId)
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Sử dụng IP address làm client identifier
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Nếu có table ID, kết hợp với IP để tạo unique identifier
            var tableId = context.Request.Query["id_table"].ToString();
            if (!string.IsNullOrEmpty(tableId))
            {
                return $"{ipAddress}_{tableId}";
            }

            return ipAddress;
        }

        private bool IsRateLimited(string clientId)
        {
            var now = DateTime.UtcNow;

            // Lấy thông tin rate limit hiện tại
            var rateLimitInfo = _requestCounts.GetOrAdd(clientId, _ => new RateLimitInfo());

            // Cleanup old requests
            rateLimitInfo.CleanupOldRequests(now);

            // Kiểm tra rate limit
            var requestsInWindow = rateLimitInfo.GetRequestsInWindow(now, WindowSizeInSeconds);
            var requestsInLastSecond = rateLimitInfo.GetRequestsInLastSecond(now);

            // Thêm request hiện tại
            rateLimitInfo.AddRequest(now);

            // Kiểm tra giới hạn
            if (requestsInWindow >= MaxRequestsPerMinute)
            {
                _logger.LogInformation($"Minute rate limit exceeded for {clientId}: {requestsInWindow}/{MaxRequestsPerMinute}");
                return true;
            }

            if (requestsInLastSecond >= MaxRequestsPerSecond)
            {
                _logger.LogInformation($"Second rate limit exceeded for {clientId}: {requestsInLastSecond}/{MaxRequestsPerSecond}");
                return true;
            }

            return false;
        }

        private int GetRetryAfterSeconds(string clientId)
        {
            var rateLimitInfo = _requestCounts.GetOrAdd(clientId, _ => new RateLimitInfo());
            var now = DateTime.UtcNow;

            // Tính thời gian cần đợi
            var oldestRequest = rateLimitInfo.GetOldestRequest();
            if (oldestRequest.HasValue)
            {
                var timeSinceOldest = now - oldestRequest.Value;
                return Math.Max(1, WindowSizeInSeconds - (int)timeSinceOldest.TotalSeconds);
            }

            return 60; // Default 60 giây
        }
    }

    public class RateLimitInfo
    {
        private readonly ConcurrentQueue<DateTime> _requests = new();
        private readonly object _lock = new object();

        public void AddRequest(DateTime timestamp)
        {
            _requests.Enqueue(timestamp);
        }

        public void CleanupOldRequests(DateTime now)
        {
            lock (_lock)
            {
                while (_requests.TryPeek(out var oldestRequest))
                {
                    if (now - oldestRequest > TimeSpan.FromMinutes(1))
                    {
                        _requests.TryDequeue(out _);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public int GetRequestsInWindow(DateTime now, int windowSeconds)
        {
            var cutoff = now.AddSeconds(-windowSeconds);
            return _requests.Count(request => request >= cutoff);
        }

        public int GetRequestsInLastSecond(DateTime now)
        {
            var cutoff = now.AddSeconds(-1);
            return _requests.Count(request => request >= cutoff);
        }

        public DateTime? GetOldestRequest()
        {
            return _requests.TryPeek(out var oldest) ? oldest : null;
        }
    }
}