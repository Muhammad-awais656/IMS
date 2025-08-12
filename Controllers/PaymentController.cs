using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Payment()
        {
            return View();
        }
        public IActionResult AddPayment()
        {
            return View(); 
        }

    }
}
