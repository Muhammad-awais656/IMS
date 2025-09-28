using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class VendorBillsController : Controller
    {
        private readonly IVendorBillsService _vendorBillsService;
        private readonly ILogger<VendorBillsController> _logger;

        public VendorBillsController(IVendorBillsService vendorBillsService, ILogger<VendorBillsController> logger)
        {
            _vendorBillsService = vendorBillsService;
            _logger = logger;
        }

        // GET: VendorBillsController
        public async Task<IActionResult> Index(int pageNumber = 1, int? pageSize = 10,
            long? vendorId = null, long? billNumber = null,
            DateTime? billDateFrom = null, DateTime? billDateTo = null, string? description = null)
        {
            try
            {
                // Set today's date as default if no dates are provided
                var today = DateTime.Today;
                if (!billDateFrom.HasValue)
                    billDateFrom = today;
                if (!billDateTo.HasValue)
                    billDateTo = today;

                var filters = new VendorBillsFilters
                {
                    VendorId = vendorId,
                    BillNumber = billNumber,
                    BillDateFrom = billDateFrom,
                    BillDateTo = billDateTo,
                    Description = description
                };

                var viewModel = await _vendorBillsService.GetAllBillsAsync(pageNumber, pageSize ?? 10, filters);
                viewModel.VendorList = await _vendorBillsService.GetAllVendorsAsync();

                // Load bill numbers if vendor is selected
                if (vendorId.HasValue)
                {
                    ViewBag.BillNumbers = await _vendorBillsService.GetSupplierBillNumbersAsync(vendorId.Value);
                }

                // Store filter values in ViewData for form persistence
                ViewData["vendorId"] = vendorId;
                ViewData["billNumber"] = billNumber;
                ViewData["billDateFrom"] = billDateFrom?.ToString("yyyy-MM-dd");
                ViewData["billDateTo"] = billDateTo?.ToString("yyyy-MM-dd");
                ViewData["description"] = description;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vendor bills");
                TempData["ErrorMessage"] = "An error occurred while loading vendor bills.";
                return View(new VendorBillsViewModel());
            }
        }

        // AJAX endpoint to get bill numbers for a supplier
        [HttpGet]
        public async Task<IActionResult> GetBillNumbers(long supplierId)
        {
            try
            {
                var billNumbers = await _vendorBillsService.GetSupplierBillNumbersAsync(supplierId);
                return Json(billNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill numbers for supplier {SupplierId}", supplierId);
                return Json(new List<SupplierBillNumber>());
            }
        }

        // GET: VendorBillsController/Details/5
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                // For now, redirect to index with the bill ID as a filter
                return RedirectToAction(nameof(Index), new { billNumber = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bill details");
                TempData["ErrorMessage"] = "An error occurred while loading bill details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VendorBillsController/GenerateBill
        public async Task<IActionResult> GenerateBill(long? vendorId = null)
        {
            try
            {
                var viewModel = await _vendorBillsService.GetBillGenerationDataAsync(vendorId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading generate bill form");
                TempData["ErrorMessage"] = "An error occurred while loading the generate bill form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX endpoint to get vendor products
        [HttpGet]
        public async Task<IActionResult> GetVendorProducts(long supplierId)
        {
            try
            {
                var products = await _vendorBillsService.GetVendorProductsAsync(supplierId);
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor products for supplier {SupplierId}", supplierId);
                return Json(new List<Product>());
            }
        }

        // AJAX endpoint to get product unit price ranges
        [HttpGet]
        public async Task<IActionResult> GetProductRanges(long productId)
        {
            try
            {
                var productRanges = await _vendorBillsService.GetProductUnitPriceRangesAsync(productId);
                return Json(productRanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product ranges for product {ProductId}", productId);
                return Json(new List<ProductRange>());
            }
        }

        // AJAX endpoint to get next bill number
        [HttpGet]
        public async Task<IActionResult> GetNextBillNumber(long supplierId)
        {
            try
            {
                var billNumber = await _vendorBillsService.GetNextBillNumberAsync(supplierId);
                return Json(new { billNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number for supplier {SupplierId}", supplierId);
                return Json(new { billNumber = 1 });
            }
        }

        // AJAX endpoint to get previous due amount
        [HttpGet]
        public async Task<IActionResult> GetPreviousDue(long? billId, long vendorId)
        {
            try
            {
                var previousDue = await _vendorBillsService.GetPreviousDueAmountAsync(billId, vendorId);
                return Json(new { previousDue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for bill {BillId} and vendor {VendorId}", billId, vendorId);
                return Json(new { previousDue = 0 });
            }
        }

        // GET: VendorBillsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VendorBillsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: VendorBillsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: VendorBillsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: VendorBillsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: VendorBillsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
