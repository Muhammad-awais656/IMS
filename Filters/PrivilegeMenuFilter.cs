using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IMS.Filters
{
    /// <summary>
    /// Populates ViewBag.IsAdmin and ViewBag.UserPrivileges for layout menu visibility.
    /// Run after authorization so we only hit the DB for authenticated users.
    /// </summary>
    public class PrivilegeMenuFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var service = context.HttpContext.RequestServices.GetService(typeof(IPrivilegeAuthorizationService)) as IPrivilegeAuthorizationService;
                if (service != null)
                {
                    var userName = user.Identity.Name;
                    var (isAdmin, privilegeNames) = await service.GetUserPrivilegeNamesAsync(userName);
                    if (context.Controller is Controller controller)
                    {
                        controller.ViewBag.IsAdmin = isAdmin;
                        controller.ViewBag.UserPrivileges = privilegeNames;
                    }
                }
            }

            await next();
        }
    }
}
