using IMS.Common_Interfaces;
using IMS.CommonUtilities;
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
        private readonly IVendor _vendorService;
        private readonly ILogger<VendorBillsController> _logger;

        public VendorBillsController(IVendorBillsService vendorBillsService, IVendor vendorService, ILogger<VendorBillsController> logger)
        {
            _vendorBillsService = vendorBillsService;
            _vendorService = vendorService;
            _logger = logger;
        }

        // GET: VendorBillsController
        public async Task<IActionResult> Index(int pageNumber = 1, int? pageSize = 10,
            long? vendorId = null, long? billNumber = null,
            DateTime? billDateFrom = null, DateTime? billDateTo = null, string? description = null)
        {
            try
            {
                // Set today's date as default if no dates are provided (Pakistan time)
                var today = DateTimeHelper.Today;
                if (!billDateFrom.HasValue)
                    billDateFrom = DateTimeHelper.Now.AddMonths(-1);
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
        public async Task<IActionResult> GetSupplierBillNumbers(long supplierId)
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
        public async Task<IActionResult> Details(long id, bool? print = false)
        {
            try
            {
                var vendorBill = await _vendorBillsService.GetVendorBillByIdAsync(id);
                if (vendorBill == null)
                {
                    return NotFound();
                }
                
                var billItems = await _vendorBillsService.GetVendorBillItemsAsync(id);
                
                var detailsModel = new
                {
                    Bill = vendorBill,
                    BillItems = billItems
                };
                
                // Pass print parameter to view
                ViewBag.Print = print;
                return View(detailsModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bill details for bill {BillId}", id);
                TempData["ErrorMessage"] = "Error loading bill details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VendorBillsController/GenerateBill
        public async Task<ActionResult> GenerateBill()
        {
            try
            {
                _logger.LogInformation("Loading Generate Bill page");
                
                // Load vendors
                _logger.LogInformation("Loading vendors...");
                var vendors = await _vendorBillsService.GetAllVendorsAsync();
                _logger.LogInformation("Loaded {VendorCount} vendors", vendors.Count);
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName");

                // Get next bill number
                _logger.LogInformation("Getting next bill number...");
                var nextBillNumber = await _vendorBillsService.GetNextBillNumberAsync(0); // Default vendor ID
                _logger.LogInformation("Next bill number: {BillNumber}", nextBillNumber);
                ViewBag.NextBillNumber = nextBillNumber;

                return View(new VendorBillGenerationViewModel { 
                    BillDate = DateTimeHelper.Now,
                    BillNumber = nextBillNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Generate Bill page");
                TempData["ErrorMessage"] = $"Error loading the Generate Bill page: {ex.Message}";
                return RedirectToAction("Index");
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
        public async Task<IActionResult> GetNextBillNumber(long? vendorId = null)
        {
            try
            {
                _logger.LogInformation("Getting next bill number for vendor: {VendorId}", vendorId);
                var billNumber = await _vendorBillsService.GetNextBillNumberAsync(vendorId ?? 0);
                _logger.LogInformation("Retrieved bill number: {BillNumber} for vendor: {VendorId}", billNumber, vendorId);
                return Json(new { success = true, billNumber = billNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number for vendor {VendorId}", vendorId);
                return Json(new { success = false, message = "Error retrieving next bill number", billNumber = 1 });
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
                
                // Get next bill number (default to 1 if no vendor selected)
                var nextBillNumber = 1;
                
                var viewModel = new GenerateBillViewModel
                {
                    VendorList = await _vendorBillsService.GetAllVendorsAsync(),
                    ProductList = products,
                    BillDate = DateTimeHelper.Today,
                    BillNumber = nextBillNumber
                };
                
                ViewBag.NextBillNumber = nextBillNumber;
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
                _logger.LogInformation("Create POST called with ActionType: '{ActionType}'", model.ActionType ?? "NULL");
                
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    model.CreatedBy = userId;
                    // Process the bill creation
                    var result = await _vendorBillsService.CreateBillAsync(model);
                    
                    if (result > 0)
                    {
                        // Regular form submission - use TempData and redirect
                        string successMessage = "Bill created successfully!";
                        TempData["Success"] = successMessage;
                        _logger.LogInformation("Bill created successfully with ID: {BillId}, ActionType: '{ActionType}'", result, model.ActionType ?? "NULL");
                        
                        if (model.ActionType == "saveAndPrint")
                        {
                            _logger.LogInformation("Redirecting to Details for BillId: {BillId} with print=true", result);
                            return RedirectToAction("Details", new { id = result, print = true });
                        }
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create bill.";
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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

        // AJAX endpoint to get products for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _vendorBillsService.GetAllProductsAsync();
                var result = products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
                    {
                        value = p.ProductId.ToString(),
                        text = p.ProductName,
                        code = p.ProductCode ?? ""
                    }).ToList();
                
                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for Kendo combobox");
                return Json(new { data = new List<object>() });
            }
        }

        // AJAX endpoint to get vendors for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                _logger.LogInformation("Getting vendors for Kendo combobox");
                var vendors = await _vendorBillsService.GetAllVendorsAsync();
                _logger.LogInformation("Retrieved {VendorCount} vendors", vendors.Count);
                
                var result = vendors.Select(v => new
                {
                    value = v.SupplierId.ToString(),
                    text = v.SupplierName
                }).ToList();
                
                _logger.LogInformation("Returning {ResultCount} vendor options", result.Count);
                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors for Kendo combobox");
                return Json(new { data = new List<object>(), error = ex.Message });
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

                // Use the existing method from VendorBillsService
                var productSizes = await _vendorBillsService.GetProductSizesAsync(productId.Value);
                _logger.LogInformation("Retrieved {Count} product sizes from database", productSizes.Count);

                var result = productSizes.Select(ps => new
                {
                    value = ps.ProductRangeId.ToString(),
                    text = ps.RangeFrom == ps.RangeTo ? 
                        $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom} - ${ps.UnitPrice:F2}" :
                        $"{ps.MeasuringUnitName} ({ps.MeasuringUnitAbbreviation}) - {ps.RangeFrom} to {ps.RangeTo} - ${ps.UnitPrice:F2}",
                    productRangeId = ps.ProductRangeId,
                    measuringUnitId = ps.MeasuringUnitIdFk,
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
                var stock = await _vendorService.GetStockByProductIdAsync(productId);
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


        // AJAX endpoint to get previous due amount
        [HttpGet]
        public async Task<JsonResult> GetPreviousDueAmount(long vendorId)
        {
            try
            {
                var previousDueAmount = await _vendorBillsService.GetPreviousDueAmountAsync(vendorId);
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
                var onlineAccounts = await _vendorService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                var accountOptions = onlineAccounts.PersonalPaymentList.Select(account => new
                {
                    value = account.PersonalPaymentId.ToString(),
                    text = $"{account.BankName} - {account.AccountHolderName}",
                    personalPaymentId = account.PersonalPaymentId,
                    bankName = account.BankName,
                    accountNumber = account.AccountNumber,
                    accountHolderName = account.AccountHolderName
                }).ToList();

                return Json(new { data = accountOptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online accounts");
                return Json(new { data = new List<object>() });
            }
        }

        // AJAX endpoint to get account balance
        [HttpGet]
        public async Task<JsonResult> GetAccountBalance(long accountId)
        {
            try
            {
                _logger.LogInformation("Getting account balance for account: {AccountId}", accountId);
                
                // Use the new VendorBillsService method with SQL query
                var currentBalance = await _vendorBillsService.GetAccountBalanceAsync(accountId);
                
                _logger.LogInformation("Account balance retrieved: {Balance} for account: {AccountId}", currentBalance, accountId);
                
                return Json(new { success = true, balance = currentBalance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account balance for account: {AccountId}", accountId);
                return Json(new { success = false, message = "Error retrieving account balance", balance = 0 });
            }
        }

        // POST: VendorBillsController/GenerateBill
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
                        
                        // Return JSON response for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = "Please select a vendor." });
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Please select a vendor.";
                            await ReloadViewDataAsync();
                            return View(model);
                        }
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
                    
                    // Validate Pay Now based on Payment Method
                    if (model.PaymentMethod != "PayLater" && model.PayNow <= 0)
                    {
                        _logger.LogWarning("PayNow validation failed - PayNow is {PayNow} for PaymentMethod {PaymentMethod}", model.PayNow, model.PaymentMethod);
                        TempData["ErrorMessage"] = "Pay Now amount is required when payment method is not 'Pay Later'.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }
                    
                    // Validate Online Account selection for Online payment method
                    if (model.PaymentMethod == "Online" && (!model.OnlineAccountId.HasValue || model.OnlineAccountId == 0))
                    {
                        _logger.LogWarning("Online account validation failed - OnlineAccountId is {OnlineAccountId} for PaymentMethod {PaymentMethod}", model.OnlineAccountId, model.PaymentMethod);
                        TempData["ErrorMessage"] = "Please select an online account for online payment.";
                        await ReloadViewDataAsync();
                        return View(model);
                    }
                    
                    // Validate account balance for Online payment method
                    if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue)
                    {
                        var accountBalance = await _vendorBillsService.GetAccountBalanceAsync(model.OnlineAccountId.Value);
                        
                        if (accountBalance <= 0)
                        {
                            _logger.LogWarning("Account balance validation failed - Balance is {Balance} for AccountId {AccountId}", accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Account balance is insufficient. Available balance: ${accountBalance:F2}";
                            await ReloadViewDataAsync();
                            return View(model);
                        }
                        
                        if (model.PayNow > accountBalance)
                        {
                            _logger.LogWarning("Payment amount validation failed - PayNow {PayNow} exceeds balance {Balance} for AccountId {AccountId}", model.PayNow, accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Payment amount exceeds available account balance. Available balance: ${accountBalance:F2}, Payment amount: ${model.PayNow:F2}";
                            await ReloadViewDataAsync();
                            return View(model);
                        }
                    }
                    
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    DateTime currentDateTime = DateTimeHelper.Now;
                    long billId;

                    // Create new bill
                    _logger.LogInformation("Creating new vendor bill");
                    
                    billId = await _vendorService.CreateVendorBillAsync(
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
                        
                            long billDetailsId = await _vendorService.AddVendorBillDetails(
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
                            var prodMaster = await _vendorService.GetStockByProductIdAsync(detail.ProductId);
                            if (prodMaster != null)
                            {
                                // Update stock quantity (INCREASE available quantity)
                                long updateStockReturn = _vendorService.UpdateStock(
                                    prodMaster.StockMasterId,
                                    detail.ProductId,
                                    prodMaster.AvailableQuantity + (decimal)detail.Quantity, // INCREASE stock
                                    prodMaster.TotalQuantity + (decimal)detail.Quantity, // INCREASE total quantity
                                    prodMaster.UsedQuantity, // Keep used quantity same
                                    userId,
                                    currentDateTime
                                );

                                // Create bill transaction
                                long transactionReturn = _vendorService.VendorBillTransactionCreate(
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
                                var transactionId = await _vendorService.ProcessOnlinePaymentTransactionAsync(
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

        // GET: VendorBillsController/PrintBill/5
        public async Task<ActionResult> PrintBill(long id)
        {
            try
            {
                var billPrint = await _vendorService.GetVendorBillForPrintAsync(id);
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
            var products = await _vendorBillsService.GetAllProductsAsync();
            ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

            // Load vendors
            var vendors = await _vendorBillsService.GetAllVendorsAsync();
            ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName");

            // Get next bill number
            var nextBillNumber = await _vendorBillsService.GetNextBillNumberAsync(0);
            ViewBag.NextBillNumber = nextBillNumber;
        }

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
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", vendorBill.VendorId);

                // Create VendorBillGenerationViewModel with existing data
                var viewModel = new VendorBillGenerationViewModel
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
                    PaymentMethod = vendorBill.PaymentMethod,
                    OnlineAccountId = vendorBill.OnlineAccountId,
                    BillDetails = billItems.Select(item => new VendorBillDetailViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductSize = item.ProductSize,
                        UnitPrice = item.UnitPrice,
                        PurchasePrice = item.BillPrice,
                        Quantity = (long)item.Quantity,
                        SalePrice = item.UnitPrice,
                        LineDiscountAmount = item.DiscountAmount,
                        PayableAmount = item.PayableAmount,
                        ProductRangeId = item.ProductRangeId,
                        ProductCode = item.ProductCode
                    }).ToList()
                };

                ViewBag.IsEdit = true;
                return View("GenerateBill", viewModel);
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
        public async Task<ActionResult> Edit(long id, VendorBillGenerationViewModel model)
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
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                var vendorBill = await _vendorBillsService.GetVendorBillByIdAsync(id);
                if (vendorBill == null)
                {
                    return NotFound();
                }
                
                return View(vendorBill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bill for deletion with ID: {BillId}", id);
                TempData["ErrorMessage"] = "Error loading bill for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorBillsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(long id, IFormCollection collection)
        {
            try
            {
                _logger.LogInformation("Attempting to delete vendor bill with ID: {BillId}", id);
                
                // Call service to delete the bill
                var result = await _vendorBillsService.DeleteVendorBillAsync(id);
                
                if (result)
                {
                    _logger.LogInformation("Vendor bill deleted successfully with ID: {BillId}", id);
                    TempData["SuccessMessage"] = "Vendor bill deleted successfully.";
                }
                else
                {
                    _logger.LogWarning("Failed to delete vendor bill with ID: {BillId}", id);
                    TempData["ErrorMessage"] = "Failed to delete vendor bill.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor bill with ID: {BillId}", id);
                TempData["ErrorMessage"] = "Error deleting vendor bill.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VendorBillsController/PrintReceipt/5
        public async Task<ActionResult> PrintReceipt(long id, bool merchantCopy = false, bool autoPrint = false)
        {
            try
            {
                _logger.LogInformation("PrintReceipt called with BillId: {BillId}, MerchantCopy: {MerchantCopy}, AutoPrint: {AutoPrint}", id, merchantCopy, autoPrint);
                
                var vendorBill = await _vendorBillsService.GetVendorBillByIdAsync(id);
                if (vendorBill == null)
                {
                    _logger.LogWarning("VendorBill not found for BillId: {BillId}", id);
                    return NotFound();
                }
                
                var billItems = await _vendorBillsService.GetVendorBillItemsAsync(id);
                _logger.LogInformation("Found {ItemCount} bill items for BillId: {BillId}", billItems?.Count ?? 0, id);
                
                var printModel = new
                {
                    Bill = vendorBill,
                    BillItems = billItems
                };
                
                ViewBag.MerchantCopy = merchantCopy;
                ViewBag.AutoPrint = autoPrint;
                _logger.LogInformation("Returning PrintReceipt view for BillId: {BillId}, MerchantCopy: {MerchantCopy}, AutoPrint: {AutoPrint}", id, merchantCopy, autoPrint);
                return View(printModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading print receipt for bill {BillId}", id);
                TempData["ErrorMessage"] = "Error loading receipt for printing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX endpoint to get all bill numbers for a specific vendor
        [HttpGet]
        public async Task<JsonResult> GetAllBillNumbersForVendor(long vendorId)
        {
            try
            {
                _logger.LogInformation("Getting bill numbers for vendor: {VendorId}", vendorId);
                
                var billNumbers = await _vendorBillsService.GetAllBillNumbersForVendorAsync(vendorId);
                
                var result = billNumbers.Select(bill => new
                {
                    value = bill.BillNumber.ToString(),
                    text = bill.BillNumber.ToString()
                }).ToList();

                _logger.LogInformation("Retrieved {Count} bill numbers for vendor {VendorId}", result.Count, vendorId);
                
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill numbers for vendor {VendorId}", vendorId);
                return Json(new { success = false, message = "Error retrieving bill numbers", data = new List<object>() });
            }
        }

        // AJAX endpoint to get active bill numbers for a supplier using GetAllVendorActiveBillNumbers stored procedure
        [HttpGet]
        public async Task<IActionResult> GetBillNumbers(long supplierId)
        {
            try
            {
                _logger.LogInformation("Getting active bill numbers for supplier: {SupplierId}", supplierId);
                
                var billNumbers = await _vendorBillsService.GetActiveBillNumbersAsync(supplierId);
                
                _logger.LogInformation("Retrieved {Count} active bill numbers for supplier {SupplierId}", billNumbers.Count, supplierId);
                
                return Json(billNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active bill numbers for supplier {SupplierId}", supplierId);
                return Json(new List<SupplierBillNumber>());
            }
        }
    }
}