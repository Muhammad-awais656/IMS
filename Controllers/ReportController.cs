using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace IMS.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult SalesReport()
        {
            return View("~/Views/Reports/SalesReport.cshtml");
        }
    }
}
