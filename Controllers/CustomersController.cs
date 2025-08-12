using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class CustomersController : Controller
    {
        public IActionResult Customers()
        {
            return View();
        }
        public IActionResult AddCustomer()
        {
            return View();
        }
    }
}
