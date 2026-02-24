using System.Security.Claims;
using Sales_Management.Services;

namespace Sales_Management.Middleware
{
    public class SalesSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SalesSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var user = context.User;
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Bỏ qua các trang Account, logoff, static files
            if (!user.Identity.IsAuthenticated || 
                !user.IsInRole("Sales") || 
                path.StartsWith("/account") || 
                path.StartsWith("/lib") || 
                path.StartsWith("/css") || 
                path.StartsWith("/js"))
            {
                await _next(context);
                return;
            }

            await _next(context);
        }
    }
}
