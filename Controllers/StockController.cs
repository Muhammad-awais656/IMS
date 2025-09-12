using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
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

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
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
                if (stockFilters == null)
                {
                    stockFilters = new StockFilters
                    {
                        ProductName = HttpContext.Request.Query["searchUsername"].ToString()
                    };
                }
                else if (string.IsNullOrEmpty(stockFilters.ProductName))
                {
                    stockFilters.ProductName = HttpContext.Request.Query["searchUsername"].ToString();
                }
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
                if (ModelState.IsValid)
                {
                    // Validate quantity relationships
                    if (stock.TotalQuantity < 0)
                    {
                        ModelState.AddModelError("", "Total quantity cannot be negative.");
                    }
                    
                    if (ModelState.IsValid)
                    {
                        var userIdStr = HttpContext.Session.GetString("UserId");
                        long userId = long.Parse(userIdStr);
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
                        ModelState.AddModelError("", "Quantities cannot be negative.");
                    }
                    else if (stock.TotalQuantity != (stock.AvailableQuantity + stock.UsedQuantity))
                    {
                        ModelState.AddModelError("", "Total quantity must equal Available quantity + Used quantity.");
                    }
                    
                    if (ModelState.IsValid)
                    {
                        stock.ModifiedDate = DateTime.Now;
                        var userIdStr = HttpContext.Session.GetString("UserId");
                        long userId = long.Parse(userIdStr);
                        stock.ModifiedBy = userId;

                        var response = await _stockService.UpdateStockAsync(stock);
                        
                        if (response != 0)
                        {
                            TempData["Success"] = AlertMessages.RecordUpdated;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                            return View(stock);
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
                return Json(products.Select(p => new { value = p.ProductId, text = p.ProductName }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category");
                return Json(new List<object>());
            }
        }
    }
}
