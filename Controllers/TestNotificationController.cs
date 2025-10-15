using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class TestNotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TestSuccess()
        {
            TempData["Success"] = "Success message from server!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult TestError()
        {
            TempData["ErrorMessage"] = "Error message from server!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult TestWarning()
        {
            TempData["WarningMessage"] = "Warning message from server!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult TestInfo()
        {
            TempData["InfoMessage"] = "Info message from server!";
            return RedirectToAction("Index");
        }
    }
}
