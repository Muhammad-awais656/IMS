using System.Diagnostics;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;


namespace IMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
      

        public IActionResult Index()
        {
            ViewBag.TotalEmployees = 70;
            return View();
        }
        [HttpGet]
        public IActionResult Ping()
        {
            // Touch session to keep it alive
            HttpContext.Session.SetString("Ping", DateTime.Now.ToString());
            return Ok();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
