using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class TestReceiptController : Controller
    {
        private readonly IReceiptService _receiptService;
        private readonly ILogger<TestReceiptController> _logger;

        public TestReceiptController(IReceiptService receiptService, ILogger<TestReceiptController> logger)
        {
            _receiptService = receiptService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var templates = await _receiptService.GetAvailableTemplatesAsync();
                return View(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading receipt templates for test");
                TempData["ErrorMessage"] = "Error loading receipt templates.";
                return View(new List<ReceiptTemplate>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestReceipt(long saleId = 1, ReceiptType templateType = ReceiptType.SalesInvoice)
        {
            try
            {
                var request = new ReceiptGenerationRequest
                {
                    SaleId = saleId,
                    TemplateType = templateType
                };

                var htmlContent = await _receiptService.GenerateReceiptAsync(request);
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
