using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
                    billDateFrom = DateTime.Now.AddMonths(-1);
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
        public async Task<IActionResult> GetPreviousDue(long vendorId)
        {
            try
            {
                var previousDue = await _vendorBillsService.GetPreviousDueAmountAsync(vendorId);
                return Json(new { previousDue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for vendor {VendorId}", vendorId);
                return Json(new { previousDue = 0 });
            }
        }

        // GET: VendorBillsController/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // Load products using the same pattern as SalesController
                var products = await _vendorBillsService.GetAllProductsAsync();
                ViewBag.Products = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(products, "ProductId", "ProductName");
                
                var viewModel = new GenerateBillViewModel
                {
                    VendorList = await _vendorBillsService.GetAllVendorsAsync(),
                    ProductList = products,
                    BillDate = DateTime.Today
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create bill page");
                TempData["ErrorMessage"] = "An error occurred while loading the create bill page.";
                return View(new GenerateBillViewModel());
            }
        }

        // POST: VendorBillsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GenerateBillViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Process the bill creation
                    var result = await _vendorBillsService.CreateBillAsync(model);
                    
                    if (result > 0)
                    {
                        TempData["SuccessMessage"] = "Bill created successfully!";
                        if (model.ActionType == "saveAndPrint")
                        {
                            return RedirectToAction("Details", new { id = result, print = true });
                        }
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create bill.";
                    }
                }
                
                // Reload data for the view
                var products = await _vendorBillsService.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");
                model.VendorList = await _vendorBillsService.GetAllVendorsAsync();
                model.ProductList = products;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bill");
                TempData["ErrorMessage"] = "An error occurred while creating the bill.";
                var products = await _vendorBillsService.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");
                model.VendorList = await _vendorBillsService.GetAllVendorsAsync();
                model.ProductList = products;
                return View(model);
            }
        }

        // AJAX endpoint to get product sizes
        [HttpGet]
        public async Task<IActionResult> GetProductSizes(long productId)
        {
            try
            {
                var productSizes = await _vendorBillsService.GetProductSizesAsync(productId);
                var result = productSizes.Select(ps => new
                {
                    value = ps.ProductRangeId,
                    text = $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom}-{ps.RangeTo} - ${ps.UnitPrice:F2}",
                    productRangeId = ps.ProductRangeId,
                    measuringUnitId = ps.MeasuringUnitIdFk,
                    rangeFrom = ps.RangeFrom,
                    rangeTo = ps.RangeTo,
                    unitPrice = ps.UnitPrice,
                    measuringUnitName = ps.MeasuringUnitName,
                    measuringUnitAbbreviation = ps.MeasuringUnitAbbreviation
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product sizes for product {ProductId}", productId);
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get previous due amount
        //[HttpGet]
        //public async Task<IActionResult> GetPreviousDue(long vendorId)
        //{
        //    try
        //    {
        //        var previousDue = await _vendorBillsService.GetPreviousDueAmountAsync(vendorId);
        //        var result = new
        //        {
        //            previousDue = previousDue
        //        };
                
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting previous due for vendor {VendorId}", vendorId);
        //        return Json(new { previousDue = 0 });
        //    }
        //}

        // GET: VendorBillsController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                // Get the vendor bill with details
                var vendorBill = await _vendorBillsService.GetVendorBillByIdAsync(id);
                if (vendorBill == null)
                {
                    return NotFound();
                }

                // Get bill items
                var billItems = await _vendorBillsService.GetVendorBillItemsAsync(id);
                
                // Load products
                var products = await _vendorBillsService.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

                // Load vendors
                var vendors = await _vendorBillsService.GetAllVendorsAsync();
                ViewBag.Vendors = new SelectList(vendors, "VendorId", "VendorName", vendorBill.VendorId);

                // Create GenerateBillViewModel with existing data
                var viewModel = new GenerateBillViewModel
                {
                    BillId = vendorBill.BillId,
                    VendorId = vendorBill.VendorId,
                    BillNumber = vendorBill.BillNumber,
                    BillDate = vendorBill.BillDate,
                    TotalAmount = vendorBill.TotalAmount,
                    DiscountAmount = vendorBill.DiscountAmount,
                    PaidAmount = vendorBill.PaidAmount,
                    DueAmount = vendorBill.DueAmount,
                    Description = vendorBill.Description,
                    BillItems = billItems
                };

                ViewBag.IsEdit = true;
                return View("Create", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit Vendor Bill page for ID: {BillId}", id);
                TempData["ErrorMessage"] = "Error loading vendor bill for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorBillsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, GenerateBillViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Update the vendor bill
                    var success = await _vendorBillsService.UpdateVendorBillAsync(id, model);
                    
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Vendor bill updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error updating vendor bill.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please correct the validation errors.";
                }

                // Reload data for the view
                var products = await _vendorBillsService.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

                var vendors = await _vendorBillsService.GetAllVendorsAsync();
                ViewBag.Vendors = new SelectList(vendors, "VendorId", "VendorName", model.VendorId);

                ViewBag.IsEdit = true;
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor bill {BillId}", id);
                TempData["ErrorMessage"] = "Error updating vendor bill.";
                return RedirectToAction(nameof(Index));
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
