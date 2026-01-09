using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class StockController : Controller
    {
        private readonly ILogger<StockController> _logger;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        private readonly IStockService _stockService;
        private readonly ISalesService _saleService;
     
        public StockController(IStockService stockService, ILogger<StockController> logger,ISalesService salesService)
        {
            _stockService = stockService;
            _logger = logger;
            _saleService = salesService;
        }

        // GET: StockController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, StockFilters? stockFilters = null)
        {
            var viewModel = new StockViewModel();

            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                
                // Get search parameter from query string
                var searchUsername = HttpContext.Request.Query["searchUsername"].ToString();
                
                if (stockFilters == null)
                {
                    stockFilters = new StockFilters
                    {
                        ProductName = searchUsername
                    };
                }
                else if (string.IsNullOrEmpty(stockFilters.ProductName))
                {
                    stockFilters.ProductName = searchUsername;
                }
                
                // Store search parameter in ViewData for pagination links
                ViewData["searchUsername"] = searchUsername;
                
                viewModel = await _stockService.GetAllStocksAsync(pageNumber, currentPageSize, stockFilters);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(viewModel);
        }

        // GET: StockController/Details/5
        public async Task<ActionResult> Details(long id)
        {
            try
            {
                var stock = await _stockService.GetStockByIdAsync(id);
                if (stock == null)
                {
                    return NotFound();
                }
                return View(stock);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StockController/Create
        public async Task<ActionResult> Create()
        {
            try
            {
                var categories = await _stockService.GetAllEnabledCategoriesAsync();
                
                // Initialize with empty products list - will be populated via AJAX based on category selection
                ViewBag.Products = new SelectList(new List<Product>(), "ProductId", "ProductName");
                ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
        }

        // POST: StockController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(StockMaster stock)
        {
            try
            {
                bool IsValid = true;
                if (ModelState.IsValid)
                {
                    // Validate quantity relationships
                    if (stock.TotalQuantity < 0)
                    {
                        TempData["WarningMessage"] = "Total quantity cannot be negative.";
                        IsValid = false;
                    }
                    
                    if (IsValid)
                    {
                        var userIdStr = HttpContext.Session.GetString("UserId");
                        long userId = long.Parse(userIdStr);
                        
                        // Check if stock already exists for this product
                        var existingStock = await _stockService.GetStockByProductIdAsync(stock.ProductIdFk); 
                        
                        if (existingStock != null && existingStock.StockMasterId > 0)
                        {
                            // Update existing stock
                            existingStock.TotalQuantity += stock.TotalQuantity;
                            existingStock.AvailableQuantity += stock.TotalQuantity; // Add new stock to available quantity
                            existingStock.ModifiedBy = userId;
                            existingStock.ModifiedDate = DateTime.Now;
                            
                            var updateResult = await _stockService.UpdateStockAsync(existingStock);
                            long transactionReturn = _saleService.SaleTransactionCreate(
                                existingStock.StockMasterId,
                                (decimal)stock.TotalQuantity,
                                string.IsNullOrEmpty(stock.Comment) ? "" :stock.Comment,
                                DateTime.Now,
                                userId,
                                1, // Added Stock Transaction Type
                                0
                            );
                            if (updateResult != 0)
                            {
                                TempData["Success"] = "Stock updated successfully. Added " + stock.TotalQuantity + " units to existing stock.";
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Failed to update existing stock.";
                                return View();
                            }
                        }
                        else
                        {
                            // Create new stock
                            stock.CreatedBy = userId;
                            stock.CreatedDate = DateTime.Now;
                            
                            // For new stock, assume all quantity is available initially
                            stock.AvailableQuantity = stock.TotalQuantity;
                            stock.UsedQuantity = 0;

                            var result = await _stockService.CreateStockAsync(stock);
                            if (result)
                            {
                                TempData["Success"] = AlertMessages.RecordAdded;
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            return View();
        }

        // GET: StockController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                var stock = await _stockService.GetStockByIdAsync(id);
                if (stock == null)
                {
                    return NotFound();
                }

                var products = await _stockService.GetAllProductsAsync();
                var categories = await _stockService.GetAllEnabledCategoriesAsync();
                
                
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", stock.ProductIdFk);
                ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", stock.CategoryIdFk);

                return View(stock);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: StockController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, StockMaster stock)
        {
            bool IsvalidData = true;
            if (id != stock.StockMasterId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate quantity relationships
                    if (stock.AvailableQuantity < 0 || stock.UsedQuantity < 0 || stock.TotalQuantity < 0)
                    {
                        TempData["WarningMessage"] =  "Quantities cannot be negative.";
                        IsvalidData = false;
                    }
                    else if (stock.TotalQuantity != (stock.AvailableQuantity + stock.UsedQuantity))
                    {
                        TempData["WarningMessage"] =  "Total quantity must equal Available quantity + Used quantity.";
                        IsvalidData = false;
                    }
                    
                    if (IsvalidData)
                    {
                        stock.ModifiedDate = DateTime.Now;
                        var userIdStr = HttpContext.Session.GetString("UserId");
                        long userId = long.Parse(userIdStr);
                        stock.ModifiedBy = userId;
                        var existingStock = await _stockService.GetStockByProductIdAsync(stock.ProductIdFk);
                        var response = await _stockService.UpdateStockAsync(stock);
                        var userInputStock = stock.TotalQuantity- existingStock.TotalQuantity;

                        long transactionReturn = _saleService.SaleTransactionCreate(
                               stock.StockMasterId,
                               (decimal)userInputStock,
                               string.IsNullOrEmpty(stock.Comment) ? "Editted Stocks" : $"Editted Stock \t{stock.Comment}",
                               DateTime.Now,
                               userId,
                               1, // Added Stock Transaction Type
                               0
                           );
                        if (response != 0)
                        {
                            TempData["Success"] = AlertMessages.RecordUpdated;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                            return View(nameof(Edit));
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            
            // Ensure ViewBag.Products and ViewBag.Categories are populated when returning View
            try
            {
                var products = await _stockService.GetAllProductsAsync();
                var categories = await _stockService.GetAllEnabledCategoriesAsync();
                
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", stock.ProductIdFk);
                ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", stock.CategoryIdFk);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading products and categories: " + ex.Message;
            }
            
            return View(stock);
        }

        // GET: StockController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                var stock = await _stockService.GetStockByIdAsync(id);
                if (stock == null)
                {
                    return NotFound();
                }
                return View(stock);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: StockController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr);
                var modifiedDate = DateTime.Now;
                
                var res = await _stockService.DeleteStockAsync(id, modifiedDate, userId);
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

        // API endpoint to get products by category ID
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(long categoryId)
        {
            try
            {
                var products = await _stockService.GetProductsByCategoryIdAsync(categoryId);
                return Json(products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new { value = p.ProductId, text = p.ProductName }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category");
                return Json(new List<object>());
            }
        }

        // API endpoint to get existing stock for a product
        [HttpGet]
        public async Task<IActionResult> GetExistingStock(long productId)
        {
            try
            {
                var stock = await _stockService.GetStockByProductIdAsync(productId);
                if (stock != null && stock.StockMasterId > 0)
                {
                    return Json(new { 
                        success = true, 
                        availableQuantity = stock.AvailableQuantity,
                        totalQuantity = stock.TotalQuantity,
                        usedQuantity = stock.UsedQuantity
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "No existing stock found for this product",
                        availableQuantity = 0,
                        totalQuantity = 0,
                        usedQuantity = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing stock for product {ProductId}", productId);
                return Json(new { 
                    success = false, 
                    message = "Error retrieving stock information",
                    availableQuantity = 0,
                    totalQuantity = 0,
                    usedQuantity = 0
                });
            }
        }

        // GET: StockController/History
        //public async Task<ActionResult> History(int pageNumber = 1, int? pageSize = null, long? StockMasterId = null, StockHistoryFilters? filters = null)
        //{
        //    var viewModel = new StockHistoryViewModel();

        //    try
        //    {

        //        //  Store StockMasterId temporarily for the view
        //        ViewBag.StockMasterId = StockMasterId;

        //        // You can also store it in TempData if you need it across multiple requests
        //        if (StockMasterId.HasValue)
        //            TempData["StockMasterId"] = StockMasterId.Value;
        //        int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
        //        if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
        //        {
        //            currentPageSize = pageSize.Value;
        //            HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
        //        }

        //        if (filters == null)
        //        {
        //            filters = new StockHistoryFilters();
        //        }
        //        if (!StockMasterId.HasValue && TempData["StockMasterId"] != null)
        //            StockMasterId = (long?)TempData["StockMasterId"];

        //        // If productId is provided, filter by that specific product
        //        if (StockMasterId.HasValue)
        //        {
        //            filters.StockMasterId = StockMasterId.Value;
        //        }

        //        // Handle TransactionTypeId if needed
        //        if (filters.TransactionTypeId.HasValue && filters.TransactionTypeId.Value > 0)
        //        {
        //            // TransactionTypeId is already set, no need to parse
        //        }

        //        viewModel = await _stockService.GetStockHistoryAsync(pageNumber, currentPageSize, filters);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = ex.Message;
        //        _logger.LogError(ex, "Error loading stock history");
        //    }

        //    return View(viewModel);

        //}
        public async Task<ActionResult> History(long? StockMasterId = null, IFormCollection formcollect = null)
        {
            var viewModel = new StockHistoryViewModel();
            int pageNumber = 1; int? pageSize = null;
            StockHistoryFilters? filters = null;
            try
            {
               

                //  Store StockMasterId temporarily for the view
                ViewBag.StockMasterId = StockMasterId;

                // You can also store it in TempData if you need it across multiple requests
                if (StockMasterId.HasValue)
                    TempData["StockMasterId"] = StockMasterId.Value;
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                if (filters == null)
                {
                    filters = new StockHistoryFilters();
                }
                if (!StockMasterId.HasValue && TempData["StockMasterId"] != null)
                    StockMasterId = (long?)TempData["StockMasterId"];

                // If productId is provided, filter by that specific product
                if (StockMasterId.HasValue)
                {
                    filters.StockMasterId = StockMasterId.Value;
                }
              
                // Handle TransactionTypeId if needed
                if (filters.TransactionTypeId.HasValue && filters.TransactionTypeId.Value > 0)
                {
                    // TransactionTypeId is already set, no need to parse
                }
             
                viewModel = await _stockService.GetStockHistoryAsync(pageNumber, currentPageSize, filters);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogError(ex, "Error loading stock history");
            }

            return View(viewModel);
         
        }

        // AJAX endpoint to get stock history for modal
        [HttpGet]
        public async Task<IActionResult> GetStockHistory(long stockMasterId, int pageNumber = 1, int pageSize = 10, 
            DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            try
            {
                var filters = new StockHistoryFilters
                {
                    StockMasterId = stockMasterId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TransactionTypeId = string.IsNullOrEmpty(transactionType) ? 0 : Int64.Parse(transactionType)
                };

                var viewModel = await _stockService.GetStockHistoryAsync(pageNumber, pageSize, filters);
                
                // Get stock details for summary
                var stock = await _stockService.GetStockByIdAsync(stockMasterId);
                
                var result = new
                {
                    success = true,
                    transactions = viewModel.TransactionList.Select(t => new
                    {
                        stockTransactionId = t.StockTransactionId,
                        stockQuantity = t.StockQuantity,
                        transactionDate = t.TransactionDate,
                        transactionType = t.TransactionType,
                        description = t.Description
                    }).ToList(),
                    currentPage = viewModel.CurrentPage,
                    totalPages = viewModel.TotalPages,
                    totalCount = viewModel.TotalCount,
                    stockSummary = new
                    {
                        transactionCount = viewModel.TotalCount,
                        availableQuantity = stock?.AvailableQuantity ?? 0
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history for modal");
                return Json(new { success = false, message = "Error loading stock history" });
            }
        }
    }
}
