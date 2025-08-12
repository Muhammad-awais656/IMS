using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace IMS.CommonUtilities
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly IStringLocalizer<GlobalExceptionFilter> _localizer;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IStringLocalizer<GlobalExceptionFilter> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred.");
            context.HttpContext.Session.SetString("ErrorMessage", context.Exception is InvalidOperationException
                ? context.Exception.Message
                : _localizer["ErrorMessage"].Value);
            context.Result = new RedirectToActionResult("Index", "Home", null);
            context.ExceptionHandled = true;
        }
    }
}
