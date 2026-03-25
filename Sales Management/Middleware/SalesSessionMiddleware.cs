using System.Security.Claims;
using SalesManagement.BLL.Interfaces;

namespace SalesManagement.Web.Middleware
{
    public class SalesSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SalesSessionMiddleware> _logger;

        public SalesSessionMiddleware(RequestDelegate next, ILogger<SalesSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var user = context.User;
            var path = context.Request.Path.Value?.ToLower() ?? "";

            _logger.LogInformation($"Processing request: {path} User: {user.Identity?.Name} Role: {(user.IsInRole("Sales") ? "Sales" : "Other")}");

            // Bá» qua cÃ¡c trang Account, logoff, static files
            if (user.Identity == null || !user.Identity.IsAuthenticated || 
                !user.IsInRole("Sales") || 
                path.StartsWith("/account") || 
                path.StartsWith("/lib") || 
                path.StartsWith("/css") || 
                path.StartsWith("/js") ||
                path.StartsWith("/images"))
            {
                await _next(context);
                return;
            }

            // Logic kiá»ƒm tra Check-in cÃ³ thá»ƒ Ä‘Æ°á»£c thÃªm á»Ÿ Ä‘Ã¢y
            // Hiá»‡n táº¡i pass-through Ä‘á»ƒ Ä‘áº£m báº£o khÃ´ng cháº·n request há»£p lá»‡
            await _next(context);
        }
    }
}
