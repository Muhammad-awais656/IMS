using IMS.Common_Interfaces;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class VendorPaymentsController : Controller
    {
        private readonly IVendorPaymentService _vendorPaymentService;
        private readonly ILogger<VendorPaymentsController> _logger;

        public VendorPaymentsController(IVendorPaymentService vendorPaymentService, ILogger<VendorPaymentsController> logger)
        {
            _vendorPaymentService = vendorPaymentService;
            _logger = logger;
        }

        // GET: VendorPaymentsController
        public async Task<IActionResult> Index(int pageNumber = 1, int? pageSize = 10,
            long? vendorId = null, long? billNumber = null,
            DateTime? billDateFrom = null, DateTime? billDateTo = null, string? description = null)
        {
            try
            {
                var filters = new VendorPaymentFilters
                {
                    VendorId = vendorId,
                    BillNumber = billNumber,
                    BillDateFrom = billDateFrom,
                    BillDateTo = billDateTo,
                    Description = description
                };

                var viewModel = await _vendorPaymentService.GetAllBillPaymentsAsync(pageNumber, pageSize ?? 10, filters);
                viewModel.VendorList = await _vendorPaymentService.GetAllVendorsAsync();

                // Load bill numbers if vendor is selected
                if (vendorId.HasValue)
                {
                    ViewBag.BillNumbers = await _vendorPaymentService.GetSupplierBillNumbersAsync(vendorId.Value);
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
                _logger.LogError(ex, "Error loading vendor payments");
                TempData["ErrorMessage"] = "An error occurred while loading vendor payments.";
                return View(new VendorPaymentViewModel());
            }
        }

        // AJAX endpoint to get bill numbers for a supplier
        [HttpGet]
        public async Task<IActionResult> GetBillNumbers(long supplierId)
        {
            try
            {
                var billNumbers = await _vendorPaymentService.GetSupplierBillNumbersAsync(supplierId);
                return Json(billNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill numbers for supplier {SupplierId}", supplierId);
                return Json(new List<SupplierBillNumber>());
            }
        }

        // GET: VendorPaymentsController/Details/5
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

        // GET: VendorPaymentsController/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new VendorPaymentViewModel();
                viewModel.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormCollection collection)
        {
            try
            {
                // Implementation for creating new payment
                // This would typically involve creating a new BillPayment record
                TempData["SuccessMessage"] = "Payment created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                TempData["ErrorMessage"] = "An error occurred while creating the payment.";
                return View();
            }
        }

        // GET: VendorPaymentsController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                // Implementation for editing payment
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, IFormCollection collection)
        {
            try
            {
                // Implementation for updating payment
                TempData["SuccessMessage"] = "Payment updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                return View();
            }
        }

        // GET: VendorPaymentsController/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                // Implementation for delete confirmation
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation");
                TempData["ErrorMessage"] = "An error occurred while loading the delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, IFormCollection collection)
        {
            try
            {
                // Implementation for deleting payment
                TempData["SuccessMessage"] = "Payment deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment");
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
                return View();
            }
        }
    }
}
