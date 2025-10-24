using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NuGet.Protocol.Core.Types;
using System.Threading.Tasks;

namespace IMS.Controllers
{
    public class VendorController : Controller
    {
        private readonly ILogger<VendorController> _logger;
        private readonly IVendor _vndorservice;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };
        public VendorController(IVendor repository, ILogger<VendorController> logger)
        {
            _vndorservice = repository;
            _logger = logger;
        }
        // GET: CustomerController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string sidx = "Id", string sord = "asc", bool _search = false, string? Name = null, string? phoneNumber = null,string? NTN = null)
        {
            var viewModel = new VendorViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (Name == null)
                {
                    Name = HttpContext.Request.Query["searchUsername"].ToString();
                }
                if (phoneNumber==null)
                {
                    phoneNumber = HttpContext.Request.Query["searchContactNo"].ToString();
                }
                if (NTN==null)
                {
                    NTN = HttpContext.Request.Query["searchEmail"].ToString();
                }
                viewModel = await _vndorservice.GetAllVendors(pageNumber, currentPageSize, Name, phoneNumber, NTN);


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
            
        }
        

        // GET: CustomerController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CustomerController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CustomerController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminSupplier adminSupplier)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminSupplier.CreatedBy = userId;
                    adminSupplier.CreatedDate = DateTime.Now;
                    adminSupplier.ModifiedBy = userId;
                    adminSupplier.ModifiedDate = DateTime.Now;

                    var result = await _vndorservice.CreateVendorAsync(adminSupplier);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(adminSupplier);
            }
            return View(adminSupplier);
        }

        // GET: CustomerController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var unit = await _vndorservice.GetVendorByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return View(unit);
        }

        // POST: CustomerController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, AdminSupplier adminSupplier)
        {
            if (id != adminSupplier.SupplierId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    adminSupplier.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminSupplier.ModifiedBy = userId;
                    var response = await _vndorservice.UpdateVendorAsync(adminSupplier);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(adminSupplier);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(adminSupplier);
        }

        // GET: CustomerController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var vendor = new AdminSupplier();
            try
            {
                vendor = await _vndorservice.GetVendorByIdAsync(id);
                if (vendor == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(vendor);
        }

        // POST: CustomerController/Delete/5
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long id)
        {
            try
            {
                var res = await _vndorservice.DeleteVendorAsync(id);
                if (res != 0)
                {
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint to get vendors for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                var vendors = await _vndorservice.GetAllEnabledVendors();
                var result = vendors.Select(v => new
                {
                    value = v.SupplierId.ToString(),
                    text = v.SupplierName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // GET: VendorController/GenerateBill
        [HttpGet]
        public async Task<ActionResult> GenerateBill()
        {
            try
            {
                // Load vendors
                var vendors = await _vndorservice.GetAllEnabledVendors();
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName");

                // Get next bill number
                var nextBillNumber = await _vndorservice.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;

                return View(new VendorBillGenerationViewModel { BillDate = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Generate Bill page");
                TempData["ErrorMessage"] = "Error loading the Generate Bill page. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // AJAX endpoint to get products for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _vndorservice.GetAllEnabledProductsAsync();
                var result = products.Select(p => new
                {
                    value = p.ProductId.ToString(),
                    text = p.ProductName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get product sizes for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetProductSizes(long? productId)
        {
            try
            {
                if (!productId.HasValue)
                {
                    _logger.LogWarning("GetProductSizes called without productId");
                    return Json(new List<object>());
                }

                _logger.LogInformation("Getting product sizes for productId: {ProductId}", productId.Value);

                // Use the existing method from VendorService
                var productSizes = await _vndorservice.GetProductUnitPriceRangeByProductIdAsync(productId.Value);
                _logger.LogInformation("Retrieved {Count} product sizes from database", productSizes.Count);

                var result = productSizes.Select(ps => new
                {
                    value = ps.ProductRangeId.ToString(),
                    text = ps.RangeFrom == ps.RangeTo ? 
                        $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom} - ${ps.UnitPrice:F2}" :
                        $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom} to {ps.RangeTo} - ${ps.UnitPrice:F2}",
                    productRangeId = ps.ProductRangeId,
                    measuringUnitId = ps.MeasuringUnitId_FK,
                    rangeFrom = ps.RangeFrom,
                    rangeTo = ps.RangeTo,
                    unitPrice = ps.UnitPrice,
                    measuringUnitName = ps.MeasuringUnitName ?? "",
                    measuringUnitAbbreviation = ps.MeasuringUnitAbbreviation ?? ""
                }).ToList();
                
                _logger.LogInformation("Returning {Count} product sizes to frontend", result.Count);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product sizes for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get available stock
        [HttpGet]
        public async Task<JsonResult> GetAvailableStock(long productId)
        {
            try
            {
                var stock = await _vndorservice.GetStockByProductIdAsync(productId);
                if (stock != null)
                {
                    return Json(new { success = true, availableQuantity = stock.AvailableQuantity });
                }
                else
                {
                    return Json(new { success = false, message = "Stock information not found", availableQuantity = 0 });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available stock for product {ProductId}", productId);
                return Json(new { success = false, message = "Error retrieving stock information", availableQuantity = 0 });
            }
        }

        // AJAX endpoint to get next bill number
        [HttpGet]
        public async Task<JsonResult> GetNextBillNumber()
        {
            try
            {
                var nextBillNumber = await _vndorservice.GetNextBillNumberAsync();
                return Json(new { success = true, billNumber = nextBillNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number");
                return Json(new { success = false, message = "Error retrieving next bill number", billNumber = "" });
            }
        }

        // AJAX endpoint to get previous due amount
        [HttpGet]
        public async Task<JsonResult> GetPreviousDueAmount(long vendorId)
        {
            try
            {
                var previousDueAmount = await _vndorservice.GetPreviousDueAmountByVendorIdAsync(vendorId);
                return Json(new { success = true, previousDueAmount = previousDueAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for vendor {VendorId}", vendorId);
                return Json(new { success = false, message = "Error retrieving previous due amount", previousDueAmount = 0 });
            }
        }

        // AJAX endpoint to get online accounts
        [HttpGet]
        public async Task<JsonResult> GetOnlineAccounts()
        {
            try
            {
                var onlineAccounts = await _vndorservice.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                var accountOptions = onlineAccounts.PersonalPaymentList.Select(account => new
                {
                    value = account.PersonalPaymentId.ToString(),
                    text = $"{account.BankName} - {account.AccountNumber}",
                    personalPaymentId = account.PersonalPaymentId,
                    bankName = account.BankName,
                    accountNumber = account.AccountNumber,
                    accountHolderName = account.AccountHolderName
                }).ToList();

                return Json(accountOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online accounts");
                return Json(new List<object>());
            }
        }

        // POST: VendorController/GenerateBill
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateBill(VendorBillGenerationViewModel model)
        {
            try
            {
                _logger.LogInformation("=== GenerateBill POST method called ===");
                
                // Remove problematic BillDetails validation errors from ModelState since we handle them separately
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("BillDetails"))
                    {
                        keysToRemove.Add(key);
                    }
                    if (key.StartsWith("Description"))
                    {
                        keysToRemove.Add(key);
                    }
                    if (key.StartsWith("PayNow"))
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }
                
                // Validate only the main bill properties
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid - proceeding with bill creation");
                    
                    // Validate vendor selection
                    if (!model.VendorId.HasValue || model.VendorId == 0)
                    {
                        _logger.LogWarning("Vendor validation failed - VendorId is null or 0");
                        TempData["ErrorMessage"] = "Please select a vendor.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }

                    // Validate bill details
                    if (model.BillDetails == null || !model.BillDetails.Any())
                    {
                        _logger.LogWarning("Bill details validation failed - BillDetails is null or empty");
                        TempData["ErrorMessage"] = "Please add at least one product to the bill.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }

                    _logger.LogInformation("Bill details validation passed - {Count} items", model.BillDetails.Count);
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    DateTime currentDateTime = DateTime.Now;
                    long billId;

                    // Create new bill
                    _logger.LogInformation("Creating new vendor bill");
                    
                    billId = await _vndorservice.CreateVendorBillAsync(
                        model.TotalAmount,
                        model.PaidAmount,
                        model.DueAmount,
                        model.VendorId.Value,
                        currentDateTime,
                        userId,
                        currentDateTime,
                        userId,
                        model.DiscountAmount,
                        model.BillNumber,
                        model.Description ?? "",
                        model.BillDate,
                        model.PaymentMethod,
                        model.OnlineAccountId
                    );

                    if (billId > 0)
                    {
                        // Add bill details for each product
                        foreach (var detail in model.BillDetails)
                        {
                        
                            long billDetailsId = await _vndorservice.AddVendorBillDetails(
                                billId,
                                detail.ProductId,
                                detail.UnitPrice,
                                detail.PurchasePrice,
                                detail.Quantity,
                                detail.SalePrice,
                                detail.LineDiscountAmount,
                                detail.PayableAmount,
                                detail.ProductRangeId
                                
                            );

                            // Get stock information and update (INCREASE stock instead of decrease)
                            var prodMaster = await _vndorservice.GetStockByProductIdAsync(detail.ProductId);
                            if (prodMaster != null)
                            {
                                // Update stock quantity (INCREASE available quantity)
                                long updateStockReturn = _vndorservice.UpdateStock(
                                    prodMaster.StockMasterId,
                                    detail.ProductId,
                                    prodMaster.AvailableQuantity + (decimal)detail.Quantity, // INCREASE stock
                                    prodMaster.TotalQuantity + (decimal)detail.Quantity, // INCREASE total quantity
                                    prodMaster.UsedQuantity, // Keep used quantity same
                                    userId,
                                    currentDateTime
                                );

                                // Create bill transaction
                                long transactionReturn = _vndorservice.VendorBillTransactionCreate(
                                    prodMaster.StockMasterId,
                                    (decimal)detail.Quantity,
                                    $"Vendor Bill #{billId}",
                                    currentDateTime,
                                    userId,
                                    3, // Vendor bill transaction type
                                    billId
                                );
                            }
                        }

                        // Process online payment transaction if payment method is Online
                        if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                        {
                            try
                            {
                                var transactionDescription = $"Vendor Bill Credit - Bill #{model.BillNumber} - {model.Description}";
                                var transactionId = await _vndorservice.ProcessOnlinePaymentTransactionAsync(
                                    model.OnlineAccountId.Value,
                                    billId,
                                    model.PaidAmount, // Credit the paid amount to the online account
                                    transactionDescription,
                                    userId,
                                    currentDateTime
                                );
                                
                                _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Bill ID: {BillId}", 
                                    transactionId, billId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing online payment transaction for Bill ID: {BillId}", billId);
                                // Don't fail the entire bill if online payment processing fails
                                // Just log the error and continue
                            }
                        }

                        // Return JSON response only for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            // AJAX request - return JSON
                            string successMessage = "Vendor bill created successfully!";
                            if (model.ActionType == "saveAndPrint")
                            {
                                return Json(new { success = true, message = successMessage, billId = billId, print = true });
                            }
                            else
                            {
                                return Json(new { success = true, message = successMessage, billId = billId });
                            }
                        }
                        else
                        {
                            // Regular form submission - use TempData and redirect
                            string successMessage = "Vendor bill created successfully!";
                            TempData["Success"] = successMessage;
                            
                            if (model.ActionType == "saveAndPrint")
                            {
                                // Align behavior with SalesController: redirect to Details with print=true
                                return RedirectToAction("Details", "VendorBills", new { id = billId, print = true });
                            }
                            else
                            {
                                return RedirectToAction("GenerateBill");
                            }
                        }
                    }
                    else
                    {
                        string errorMessage = billId != 0 
                            ? $"Failed to create vendor bill. Error code: {billId}" 
                            : "Failed to create vendor bill. Bill ID was not generated.";
                        
                        // Return JSON response only for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
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
                    
                    // Return JSON response only for AJAX calls
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
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
                _logger.LogError(ex, "Error creating vendor bill");
                string errorMessage = ex.Message;
                
                // Return JSON response only for AJAX calls
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
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

        // GET: VendorController/PrintBill/5
        public async Task<ActionResult> PrintBill(long id)
        {
            try
            {
                var billPrint = await _vndorservice.GetVendorBillForPrintAsync(id);
                if (billPrint == null)
                {
                    return NotFound();
                }
                
                return View(billPrint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading print bill for vendor bill {BillId}", id);
                TempData["ErrorMessage"] = "Error loading bill for printing.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task ReloadViewDataAsync()
        {
            // Load products
            var products = await _vndorservice.GetAllEnabledProductsAsync();
            ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

            // Load vendors
            var vendors = await _vndorservice.GetAllEnabledVendors();
            ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName");

            // Get next bill number
            var nextBillNumber = await _vndorservice.GetNextBillNumberAsync();
            ViewBag.NextBillNumber = nextBillNumber;
        }
    }
}
