using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;

namespace IMS.Authorization
{
    /// <summary>
    /// Custom authorization that requires authentication and optionally a privilege (feature name).
    /// Use: [CustomAuthorize] or [CustomAuthorize(EnumPrivilegesName.USER_MANAGEMENT_USERS)]
    /// Supports cookie auth and Basic auth header (for API calls).
    /// </summary>
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        /// <summary>
        /// Privilege (feature name) to check. If null or empty, only authentication is required.
        /// </summary>
        public string Privilege { get; set; } = string.Empty;

        /// <summary>
        /// Alias for Privilege so you can use: [CustomAuthorize(privilege = EnumPrivilegesName.XXX)]
        /// </summary>
        public string privilege
        {
            get => Privilege;
            set => Privilege = value ?? string.Empty;
        }

        public CustomAuthorizeAttribute() { }

        public CustomAuthorizeAttribute(string privilege)
        {
            if (!string.IsNullOrEmpty(privilege))
                Privilege = privilege;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (IsIdentityAuthorized(context.HttpContext))
                return;
            if (IsLoginAuthorized(context.HttpContext))
                return;

            context.Result = new ContentResult
            {
                Content = "Session has expired, please login again.",
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        private bool IsIdentityAuthorized(HttpContext context)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
                return false;

            if (string.IsNullOrEmpty(Privilege))
                return true;

            var service = context.RequestServices.GetService(typeof(IPrivilegeAuthorizationService)) as IPrivilegeAuthorizationService;
            if (service == null)
                return false;

            var userName = context.User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return false;

            return service.UserHasPrivilegeAsync(userName, Privilege).GetAwaiter().GetResult();
        }

        private bool IsLoginAuthorized(HttpContext context)
        {
            try
            {
                var credentials = GetCredentialsFromAuthorizationHeader(context);
                if (credentials == null)
                    return false;

                var userService = context.RequestServices.GetService(typeof(Common_Interfaces.IUserService)) as Common_Interfaces.IUserService;
                if (userService == null)
                    return false;

                var (userName, password) = credentials.Value;
                // Validate user (use branch 0 or first available; caller may need to set branch in header for strict check)
                var user = userService.GetUserByCredentialsAsync(userName, password, 0).GetAwaiter().GetResult();
                if (user == null)
                {
                    // Try with a non-zero branch if your DB allows; here we require valid credentials
                    return false;
                }

                if (string.IsNullOrEmpty(Privilege))
                    return true;

                var privilegeService = context.RequestServices.GetService(typeof(IPrivilegeAuthorizationService)) as IPrivilegeAuthorizationService;
                if (privilegeService == null)
                    return false;

                return privilegeService.UserHasPrivilegeAsync(user.UserId, Privilege).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        private static (string UserName, string Password)? GetCredentialsFromAuthorizationHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var colonIndex = decoded.IndexOf(':');
                if (colonIndex < 0)
                    return null;
                return (decoded[..colonIndex], decoded[(colonIndex + 1)..]);
            }
            catch
            {
                return null;
            }
        }
    }
}
