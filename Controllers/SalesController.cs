using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using System.Linq;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly ILogger<SalesController> _logger;
        private readonly IProductService _productService;
        private readonly ICustomer _customerService;
        private readonly IPersonalPaymentService _personalPaymentService;
        private readonly IAdminMeasuringUnitService _measuringUnitService;
        private readonly IVendor _vendorService;
        private readonly IVendorBillsService _vendorBillsService;
        private const string VendorCustomerPrefix = "Vendor - ";

        public SalesController(ISalesService salesService, ILogger<SalesController> logger, IProductService productService, ICustomer customerService, IPersonalPaymentService personalPaymentService, IAdminMeasuringUnitService measuringUnitService, IVendor vendorService, IVendorBillsService vendorBillsService)
        {
            _salesService = salesService;
            _logger = logger;
            _productService = productService;
            _customerService = customerService;
            _personalPaymentService = personalPaymentService;
            _measuringUnitService = measuringUnitService;
            _vendorService = vendorService;
            _vendorBillsService = vendorBillsService;
        }

        // GET: SalesController
        public async Task<ActionResult> Index(SalesViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new SalesViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new SalesFilters();
                }

                var currentPageSize = pageSize ?? HttpContext.Session.GetInt32("PageSize") ?? 10;
                HttpContext.Session.SetInt32("PageSize", currentPageSize);

                // Check if there are any sales records at all
                var hasAnySales = await _salesService.HasAnySalesAsync();
                if (!hasAnySales)
                {
                    TempData["InfoMessage"] = "No sales records found in the database. You can create your first sale using the 'Add New Sale' button.";
                    // Load customers for the filter dropdown even when no sales exist
                    var customersRes = await _salesService.GetAllCustomersAsync();
                    model.CustomerList = customersRes;
                    return View(model);
                }

                // Preserve filters before service call
                var stockFilters = model.Filters;

                // Get filtered data using model.Filters
                model = await _salesService.GetAllSalesAsync(pageNumber, currentPageSize, stockFilters);
                
                // Reassign filters to ensure they're preserved
                model.Filters = stockFilters;
                
                // Load customers for the filter dropdown
                var customers = await _salesService.GetAllCustomersAsync();
                model.CustomerList = customers;
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new SalesViewModel();
                }
                try
                {
                    // Load customers for the filter dropdown even when there's an error
                    var customers = await _salesService.GetAllCustomersAsync();
                    model.CustomerList = customers;
                }
                catch (Exception customerEx)
                {
                    _logger.LogError(customerEx, "Error loading customers for filter dropdown");
                }
                return View(model);
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

                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "Sale not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Handle online payment reversal and payments the same way as Edit Sale
                var isOnline = !string.IsNullOrEmpty(sale.PaymentMethod)
                    && string.Equals(sale.PaymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                    && sale.TotalReceivedAmount > 0;

                if (isOnline)
                {
                    try
                    {
                        await _salesService.ReverseOnlinePaymentTransactionBySaleIdAsync(id, userId);
                        _logger.LogInformation("Online payment reversed for deleted Sale ID: {SaleId}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reversing online payment for deleted Sale ID: {SaleId}", id);
                        TempData["WarningMessage"] = "Sale deletion initiated but reversing the online transaction failed. Please verify account balances.";
                    }
                }

                // Delete payments against this sale (as in Edit Sale)
                await _salesService.DeletePaymentBySaleIdAsync(id);
                // Delete stock transaction records for this sale (as in Edit Sale)
                await _salesService.DeleteStockTransactionBySaleIdAsync(id);

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
                _logger.LogError(ex, "Error deleting sale ID: {SaleId}", id);
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
        public async Task<ActionResult> EditSale(long id, bool IsEditMode = true)
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
                    VendorId = sale.VendorId,
                    BillNo = sale.BillNumber.ToString(),
                    SaleDate = sale.SaleDate,
                    DiscountAmount = sale.DiscountAmount,
                    ReceivedAmount = sale.TotalReceivedAmount,
                    PayNow = sale.TotalReceivedAmount, // Show received amount in Pay Now when editing
                    DueAmount = sale.TotalDueAmount,
                    Description = sale.SaleDescription,
                    SaleId = sale.SaleId, // Add this to track the sale being edited
                    PaymentMethod = sale.PaymentMethod ?? "Cash", // Use stored payment method or default to Cash
                    OnlineAccountId = sale.PersonalPaymentId ?? sale.OnlineAccountId, // Use PersonalPaymentId if available, otherwise OnlineAccountId
                    IsEditMode = IsEditMode // Pass IsEditMode to the model
                };

                // If this sale was created for a vendor, map back to vendor selection
                bool isVendorCustomer = false;
                if (sale.CustomerIdFk > 0)
                {
                    var customerDetails = await _customerService.GetCustomerIdAsync(sale.CustomerIdFk.Value);
                    if (customerDetails != null &&
                        !string.IsNullOrWhiteSpace(customerDetails.CustomerName) &&
                        customerDetails.CustomerName.StartsWith(VendorCustomerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var vendorName = customerDetails.CustomerName.Substring(VendorCustomerPrefix.Length).Trim();
                        var vendors = await _vendorService.GetAllEnabledVendors();
                        var matchedVendor = vendors.FirstOrDefault(v =>
                            v.SupplierName.Equals(vendorName, StringComparison.OrdinalIgnoreCase));

                        if (matchedVendor != null)
                        {
                            viewModel.VendorId = matchedVendor.SupplierId;
                            viewModel.CustomerId = null;
                            viewModel.PreviousDue = await _vendorBillsService.GetPreviousDueAmountAsync(matchedVendor.SupplierId);
                            ViewBag.SelectedVendorName = matchedVendor.SupplierName;
                            isVendorCustomer = true;
                        }
                    }
                }

                // Get previous due amount for the customer
                if (!isVendorCustomer && sale.CustomerIdFk > 0)
                {
                    var previousDueAmount = await _salesService.GetPreviousDueAmountByCustomerIdAsync(sale.CustomerIdFk.Value);
                    viewModel.PreviousDue = previousDueAmount;
                }

                ViewBag.IsEdit = true;
                ViewBag.IsEditMode = IsEditMode;
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

        /// <summary>
        /// POST: SalesController/EditSale/5 - Save changes for an existing sale. Separate from AddSale so edit-specific logic (e.g. stock revert) does not affect Add Sale.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditSale(long id, AddSaleViewModel model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid form data.";
                return RedirectToAction(nameof(EditSale), new { id });
            }

            model.SaleId = id;

            // Remove SaleDetails/Description/PayNow validation errors; we validate manually
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("SaleDetails") || k.StartsWith("Description") || k.StartsWith("PayNow")).ToList();
            foreach (var key in keysToRemove)
                ModelState.Remove(key);

            var hasCustomer = model.CustomerId.HasValue && model.CustomerId > 0;
            var hasVendor = model.VendorId.HasValue && model.VendorId > 0;
            if (!hasCustomer && !hasVendor)
            {
                TempData["ErrorMessage"] = "Please select a customer or vendor. One of them is mandatory.";
                await ReloadEditSaleViewDataAsync(id, model);
                return View("AddSale", model);
            }
            if (hasCustomer && hasVendor)
            {
                TempData["ErrorMessage"] = "Please select either a customer or a vendor, not both.";
                await ReloadEditSaleViewDataAsync(id, model);
                return View("AddSale", model);
            }

            if (model.SaleDetails == null || !model.SaleDetails.Any())
            {
                TempData["ErrorMessage"] = "Please add at least one product to the sale.";
                await ReloadEditSaleViewDataAsync(id, model);
                return View("AddSale", model);
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check the form data and try again.";
                await ReloadEditSaleViewDataAsync(id, model);
                return View("AddSale", model);
            }

            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr);
                DateTime currentDateTime = DateTimeHelper.Now;

                // Revert stock for existing sale (restore quantities from deleted details)
                await _salesService.TransactionDeleteAndStockUpdate(id);
                await _salesService.DeleteSaleDetailsBySaleIdAsync(id);
                await _salesService.DeletePaymentBySaleIdAsync(id);
                await _salesService.DeleteStockTransactionBySaleIdAsync(id);
                await _salesService.ReverseOnlinePaymentTransactionBySaleIdAsync(id, userId);

                var updateResult = await _salesService.UpdateSaleAsync(new Sale
                {
                    SaleId = id,
                    TotalAmount = model.TotalAmount,
                    TotalReceivedAmount = model.ReceivedAmount,
                    TotalDueAmount = model.DueAmount,
                    CustomerIdFk = model.CustomerId ?? 0,
                    SupplierIdFk = model.VendorId ?? 0,
                    DiscountAmount = model.DiscountAmount,
                    BillNumber = long.Parse(model.BillNo ?? "0"),
                    SaleDescription = model.Description ?? "Update Sales Return",
                    SaleDate = model.SaleDate,
                    ModifiedBy = userId,
                    ModifiedDate = currentDateTime,
                    PaymentMethod = model.PaymentMethod,
                    OnlineAccountId = model.PaymentMethod == "Online" ? model.OnlineAccountId : null,
                });

                if (updateResult == 0)
                {
                    TempData["ErrorMessage"] = "Failed to update sale.";
                    await ReloadEditSaleViewDataAsync(id, model);
                    return View("AddSale", model);
                }

                foreach (var detail in model.SaleDetails)
                {
                    int detailReturnValue;
                    _salesService.AddSaleDetails(
                        id,
                        detail.ProductId,
                        detail.UnitPrice,
                        detail.Quantity,
                        detail.SalePrice,
                        detail.LineDiscountAmount,
                        detail.PayableAmount,
                        detail.ProductRangeId,
                        currentDateTime,
                        userId,
                        model.PaymentMethod,
                        model.OnlineAccountId,
                        out detailReturnValue
                    );

                    var prodMaster = await _salesService.GetStockByProductIdAsync(detail.ProductId);
                    if (prodMaster != null)
                    {
                        decimal quantityToDeduct = (decimal)detail.Quantity;
                        var productRanges = await _salesService.GetProductUnitPriceRangeByProductIdAsync(detail.ProductId);
                        var selectedProductRange = productRanges?.FirstOrDefault(pr => pr.ProductRangeId == detail.ProductRangeId);

                        if (selectedProductRange != null)
                        {
                            var product = await _productService.GetProductByIdAsync(detail.ProductId);
                            if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                            {
                                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                                var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                                long? baseUnitId = smallestUnit?.MeasuringUnitId ?? (measuringUnits.Any() ? measuringUnits.First().MeasuringUnitId : (long?)null);

                                if (baseUnitId.HasValue && selectedProductRange.MeasuringUnitId_FK != baseUnitId.Value)
                                {
                                    var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                                    var convertedQuantity = await unitConversionService.ConvertUnitAsync(
                                        selectedProductRange.MeasuringUnitId_FK,
                                        baseUnitId.Value,
                                        (decimal)detail.Quantity
                                    );
                                    if (convertedQuantity.HasValue)
                                        quantityToDeduct = convertedQuantity.Value;
                                }
                            }
                        }

                        _salesService.UpdateStock(
                            prodMaster.StockMasterId,
                            detail.ProductId,
                            prodMaster.AvailableQuantity - quantityToDeduct,
                            prodMaster.TotalQuantity,
                            prodMaster.UsedQuantity + quantityToDeduct,
                            userId,
                            currentDateTime
                        );

                        _salesService.SaleTransactionCreate(
                            prodMaster.StockMasterId,
                            (decimal)detail.Quantity,
                            $"Sale #{id}",
                            currentDateTime,
                            userId,
                            2,
                            id
                        );
                    }
                }

                if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                {
                    try
                    {
                        var transactionDescription = $"Sale Credit - Bill #{model.BillNo} - {model.Description}";
                        await _salesService.ProcessOnlinePaymentTransactionAsync(
                            model.OnlineAccountId.Value,
                            id,
                            model.ReceivedAmount,
                            transactionDescription,
                            userId,
                            currentDateTime
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing online payment transaction for Edit Sale ID: {SaleId}", id);
                    }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    if (model.ActionType == "saveAndPrint")
                        return Json(new { success = true, message = "Sale updated successfully!", saleId = id, print = true });
                    return Json(new { success = true, message = "Sale updated successfully!", saleId = id });
                }

                TempData["Success"] = "Sale updated successfully!";
                if (model.ActionType == "saveAndPrint")
                    return RedirectToAction("Details", new { id, print = true });
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale ID: {SaleId}", id);
                TempData["ErrorMessage"] = ex.Message;
                await ReloadEditSaleViewDataAsync(id, model);
                return View("AddSale", model);
            }
        }

        private async Task ReloadEditSaleViewDataAsync(long saleId, AddSaleViewModel model)
        {
            var products = await _productService.GetAllEnabledProductsAsync();
            ViewBag.Products = new SelectList(products, "ProductId", "ProductName");
            var customers = await _salesService.GetAllCustomersAsync();
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
            ViewBag.IsEdit = true;
            ViewBag.IsEditMode = true;
            ViewBag.SaleId = saleId;
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
        public async Task<JsonResult> GetVendorPreviousDueAmount(long vendorId)
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

        [HttpPost]
        [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
        public async Task<JsonResult> SaveCustomerOpenBalance([FromBody] CustomerOpenBalanceRequest request)
        {
            try
            {
                if (request == null || request.CustomerId <= 0)
                    return Json(new { success = false, message = "Invalid customer." });
                //if (string.IsNullOrWhiteSpace(request.Type) || (request.Type != "Payable" && request.Type != "Receivable"))
                //    return Json(new { success = false, message = "Type must be Payable or Receivable." });
                if (request.OpeningBalance < 0)
                    return Json(new { success = false, message = "Opening balance cannot be negative." });
                if (request.OpeningBalance == 0)
                    return Json(new { success = false, message = "Opening balance cannot be empty." });

                var userIdStr = HttpContext.Session.GetString("UserId");
                long createdBy = long.TryParse(userIdStr, out var uid) ? uid : 1;

                DateTime? balanceDate = null;
                if (!string.IsNullOrWhiteSpace(request.BalanceDate) && DateTime.TryParse(request.BalanceDate, out var parsedDate))
                    balanceDate = parsedDate;

                long saleId = await _salesService.AddOpeningBalanceSaleAsync(
                    request.CustomerId,
                    request.Type,
                    request.OpeningBalance,
                    createdBy,
                    balanceDate);
                return Json(new { success = true, message = "Opening balance saved.", saleId = saleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customer opening balance");
                return Json(new { success = false, message = ex.Message });
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
                    // Get product to find base unit
                    var product = await _productService.GetProductByIdAsync(productId);
                    long? baseUnitId = null;
                    
                    if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                    {
                        // Get the smallest unit for this measuring unit type as base unit
                        var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                        var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                        if (smallestUnit != null)
                        {
                            baseUnitId = smallestUnit.MeasuringUnitId;
                        }
                        else if (measuringUnits.Any())
                        {
                            // If no smallest unit is marked, use the first enabled unit as base unit
                            baseUnitId = measuringUnits.First().MeasuringUnitId;
                            _logger.LogWarning("No smallest unit marked for product {ProductId}, using first unit {UnitId} as base unit", productId, baseUnitId);
                        }
                    }
                    
                    return Json(new { 
                        success = true, 
                        availableQuantity = stock.AvailableQuantity,
                        baseUnitId = baseUnitId
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Stock information not found", availableQuantity = 0, baseUnitId = (long?)null });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available stock for product {ProductId}", productId);
                return Json(new { success = false, message = "Error retrieving stock information", availableQuantity = 0, baseUnitId = (long?)null });
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
                    
                var hasCustomer = model.CustomerId.HasValue && model.CustomerId > 0;
                var hasVendor = model.VendorId.HasValue && model.VendorId > 0;

                if (!hasCustomer && !hasVendor)
                {
                    _logger.LogWarning("Customer/Vendor validation failed - both are null or 0");
                    TempData["ErrorMessage"] = "Please select a customer or vendor. One of them is mandatory.";
                    await ReloadViewDataAsync();
                    return View(model);
                }

                if (hasCustomer && hasVendor)
                {
                    _logger.LogWarning("Customer/Vendor validation failed - both provided");
                    TempData["ErrorMessage"] = "Please select either a customer or a vendor, not both.";
                    await ReloadViewDataAsync();
                    return View(model);
                }

                    //if (hasVendor)
                    //{
                    //    var vendorUserIdStr = HttpContext.Session.GetString("UserId");
                    //    long vendorUserId = long.Parse(vendorUserIdStr);
                    //    var resolvedCustomerId = await ResolveCustomerIdForVendorAsync(model.VendorId!.Value, vendorUserId);
                    //    if (!resolvedCustomerId.HasValue)
                    //    {
                    //        TempData["ErrorMessage"] = "Unable to map vendor to a customer record.";
                    //        await ReloadViewDataAsync();
                    //        return View(model);
                    //    }

                    //    model.CustomerId = resolvedCustomerId.Value;
                    //}

                    // Validate sale details
                    if (model.SaleDetails == null || !model.SaleDetails.Any())
                {
                        _logger.LogWarning("Sale details validation failed - SaleDetails is null or empty");
                        TempData["ErrorMessage"] = "Please add at least one product to the sale.";
                        await ReloadViewDataAsync();
                        return View(model);
                }

                    _logger.LogInformation("Sale details validation passed - {Count} items", model.SaleDetails.Count);

                    // Add Sale POST only handles new sales; edit submissions must use EditSale POST
                    if (model.SaleId.HasValue && model.SaleId > 0)
                    {
                        _logger.LogWarning("AddSale POST received SaleId {SaleId}; redirecting to Edit Sale.", model.SaleId);
                        return RedirectToAction(nameof(EditSale), new { id = model.SaleId.Value });
                    }

                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    DateTime currentDateTime = DateTimeHelper.Now;

                    _logger.LogInformation("Creating new sale");
                    long saleId = await _salesService.CreateSaleAsync(
                            model.TotalAmount,
                            model.ReceivedAmount,
                            model.DueAmount,
                            model.CustomerId==null ? 0 : model.CustomerId.Value,
                            model.VendorId == null ? 0 : model.VendorId.Value,
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
                                 currentDateTime,
                                   userId,
                                    model.PaymentMethod,
                               model.OnlineAccountId,
                                out detailReturnValue
                            );

                            // Get stock information and update
                            var prodMaster = await _salesService.GetStockByProductIdAsync(detail.ProductId);
                            if (prodMaster!=null)
                            {
                                // Calculate quantity in base unit (smallest unit) for stock update
                                decimal quantityToDeduct = (decimal)detail.Quantity;
                                
                                // Get ProductRange to find the MeasuringUnitId
                                var productRanges = await _salesService.GetProductUnitPriceRangeByProductIdAsync(detail.ProductId);
                                var selectedProductRange = productRanges?.FirstOrDefault(pr => pr.ProductRangeId == detail.ProductRangeId);
                                
                                if (selectedProductRange != null)
                                {
                                    // Get product to find base unit
                                    var product = await _productService.GetProductByIdAsync(detail.ProductId);
                                    if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                                    {
                                        // Get the smallest unit for this measuring unit type as base unit
                                        var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                                        var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                                        long? baseUnitId = null;
                                        
                                        if (smallestUnit != null)
                                        {
                                            baseUnitId = smallestUnit.MeasuringUnitId;
                                        }
                                        else if (measuringUnits.Any())
                                        {
                                            // If no smallest unit is marked, use the first enabled unit as base unit
                                            baseUnitId = measuringUnits.First().MeasuringUnitId;
                                            _logger.LogWarning("No smallest unit marked for product {ProductId}, using first unit {UnitId} as base unit", detail.ProductId, baseUnitId);
                                        }
                                        
                                        // If selected unit is different from base unit, convert quantity
                                        if (baseUnitId.HasValue && selectedProductRange.MeasuringUnitId_FK != baseUnitId.Value)
                                        {
                                            var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                                            var convertedQuantity = await unitConversionService.ConvertUnitAsync(
                                                selectedProductRange.MeasuringUnitId_FK, 
                                                baseUnitId.Value, 
                                                (decimal)detail.Quantity
                                            );
                                            
                                            if (convertedQuantity.HasValue)
                                            {
                                                quantityToDeduct = convertedQuantity.Value;
                                                _logger.LogInformation("Converted {Quantity} {FromUnitId} to {ConvertedQuantity} {ToUnitId} for product {ProductId}", 
                                                    detail.Quantity, selectedProductRange.MeasuringUnitId_FK, quantityToDeduct, baseUnitId.Value, detail.ProductId);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("No conversion found from unit {FromUnitId} to base unit {ToUnitId} for product {ProductId}, using original quantity", 
                                                    selectedProductRange.MeasuringUnitId_FK, baseUnitId.Value, detail.ProductId);
                                            }
                                        }
                                    }
                                }
                                
                                // Update stock quantity (deduct converted quantity in base unit)
                                long updateStockReturn = _salesService.UpdateStock(
                                    prodMaster.StockMasterId,
                                    detail.ProductId,
                                    prodMaster.AvailableQuantity - quantityToDeduct,
                                    prodMaster.TotalQuantity,
                                    prodMaster.UsedQuantity + quantityToDeduct,
                                    userId,
                                    currentDateTime
                                );

                                // Create sale transaction (store original quantity, not converted)
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
                var result = products.Select(p => new
                {
                    value = p.ProductId.ToString(),
                    text = p.ProductName,
                    productCode = p.ProductCode ?? string.Empty,
                    productName = p.ProductName
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

        // AJAX endpoint to get vendors for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                var vendors = await _vendorService.GetAllEnabledVendors();
                var result = vendors?.Select(v => new
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

        private async Task<long?> ResolveCustomerIdForVendorAsync(long vendorId, long userId)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
            if (vendor == null)
            {
                _logger.LogWarning("Vendor not found for vendorId {VendorId}", vendorId);
                return null;
            }

            var vendorCustomerName = $"{VendorCustomerPrefix}{vendor.SupplierName}";
            var customers = await _customerService.GetAllEnabledCustomers();
            var existingCustomer = customers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.CustomerName) &&
                c.CustomerName.Equals(vendorCustomerName, StringComparison.OrdinalIgnoreCase));

            if (existingCustomer != null)
            {
                return existingCustomer.CustomerId;
            }

            var newCustomer = new Customer
            {
                CustomerName = vendorCustomerName,
                CustomerContactNumber = vendor.SupplierPhoneNumber,
                CustomerEmail = vendor.SupplierEmail,
                CustomerAddress = vendor.SupplierAddress,
                IsEnabled = true,
                CreatedBy = userId,
                CreatedDate = DateTimeHelper.Now,
                ModifiedBy = userId,
                ModifiedDate = DateTimeHelper.Now
            };

            var created = await _customerService.CreateCustomerAsync(newCustomer);
            if (!created)
            {
                _logger.LogWarning("Failed to create customer record for vendor {VendorName}", vendor.SupplierName);
                return null;
            }

            customers = await _customerService.GetAllEnabledCustomers();
            existingCustomer = customers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.CustomerName) &&
                c.CustomerName.Equals(vendorCustomerName, StringComparison.OrdinalIgnoreCase));

            return existingCustomer?.CustomerId;
        }

        /// <summary>
        /// Export sales to Excel (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(long? customerId = null, long? billNumber = null, DateTime? saleFrom = null, DateTime? saleDateTo = null, string? description = null)
        {
            try
            {
                var filters = new SalesFilters
                {
                    CustomerId = customerId,
                    BillNumber = billNumber,
                    SaleFrom = saleFrom,
                    SaleDateTo = saleDateTo,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description
                };
                const int exportPageSize = 100000;
                var model = await _salesService.GetAllSalesAsync(1, exportPageSize, filters);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sales");

                worksheet.Cell(1, 1).Value = "Sale Id";
                worksheet.Cell(1, 2).Value = "Bill #";
                worksheet.Cell(1, 3).Value = "Sales Date";
                worksheet.Cell(1, 4).Value = "Customer";
                worksheet.Cell(1, 5).Value = "Supplier";
                worksheet.Cell(1, 6).Value = "Total Amount";
                worksheet.Cell(1, 7).Value = "Discount";
                worksheet.Cell(1, 8).Value = "Received";
                worksheet.Cell(1, 9).Value = "Total Dues";
                worksheet.Cell(1, 10).Value = "Description";
                worksheet.Cell(1, 11).Value = "Payment Method";
                worksheet.Cell(1, 12).Value = "Status";

                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                decimal totalAmount = 0, totalDiscount = 0, totalReceived = 0, totalDue = 0;
                foreach (var item in model.SalesList ?? new List<SaleWithCustomerViewModel>())
                {
                    worksheet.Cell(row, 1).Value = item.SaleId;
                    worksheet.Cell(row, 2).Value = item.BillNumber;
                    worksheet.Cell(row, 3).Value = item.SaleDate.ToString("dd-MMM-yyyy");
                    worksheet.Cell(row, 4).Value = item.CustomerName ?? "";
                    worksheet.Cell(row, 5).Value = item.SupplierName ?? "";
                    worksheet.Cell(row, 6).Value = item.TotalAmount;
                    worksheet.Cell(row, 7).Value = item.DiscountAmount;
                    worksheet.Cell(row, 8).Value = item.TotalReceivedAmount;
                    worksheet.Cell(row, 9).Value = item.TotalDueAmount;
                    worksheet.Cell(row, 10).Value = item.SaleDescription ?? "";
                    worksheet.Cell(row, 11).Value = item.PaymentMethod ?? "";
                    worksheet.Cell(row, 12).Value = item.IsDeleted ? "Deleted" : "Active";
                    totalAmount += item.TotalAmount;
                    totalDiscount += item.DiscountAmount;
                    totalReceived += item.TotalReceivedAmount;
                    totalDue += item.TotalDueAmount;
                    row++;
                }

                row++;
                worksheet.Cell(row, 5).Value = "TOTAL:";
                worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Value = totalAmount;
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Value = totalDiscount;
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Value = totalReceived;
                worksheet.Cell(row, 8).Style.Font.Bold = true;
                worksheet.Cell(row, 9).Value = totalDue;
                worksheet.Cell(row, 9).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                string filename = $"Sales_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales to Excel");
                TempData["ErrorMessage"] = "An error occurred while exporting to Excel.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Generate Sale Detail Report in Excel (Code, Product, Item Level Discount, Unit Sale Price, Qty, Total Discount, Payable). Uses same filters as Sales Management.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportSaleDetailReportExcel(long? customerId = null, long? billNumber = null, DateTime? saleFrom = null, DateTime? saleDateTo = null, string? description = null)
        {
            try
            {
                var filters = new SalesFilters
                {
                    CustomerId = customerId,
                    BillNumber = billNumber,
                    SaleFrom = saleFrom,
                    SaleDateTo = saleDateTo,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description
                };
                var list = await _salesService.GetSaleDetailReportForExportAsync(filters);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sale Detail Report");

                string[] headers = { "Code", "Product", "Item Level Discount", "Unit Sale Price", "Qty", "Total Discount", "Payable" };
                const int colCount = 7;
                int row = 1;

                worksheet.Cell(row, 1).Value = "Sale Detail Report";
                worksheet.Range(row, 1, row, colCount).Merge().Style.Font.Bold = true;
                worksheet.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.LightBlue;
                row += 2;

                var culture = new System.Globalization.CultureInfo("ur-PK");
                var items = list ?? new List<SaleDetailReportItem>();
                long? currentSaleId = null;
                decimal billTotalPayable = 0;

                foreach (var item in items)
                {
                    if (item.SaleIdFk != currentSaleId)
                    {
                        if (currentSaleId.HasValue)
                        {
                            worksheet.Cell(row, 1).Value = "Total Payable";
                            worksheet.Range(row, 1, row, colCount - 1).Merge().Style.Font.Bold = true;
                            worksheet.Cell(row, colCount).Value = billTotalPayable.ToString("N2", culture);
                            worksheet.Cell(row, colCount).Style.Font.Bold = true;
                            row++;
                        }
                        billTotalPayable = 0;
                        currentSaleId = item.SaleIdFk;
                        var billLabel = item.BillNumber.HasValue ? $"Bill # {item.BillNumber} (Sale Id: {item.SaleIdFk})" : $"Sale Id: {item.SaleIdFk}";
                        worksheet.Cell(row, 1).Value = billLabel;
                        worksheet.Range(row, 1, row, colCount).Merge().Style.Font.Bold = true;
                        worksheet.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.LightBlue;
                        row++;
                        for (int c = 0; c < headers.Length; c++)
                            worksheet.Cell(row, c + 1).Value = headers[c];
                        worksheet.Range(row, 1, row, colCount).Style.Font.Bold = true;
                        worksheet.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.LightGray;
                        row++;
                    }

                    billTotalPayable += item.PayableAmount;
                    worksheet.Cell(row, 1).Value = item.Code ?? "";
                    worksheet.Cell(row, 2).Value = item.ProductName ?? "";
                    worksheet.Cell(row, 3).Value = item.LineDiscountAmount.ToString("N2", culture);
                    worksheet.Cell(row, 4).Value = item.UnitPrice.ToString("N2", culture);
                    worksheet.Cell(row, 5).Value = item.Quantity;
                    worksheet.Cell(row, 6).Value = item.LineDiscountAmount.ToString("N2", culture);
                    worksheet.Cell(row, 7).Value = item.PayableAmount.ToString("N2", culture);
                    row++;
                }

                if (currentSaleId.HasValue)
                {
                    worksheet.Cell(row, 1).Value = "Total Payable";
                    worksheet.Range(row, 1, row, colCount - 1).Merge().Style.Font.Bold = true;
                    worksheet.Cell(row, colCount).Value = billTotalPayable.ToString("N2", culture);
                    worksheet.Cell(row, colCount).Style.Font.Bold = true;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                string filename = $"SaleDetailReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sale detail report to Excel");
                TempData["ErrorMessage"] = "An error occurred while generating the sale detail report.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export sales to PDF (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(long? customerId = null, long? billNumber = null, DateTime? saleFrom = null, DateTime? saleDateTo = null, string? description = null)
        {
            try
            {
                var filters = new SalesFilters
                {
                    CustomerId = customerId,
                    BillNumber = billNumber,
                    SaleFrom = saleFrom,
                    SaleDateTo = saleDateTo,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description
                };
                const int exportPageSize = 100000;
                var model = await _salesService.GetAllSalesAsync(1, exportPageSize, filters);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 12f, 12f, 12f, 12f);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                document.Add(new Paragraph("Sales Management Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                var table = new PdfPTable(12);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 0.8f, 0.8f, 1.2f, 2f, 1.5f, 1.2f, 1f, 1f, 1.2f, 2f, 1.2f, 0.8f });

                string[] headers = { "Sale Id", "Bill #", "Date", "Customer", "Supplier", "Total", "Discount", "Received", "Dues", "Description", "Payment", "Status" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                decimal totalAmount = 0, totalDiscount = 0, totalReceived = 0, totalDue = 0;
                foreach (var s in model.SalesList ?? new List<SaleWithCustomerViewModel>())
                {
                    table.AddCell(s.SaleId.ToString());
                    table.AddCell(s.BillNumber.ToString());
                    table.AddCell(s.SaleDate.ToString("dd-MMM-yy"));
                    table.AddCell(s.CustomerName ?? "");
                    table.AddCell(s.SupplierName ?? "");
                    table.AddCell(s.TotalAmount.ToString("N2"));
                    table.AddCell(s.DiscountAmount.ToString("N2"));
                    table.AddCell(s.TotalReceivedAmount.ToString("N2"));
                    table.AddCell(s.TotalDueAmount.ToString("N2"));
                    table.AddCell(s.SaleDescription ?? "");
                    table.AddCell(s.PaymentMethod ?? "");
                    table.AddCell(s.IsDeleted ? "Deleted" : "Active");
                    totalAmount += s.TotalAmount;
                    totalDiscount += s.DiscountAmount;
                    totalReceived += s.TotalReceivedAmount;
                    totalDue += s.TotalDueAmount;
                }

                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7)))
                {
                    Colspan = 5,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(totalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(totalDiscount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(totalReceived.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(totalDue.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                for (int i = 0; i < 3; i++)
                    table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                string filename = $"Sales_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales to PDF");
                TempData["ErrorMessage"] = "An error occurred while exporting to PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SalesController/PrintReceipt/5
        public async Task<ActionResult> PrintReceipt(long id, bool merchantCopy = false)
        {
            try
            {
                var salePrint = await _salesService.GetSaleForPrintAsync(id);
                var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                var res = await unitConversionService.GetSmallestMeasuringUnitAsync();
                if (salePrint != null)
                {
                    foreach (var item in salePrint.SaleDetails)
                    {
                        if (!item.IsSmallestUnit)
                        {
                            //var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();

                           
                            //var conversionResult = await unitConversionService.ConvertUnitToSmallestAsync(item.MeasuringUnitId, res.MeasuringUnitId, item.Quantity);

                            //if (conversionResult.HasValue)
                            //{
                                //// conversionResult is the result of converting 1 unit from fromUnitId to toUnitId
                                //// So to convert stockInBaseUnit, we multiply: stockInBaseUnit * conversionResult
                                //// Example: 685 kg * (1 bori / 50 kg) = 685 * 0.02 = 13.7 bori
                                
                            //    item.PrintQuantity = conversionResult.Value;
                            //}
                            //else
                            //{
                                item.PrintQuantity = (decimal)item.Quantity;
                           // }
                            

                        }
                        else
                        {
                            item.PrintQuantity = (decimal)item.Quantity;
                        }



                    }

                }

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

        // AJAX endpoint to get available units for conversion
        [HttpGet]
        public async Task<IActionResult> GetAvailableUnits()
        {
            try
            {
                // Get all enabled measuring units
                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(null);
                var result = measuringUnits.Select(u => new
                {
                    value = u.MeasuringUnitId.ToString(),
                    text = $"{u.MeasuringUnitName} ({u.MeasuringUnitAbbreviation})",
                    measuringUnitId = u.MeasuringUnitId,
                    measuringUnitName = u.MeasuringUnitName,
                    measuringUnitAbbreviation = u.MeasuringUnitAbbreviation
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available units");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to convert unit and get stock in selected unit
        [HttpGet]
        public async Task<JsonResult> ConvertQuantityToBaseUnit(long productId, long fromUnitId, long toUnitId, decimal quantity)
        {
            try
            {
                var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                var conversionResult = await unitConversionService.ConvertUnitAsync(fromUnitId, toUnitId, 1);
                
                if (conversionResult.HasValue)
                {
                    // conversionResult is the result of converting 1 unit from fromUnitId to toUnitId
                    // So to convert quantity, we multiply: quantity * conversionResult
                    var convertedQuantity = quantity * conversionResult.Value;
                    
                    _logger.LogInformation("Converted {Quantity} from unit {FromUnitId} to base unit {ToUnitId}: {ConvertedQuantity}", 
                        quantity, fromUnitId, toUnitId, convertedQuantity);
                    
                    return Json(new { success = true, convertedQuantity = convertedQuantity, conversionFactor = conversionResult.Value });
                }
                else
                {
                    _logger.LogWarning("No conversion found from unit {FromUnitId} to unit {ToUnitId}", fromUnitId, toUnitId);
                    // No conversion found, return original quantity
                    return Json(new { success = true, convertedQuantity = quantity, conversionFactor = 1 });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting quantity for product {ProductId}", productId);
                return Json(new { success = false, message = "Error converting quantity", convertedQuantity = quantity });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ConvertUnitAndGetStock(long productId, long? fromUnitId, long? toUnitId, decimal stockInBaseUnit)
        {
            try
            {
                if (!fromUnitId.HasValue || !toUnitId.HasValue)
                {
                    _logger.LogInformation("ConvertUnitAndGetStock: Missing unit IDs - fromUnitId: {FromUnitId}, toUnitId: {ToUnitId}", fromUnitId, toUnitId);
                    return Json(new { success = true, convertedStock = stockInBaseUnit, conversionFactor = 1 });
                }

                _logger.LogInformation("ConvertUnitAndGetStock: Converting {StockInBaseUnit} from unit {FromUnitId} to unit {ToUnitId}", 
                    stockInBaseUnit, fromUnitId.Value, toUnitId.Value);

                var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                
                // ConvertUnitAsync returns the result of converting 1 unit from fromUnitId to toUnitId
                // For example: if 1 bori = 50 kg, then ConvertUnitAsync(kg, bori, 1) should return 0.02 (1/50)
                // But if we have FromUnitId=bori, ToUnitId=kg with factor=50, then:
                // - Direct: ConvertUnitAsync(bori, kg, 1) = 1 * 50 = 50
                // - Reverse: ConvertUnitAsync(kg, bori, 1) = 1 / 50 = 0.02
                
                var conversionResult = await unitConversionService.ConvertUnitAsync(fromUnitId.Value, toUnitId.Value, 1);
                
                if (conversionResult.HasValue)
                {
                    // conversionResult is the result of converting 1 unit from fromUnitId to toUnitId
                    // So to convert stockInBaseUnit, we multiply: stockInBaseUnit * conversionResult
                    // Example: 685 kg * (1 bori / 50 kg) = 685 * 0.02 = 13.7 bori
                    var convertedStock = stockInBaseUnit * conversionResult.Value;
                    
                    _logger.LogInformation("ConvertUnitAndGetStock: Conversion successful - {StockInBaseUnit} * {ConversionFactor} = {ConvertedStock}", 
                        stockInBaseUnit, conversionResult.Value, convertedStock);
                    
                    return Json(new { success = true, convertedStock = convertedStock, conversionFactor = conversionResult.Value });
                }
                else
                {
                    _logger.LogWarning("ConvertUnitAndGetStock: No conversion found from unit {FromUnitId} to unit {ToUnitId}", 
                        fromUnitId.Value, toUnitId.Value);
                    // No conversion found, return original stock
                    return Json(new { success = true, convertedStock = stockInBaseUnit, conversionFactor = 1 });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting unit for product {ProductId}: {Message}", productId, ex.Message);
                return Json(new { success = false, message = "Error converting unit", convertedStock = stockInBaseUnit, conversionFactor = 1 });
            }
        }
    }

    public class CustomerOpenBalanceRequest
    {
        public long CustomerId { get; set; }
        public string Type { get; set; } = ""; // "Payable" or "Receivable"
        public decimal OpeningBalance { get; set; }
        public string? BalanceDate { get; set; } // ISO date from UI (e.g. "yyyy-MM-dd")
    }
}
