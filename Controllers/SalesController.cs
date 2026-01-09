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
        private readonly IPersonalPaymentService _personalPaymentService;

        public SalesController(ISalesService salesService, ILogger<SalesController> logger, IProductService productService, ICustomer customerService, IPersonalPaymentService personalPaymentService)
        {
            _salesService = salesService;
            _logger = logger;
            _productService = productService;
            _customerService = customerService;
            _personalPaymentService = personalPaymentService;
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
                    var emptyViewModel = new SalesViewModel();
                    // Load customers for the filter dropdown even when no sales exist
                    var customersRes = await _salesService.GetAllCustomersAsync();
                    emptyViewModel.CustomerList = customersRes;
                    return View(emptyViewModel);
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
                if (!string.IsNullOrEmpty(description))
                {
                    stockFilters.Description = description;
                }
                
                // Handle date filters - use Pakistan Standard Time
                if (saleFrom == null && saleDateTo == null)
                {
                    // Use Pakistan Standard Time for default dates (today)
                    stockFilters.SaleFrom = DateTimeHelper.Today;
                    stockFilters.SaleDateTo = DateTimeHelper.Today;
                }
                else
                {
                    // Convert provided dates to Pakistan time
                    // Date inputs provide date-only values, so we treat them as Pakistan dates
                    if (saleFrom.HasValue)
                    {
                        // Ensure the date is treated as Pakistan time (start of day)
                        var pakistanFromDate = saleFrom.Value.Date;
                        stockFilters.SaleFrom = pakistanFromDate;
                    }
                    if (saleDateTo.HasValue)
                    {
                        // Set to end of day in Pakistan time
                        var pakistanToDate = saleDateTo.Value.Date.AddDays(1).AddTicks(-1);
                        stockFilters.SaleDateTo = pakistanToDate;
                    }
                }
                var viewModel = await _salesService.GetAllSalesAsync(pageNumber, currentPageSize, stockFilters);
                
                // Load customers for the filter dropdown
                var customers = await _salesService.GetAllCustomersAsync();
                viewModel.CustomerList = customers;
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                var errorViewModel = new SalesViewModel();
                try
                {
                    // Load customers for the filter dropdown even when there's an error
                    var customers = await _salesService.GetAllCustomersAsync();
                    errorViewModel.CustomerList = customers;
                }
                catch (Exception customerEx)
                {
                    _logger.LogError(customerEx, "Error loading customers for filter dropdown");
                }
                return View(errorViewModel);
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
                    sale.CreatedDate = DateTimeHelper.Now;
                    sale.ModifiedBy = userId;
                    sale.ModifiedDate = DateTimeHelper.Now;

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
                    sale.ModifiedDate = DateTimeHelper.Now;
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
                var modifiedDate = DateTimeHelper.Now;
                
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


                return View(new AddSaleViewModel { SaleDate = DateTimeHelper.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Add Sale page");
                TempData["ErrorMessage"] = "Error loading the Add Sale page. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditSale(long id)
        {
            try
            {
                // Get the sale with details
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }

                // Get sale details
                var saleDetails = await _salesService.GetSaleDetailsBySaleIdAsync(id);
                
                // Load products
                var products = await _productService.GetAllEnabledProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");

                // Load customers
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", sale.CustomerIdFk);

                // Create AddSaleViewModel with existing data
                var viewModel = new AddSaleViewModel
                {
                    SaleDetails = saleDetails,
                    TotalAmount = sale.TotalAmount,
                    TotalReceivedAmount = sale.TotalReceivedAmount,
                    TotalDueAmount = sale.TotalDueAmount,
                    CustomerId = sale.CustomerIdFk,
                    BillNo = sale.BillNumber.ToString(),
                    SaleDate = sale.SaleDate,
                    DiscountAmount = sale.DiscountAmount,
                    ReceivedAmount = sale.TotalReceivedAmount,
                    DueAmount = sale.TotalDueAmount,
                    Description = sale.SaleDescription,
                    SaleId = sale.SaleId, // Add this to track the sale being edited
                    PaymentMethod = "Cash", // Default to Cash since we don't store this in the database
                    OnlineAccountId = null // Default to null since we don't store this in the database
                };

                // Get previous due amount for the customer
                if (sale.CustomerIdFk > 0)
                {
                    var previousDueAmount = await _salesService.GetPreviousDueAmountByCustomerIdAsync(sale.CustomerIdFk);
                    viewModel.PreviousDue = previousDueAmount;
                }

                ViewBag.IsEdit = true;
                ViewBag.SaleId = sale.SaleId;

                return View("AddSale", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit Sale page for ID: {SaleId}", id);
                TempData["ErrorMessage"] = "Error loading the sale for editing. Please try again.";
                return RedirectToAction("Index");
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

        [HttpGet]
        public async Task<JsonResult> GetAvailableStock(long productId)
        {
            try
            {
                var stock = await _salesService.GetStockByProductIdAsync(productId);
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

        [HttpGet]
        public async Task<JsonResult> GetOnlineAccounts()
        {
            try
            {
                var onlineAccounts = await _personalPaymentService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                var accountOptions = onlineAccounts.PersonalPaymentList.Select(account => new
                {
                    value = account.PersonalPaymentId.ToString(),
                    text = $"{account.BankName} - {account.AccountHolderName}",
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
                    
                    DateTime currentDateTime = DateTimeHelper.Now;
                    long saleId;

                    // Check if we're editing an existing sale
                    if (model.SaleId.HasValue && model.SaleId > 0)
                    {
                        _logger.LogInformation("Updating existing sale with ID: {SaleId}", model.SaleId);
                        
                        // Update existing sale
                        var updateResult = await _salesService.UpdateSaleAsync(new Sale
                        {
                            SaleId = model.SaleId.Value,
                            TotalAmount = model.TotalAmount,
                            TotalReceivedAmount = model.ReceivedAmount,
                            TotalDueAmount = model.DueAmount,
                            CustomerIdFk = model.CustomerId.Value,
                            DiscountAmount = model.DiscountAmount,
                            BillNumber = long.Parse(model.BillNo ?? "0"),
                            SaleDescription = model.Description ?? "Add Sale",
                            SaleDate = model.SaleDate,
                            ModifiedBy = userId,
                            ModifiedDate = currentDateTime
                        });

                        if (updateResult == 0)
                        {
                            TempData["ErrorMessage"] = "Failed to update sale.";
                            await ReloadViewDataAsync();
                            return View(model);
                        }
                        //Update Stock
                        await _salesService.TransactionDeleteAndStockUpdate(model.SaleId.Value);
                        
                        // Delete existing sale details
                        await _salesService.DeleteSaleDetailsBySaleIdAsync(model.SaleId.Value);
                        
                        saleId = model.SaleId.Value;
                    }
                    else
                    {
                        _logger.LogInformation("Creating new sale");
                        
                        // Create new sale
                        saleId = await _salesService.CreateSaleAsync(
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
                            model.SaleDate,
                            model.PaymentMethod,
                            model.OnlineAccountId
                        );
                    }

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
                                    prodMaster.UsedQuantity + (decimal)detail.Quantity,
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

                        // Process online payment transaction if payment method is Online
                        if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                        {
                            try
                            {
                                var transactionDescription = $"Sale Credit - Bill #{model.BillNo} - {model.Description}";
                                var transactionId = await _salesService.ProcessOnlinePaymentTransactionAsync(
                                    model.OnlineAccountId.Value,
                                    saleId,
                                    model.ReceivedAmount, // Credit the received amount to the online account
                                    transactionDescription,
                                    userId,
                                    currentDateTime
                                );
                                
                                _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Sale ID: {SaleId}", 
                                    transactionId, saleId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing online payment transaction for Sale ID: {SaleId}", saleId);
                                // Don't fail the entire sale if online payment processing fails
                                // Just log the error and continue
                            }
                        }

                        // Return JSON response for AJAX calls
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                            Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                        {
                            // AJAX request - return JSON
                            string successMessage = model.SaleId.HasValue ? "Sale updated successfully!" : "Sale created successfully!";
                            if (model.ActionType == "saveAndPrint")
                            {
                                return Json(new { success = true, message = successMessage, saleId = saleId, print = true });
                            }
                            else
                            {
                                return Json(new { success = true, message = successMessage, saleId = saleId });
                            }
                        }
                        else
                        {
                            // Regular form submission - use TempData and redirect
                            string successMessage = model.SaleId.HasValue ? "Sale updated successfully!" : "Sale created successfully!";
                            TempData["Success"] = successMessage;
                            
                            if (model.ActionType == "saveAndPrint")
                            {
                                return RedirectToAction("Details", new { id = saleId, print = true });
                            }
                            else
                            {
                                return RedirectToAction("Index");
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

        // AJAX endpoint to get products for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _productService.GetAllEnabledProductsAsync();
                var result = products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
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

        // AJAX endpoint to get customers for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _customerService.GetAllEnabledCustomers();
                var result = customers?.Select(c => new
                {
                    value = c.CustomerId.ToString(),
                    text = c.CustomerName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // GET: SalesController/PrintReceipt/5
        public async Task<ActionResult> PrintReceipt(long id, bool merchantCopy = false)
        {
            try
            {
                var salePrint = await _salesService.GetSaleForPrintAsync(id);
                if (salePrint == null)
                {
                    return NotFound();
                }
                
                ViewBag.MerchantCopy = merchantCopy;
                return View(salePrint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading print receipt for sale {SaleId}", id);
                TempData["ErrorMessage"] = "Error loading receipt for printing.";
                return RedirectToAction(nameof(Index));
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

                // Use the existing method from SalesService
                var productSizes = await _salesService.GetProductUnitPriceRangeByProductIdAsync(productId.Value);
                _logger.LogInformation("Retrieved {Count} product sizes from database", productSizes.Count);

                // Log the raw data for debugging
                foreach (var ps in productSizes)
                {
                    _logger.LogInformation("ProductSize - ID: {ProductRangeId}, RangeFrom: {RangeFrom}, RangeTo: {RangeTo}, UnitPrice: {UnitPrice}, MeasuringUnit: {MeasuringUnitName}", 
                        ps.ProductRangeId, ps.RangeFrom, ps.RangeTo, ps.UnitPrice, ps.MeasuringUnitName);
                }

                // Log all records before filtering for debugging
                _logger.LogInformation("=== DEBUGGING: All product sizes before filtering ===");
                foreach (var ps in productSizes)
                {
                    _logger.LogInformation("BEFORE FILTER - ID: {ProductRangeId}, RangeFrom: {RangeFrom}, RangeTo: {RangeTo}, UnitPrice: {UnitPrice}, MeasuringUnit: {MeasuringUnitName}", 
                        ps.ProductRangeId, ps.RangeFrom, ps.RangeTo, ps.UnitPrice, ps.MeasuringUnitName);
                }

                // TEMPORARILY DISABLE FILTERING FOR DEBUGGING
                // Filter out entries with zero ranges and create result
                // var validProductSizes = productSizes.Where(ps => ps.RangeFrom > 0 || ps.RangeTo > 0).ToList();
                var validProductSizes = productSizes.ToList(); // Use all records for debugging
                _logger.LogInformation("Filtered to {Count} valid product sizes", validProductSizes.Count);
                
                // Log valid records after filtering
                _logger.LogInformation("=== DEBUGGING: Valid product sizes after filtering ===");
                foreach (var ps in validProductSizes)
                {
                    _logger.LogInformation("AFTER FILTER - ID: {ProductRangeId}, RangeFrom: {RangeFrom}, RangeTo: {RangeTo}, UnitPrice: {UnitPrice}, MeasuringUnit: {MeasuringUnitName}", 
                        ps.ProductRangeId, ps.RangeFrom, ps.RangeTo, ps.UnitPrice, ps.MeasuringUnitName);
                }

                var result = validProductSizes.Select(ps => new
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
    }
}
