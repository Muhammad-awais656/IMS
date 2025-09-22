using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace IMS.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly ILogger<SalesController> _logger;
        private readonly IProductService _productService;
        private readonly ICustomer _customerService;

        public SalesController(ISalesService salesService, ILogger<SalesController> logger, IProductService productService, ICustomer customerService)
        {
            _salesService = salesService;
            _logger = logger;
            _productService = productService;
            _customerService = customerService;
        }

        // GET: SalesController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? searchCustomer = null, 
            long? customerId = null, long? billNumber = null, DateTime? saleFrom = null, DateTime? saleDateTo = null, 
            string? description = null)
        {
            try
            {
                var currentPageSize = pageSize ?? HttpContext.Session.GetInt32("PageSize") ?? 10;
                HttpContext.Session.SetInt32("PageSize", currentPageSize);

                // Check if there are any sales records at all
                var hasAnySales = await _salesService.HasAnySalesAsync();
                if (!hasAnySales)
                {
                    TempData["InfoMessage"] = "No sales records found in the database. You can create your first sale using the 'Add New Sale' button.";
                    return View(new SalesViewModel());
                }

                var stockFilters = new SalesFilters();
                if (!string.IsNullOrEmpty(searchCustomer))
                {
                    stockFilters.CustomerId = customerId;
                }
                else if (customerId.HasValue)
                {
                    stockFilters.CustomerId = customerId;
                }
                if (billNumber.HasValue)
                {
                    stockFilters.BillNumber = billNumber;
                }
                if (saleFrom.HasValue)
                {
                    stockFilters.SaleFrom = saleFrom;
                }
                if (saleDateTo.HasValue)
                {
                    stockFilters.SaleDateTo = saleDateTo;
                }
                if (!string.IsNullOrEmpty(description))
                {
                    stockFilters.Description = description;
                }

                var viewModel = await _salesService.GetAllSalesAsync(pageNumber, currentPageSize, stockFilters);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new SalesViewModel());
            }
        }

        // GET: SalesController/Details/5
        public async Task<ActionResult> Details(long id, bool? print = false)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }
                
                // Pass print parameter to view
                ViewBag.Print = print;
                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SalesController/Create
        public async Task<ActionResult> Create()
        {
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
                
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View();
        }

        // POST: SalesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Sale sale)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    sale.CreatedBy = userId;
                    sale.CreatedDate = DateTime.Now;
                    sale.ModifiedBy = userId;
                    sale.ModifiedDate = DateTime.Now;

                    var result = await _salesService.CreateSaleAsync(sale);
                    if (result)
                    {
                        TempData["Success"] = "Sale created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create sale.";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            
            // Ensure ViewBag is populated when returning View
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
                
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
            }
            
            return View();
        }

        // GET: SalesController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }

                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", sale.CustomerIdFk);

                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, Sale sale)
        {
            if (id != sale.SaleId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    sale.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    sale.ModifiedBy = userId;

                    var response = await _salesService.UpdateSaleAsync(sale);
                    
                    if (response != 0)
                    {
                        TempData["Success"] = "Sale updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update sale.";
                        return View(sale);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            
            // Ensure ViewBag is populated when returning View
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", sale.CustomerIdFk);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
            }
            
            return View(sale);
        }

        // GET: SalesController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }
                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr);
                var modifiedDate = DateTime.Now;
                
                var res = await _salesService.DeleteSaleAsync(id, modifiedDate, userId);
                if (res != 0)
                {
                                            TempData["Success"] = "Sale deleted successfully!";
                        return RedirectToAction(nameof(Index));
                }
                else
                {
                                            TempData["ErrorMessage"] = "Failed to delete sale.";
                        return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ReloadViewDataAsync()
        {
            // Load products
            var products = await _productService.GetAllEnabledProductsAsync();
            ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

            // Load customers
            var customers = await _salesService.GetAllCustomersAsync();
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

            // Get next bill number
            var nextBillNumber = await _salesService.GetNextBillNumberAsync();
            ViewBag.NextBillNumber = nextBillNumber;
        }
        [HttpGet]
        public async Task<ActionResult> AddSale()
        {
            try
            {
                // Load products
                var products = await _productService.GetAllEnabledProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

                // Load customers
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

                // Get next bill number
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;


                return View(new AddSaleViewModel { SaleDate = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Add Sale page");
                TempData["ErrorMessage"] = "Error loading the Add Sale page. Please try again.";
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetProductSizes(long productId)
        {
            try
            {
                var productSizes = await _salesService.GetProductUnitPriceRangeByProductIdAsync(productId);
                var sizeOptions = productSizes.Select(ps => new
                {
                    value = ps.ProductRangeId,
                    text = $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom}-{ps.RangeTo} - ${ps.UnitPrice:F2}",
                    productRangeId = ps.ProductRangeId,
                    measuringUnitId = ps.MeasuringUnitId_FK,
                    rangeFrom = ps.RangeFrom,
                    rangeTo = ps.RangeTo,
                    unitPrice = ps.UnitPrice,
                    measuringUnitName = ps.MeasuringUnitName,
                    measuringUnitAbbreviation = ps.MeasuringUnitAbbreviation
                }).ToList();

                return Json(sizeOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product sizes");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPreviousDueAmount(long customerId)
        {
            try
            {
                var previousDueAmount = await _salesService.GetPreviousDueAmountByCustomerIdAsync(customerId);
                return Json(new { success = true, previousDueAmount = previousDueAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for customer {CustomerId}", customerId);
                return Json(new { success = false, message = "Error retrieving previous due amount", previousDueAmount = 0 });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetNextBillNumber()
        {
            try
            {
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                return Json(new { success = true, billNumber = nextBillNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number");
                return Json(new { success = false, message = "Error retrieving next bill number", billNumber = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddSale(AddSaleViewModel model)
        {
            try
            {
                _logger.LogInformation("=== AddSale POST method called ===");
                
                // Remove problematic SaleDetails validation errors from ModelState since we handle them separately
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("SaleDetails"))
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }
                
                // Log raw form data for debugging
                _logger.LogInformation("Raw form data count: {Count}", Request.Form.Count);
                if (Request.Form.Count == 0)
                {
                    _logger.LogWarning("No form data received - this indicates form submission issue");
                    TempData["ErrorMessage"] = "No form data received. Please try again.";
                    await ReloadViewDataAsync();
                    return View(model);
                }
                
                // Log form data for debugging
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation("Form Key: {Key}, Value: {Value}", key, Request.Form[key]);
                }
                
                // Validate only the main sale properties
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid - proceeding with sale creation");
                    
                    // Validate customer selection
                    if (!model.CustomerId.HasValue || model.CustomerId == 0)
                    {
                        _logger.LogWarning("Customer validation failed - CustomerId is null or 0");
                        TempData["ErrorMessage"] = "Please select a customer.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }

                    // Validate sale details
                    if (model.SaleDetails == null || !model.SaleDetails.Any())
                    {
                        _logger.LogWarning("Sale details validation failed - SaleDetails is null or empty");
                        TempData["ErrorMessage"] = "Please add at least one product to the sale.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }
                    
                    _logger.LogInformation("Sale details validation passed - {Count} items", model.SaleDetails.Count);
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    DateTime currentDateTime = DateTime.Now;

                    // Add the sale header
                    var saleId = await _salesService.CreateSaleAsync(
                        model.TotalAmount,
                        model.ReceivedAmount,
                        model.DueAmount,
                        model.CustomerId.Value,
                        currentDateTime,
                        userId,
                        currentDateTime,
                        userId,
                        model.DiscountAmount,
                        long.Parse(model.BillNo ?? "0"),
                        model.Description ?? "",
                        model.SaleDate
                    );

                    if (saleId > 0)
                    {
                        // Add sale details for each product
                        foreach (var detail in model.SaleDetails)
                        {
                            int detailReturnValue;
                            long saleDetailsId = _salesService.AddSaleDetails(
                                saleId,
                                detail.ProductId,
                                detail.UnitPrice,
                                detail.Quantity,
                                detail.SalePrice,
                                detail.LineDiscountAmount,
                                detail.PayableAmount,
                                detail.ProductRangeId,
                                out detailReturnValue
                            );

                            // Get stock information and update
                            var prodMaster = await _salesService.GetStockByProductIdAsync(detail.ProductId);
                            if (prodMaster!=null)
                            {
                                // Update stock quantity
                                long updateStockReturn = _salesService.UpdateStock(
                                    prodMaster.StockMasterId,
                                    detail.ProductId,
                                    prodMaster.AvailableQuantity - (decimal)detail.Quantity, // This should be calculated based on current stock
                                    prodMaster.TotalQuantity,
                                    prodMaster.AvailableQuantity + (decimal)detail.Quantity,
                                    userId,
                                    currentDateTime
                                );

                                // Create sale transaction
                                long transactionReturn = _salesService.SaleTransactionCreate(
                                    prodMaster.StockMasterId,
                                    (decimal)detail.Quantity,
                                    $"Sale #{saleId}",
                                    currentDateTime,
                                    userId,
                                    2, // Sale transaction
                                    saleId
                                );
                            }
                        }

                        // Return JSON response for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                            Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                        {
                            // AJAX request - return JSON
                            if (model.ActionType == "saveAndPrint")
                            {
                                return Json(new { success = true, message = "Sale created successfully!", saleId = saleId, print = true });
                            }
                            else
                            {
                                return Json(new { success = true, message = "Sale created successfully!", saleId = saleId });
                            }
                        }
                        else
                        {
                            // Regular form submission - use TempData and redirect
                            TempData["Success"] = "Sale created successfully!";
                            
                            if (model.ActionType == "saveAndPrint")
                            {
                                return RedirectToAction("Details", new { id = saleId, print = true });
                            }
                            else
                            {
                                return RedirectToAction("AddSale");
                            }
                        }
                    }
                    else
                    {
                        string errorMessage = saleId != 0 
                            ? $"Failed to create sale. Error code: {saleId}" 
                            : "Failed to create sale. Sale ID was not generated.";
                        
                        // Return JSON response for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                            Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                        {
                            return Json(new { success = false, message = errorMessage });
                        }
                        else
                        {
                            TempData["ErrorMessage"] = errorMessage;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid - validation failed");
                    string errorMessage = "Please check the form data and try again.";
                    
                    // Return JSON response for AJAX calls
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                        Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                    {
                        return Json(new { success = false, message = errorMessage });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                string errorMessage = ex.Message;
                
                // Return JSON response for AJAX calls
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    return Json(new { success = false, message = errorMessage });
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                }
            }
            
            // Reload the view with necessary data if validation fails
            await ReloadViewDataAsync();
            return View(model);
        }

    }
}
