using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly IModernReceiptService _modernReceiptService;
        private readonly ILogger<ReceiptController> _logger;

        public ReceiptController(IModernReceiptService modernReceiptService, ILogger<ReceiptController> logger)
        {
            _modernReceiptService = modernReceiptService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Generate(long saleId, ModernReceiptType templateType = ModernReceiptType.Simple)
        {
            try
            {
                var htmlContent = await _modernReceiptService.GenerateReceiptHtmlAsync(saleId, templateType);
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt for sale {SaleId}", saleId);
                TempData["ErrorMessage"] = "Error generating receipt. Please try again.";
                return RedirectToAction("Index", "Sales");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(long saleId, ModernReceiptType templateType = ModernReceiptType.Simple)
        {
            try
            {
                var htmlContent = await _modernReceiptService.GenerateReceiptHtmlAsync(saleId, templateType);
                
                // Add print-specific CSS
                var printHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Receipt - Sale #{saleId}</title>
    <style>
        @media print {{
            body {{ margin: 0; padding: 0; }}
            .no-print {{ display: none !important; }}
        }}
        .print-button {{
            position: fixed;
            top: 10px;
            right: 10px;
            z-index: 1000;
            background: #007bff;
            color: white;
            border: none;
            padding: 10px 20px;
            border-radius: 5px;
            cursor: pointer;
        }}
        .print-button:hover {{
            background: #0056b3;
        }}
    </style>
</head>
<body>
    <button class='print-button no-print' onclick='window.print()'>Print Receipt</button>
    {htmlContent}
</body>
</html>";

                return Content(printHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating printable receipt for sale {SaleId}", saleId);
                TempData["ErrorMessage"] = "Error generating printable receipt. Please try again.";
                return RedirectToAction("Index", "Sales");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(long saleId, ModernReceiptType templateType = ModernReceiptType.Simple)
        {
            try
            {
                var pdfBytes = await _modernReceiptService.GenerateReceiptPdfAsync(saleId, templateType);
                var fileName = $"Receipt_Sale_{saleId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                
                return File(pdfBytes, "text/html", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading receipt for sale {SaleId}", saleId);
                TempData["ErrorMessage"] = "Error downloading receipt. Please try again.";
                return RedirectToAction("Index", "Sales");
            }
        }

        [HttpGet]
        public IActionResult Templates()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading receipt templates");
                TempData["ErrorMessage"] = "Error loading receipt templates.";
                return View(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCustom(long saleId, ModernReceiptType templateType)
        {
            try
            {
                var htmlContent = await _modernReceiptService.GenerateReceiptHtmlAsync(saleId, templateType);
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom receipt");
                TempData["ErrorMessage"] = "Error generating custom receipt. Please try again.";
                return RedirectToAction("Index", "Sales");
            }
        }
    }
}
