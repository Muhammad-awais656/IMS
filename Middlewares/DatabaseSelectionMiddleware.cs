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
            if (!context.User.Identity.IsAuthenticated || string.IsNullOrEmpty(context.Session.GetString("Domain")))
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
