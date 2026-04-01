using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IMS.Authorization
{
    /// <summary>
    /// MVC custom authorization: requires authentication and optionally a privilege (feature name).
    /// Use: [MvcCustomAuthorize] or [MvcCustomAuthorize(privilege = EnumPrivilegesName.CUSTOMERS)]
    /// Returns ForbidResult (403) when not authorized.
    /// </summary>
    public class MvcCustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string privilege { get; set; } = string.Empty;

        //public MvcCustomAuthorizeAttribute() { }

        public MvcCustomAuthorizeAttribute(string? value = null)
        {
            if (!string.IsNullOrEmpty(value))
                privilege = value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (string.IsNullOrEmpty(privilege))
            {
                if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Result = new ForbidResult();
                }
                return;
            }

            if (context.HttpContext.User.Identity?.IsAuthenticated == true && UserHasPrivilege(context))
            {
                return;
            }

            context.Result = new ForbidResult();
        }

        private bool UserHasPrivilege(AuthorizationFilterContext context)
        {
            var service = context.HttpContext.RequestServices.GetService(typeof(IPrivilegeAuthorizationService)) as IPrivilegeAuthorizationService;
            if (service == null)
                return false;

            var userName = context.HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return false;

            return service.UserHasPrivilegeAsync(userName, privilege).GetAwaiter().GetResult();
        }
    }
}
