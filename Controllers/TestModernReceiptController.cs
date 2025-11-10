using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class TestModernReceiptController : Controller
    {
        private readonly IModernReceiptService _modernReceiptService;
        private readonly ILogger<TestModernReceiptController> _logger;

        public TestModernReceiptController(IModernReceiptService modernReceiptService, ILogger<TestModernReceiptController> logger)
        {
            _modernReceiptService = modernReceiptService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var templates = new List<object>
            {
                new { Name = "Simple Invoice", Type = ModernReceiptType.Simple, Description = "Simple sales invoice template" },
                new { Name = "Detailed Invoice", Type = ModernReceiptType.Detailed, Description = "Detailed sales invoice with company branding" },
                new { Name = "Compact Receipt", Type = ModernReceiptType.Compact, Description = "Compact sales invoice for thermal printers" },
                new { Name = "General Report", Type = ModernReceiptType.GeneralReport, Description = "General purpose report template" }
            };
            return View(templates);
        }

        [HttpGet]
        public async Task<IActionResult> TestReceipt(long saleId = 1, ModernReceiptType templateType = ModernReceiptType.Simple)
        {
            try
            {
                var htmlContent = await _modernReceiptService.GenerateReceiptHtmlAsync(saleId, templateType);
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test receipt for sale {SaleId}", saleId);
                return Content($"<html><body><h1>Error generating receipt</h1><p>{ex.Message}</p></body></html>", "text/html");
            }
        }
    }
}
