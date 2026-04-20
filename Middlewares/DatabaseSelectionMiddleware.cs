using Microsoft.AspNetCore.Http;

namespace IMS.Middlewares
{
    public class DatabaseSelectionMiddleware
    {
        private readonly RequestDelegate _next;

        public DatabaseSelectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.User.Identity.IsAuthenticated || context.Session.GetInt32("BranchId") is null)
            {
                if (!context.Request.Path.StartsWithSegments("/Account/Login"))
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
            await _next(context);
        }
    }
}
