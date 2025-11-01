using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace IMS.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
    }
}
