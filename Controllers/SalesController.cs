using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Sales()
        {
            return View();
        }
        public IActionResult AddSale()
        {
            return View();
        }
    }
}
