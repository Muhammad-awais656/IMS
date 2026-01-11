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
     
        private readonly IProductService _productService;
        private readonly IAdminMeasuringUnitService _measuringUnitService;
     
        public StockController(IStockService stockService, ILogger<StockController> logger, ISalesService salesService, IProductService productService, IAdminMeasuringUnitService measuringUnitService)
        {
            _stockService = stockService;
            _logger = logger;
            _saleService = salesService;
            _productService = productService;
            _measuringUnitService = measuringUnitService;
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
                var selectedUnitIdStr = HttpContext.Request.Query["selectedUnitId"].ToString();
                long? selectedUnitId = null;
                if (!string.IsNullOrEmpty(selectedUnitIdStr) && long.TryParse(selectedUnitIdStr, out long parsedUnitId))
                {
                    selectedUnitId = parsedUnitId;
                }
                
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
                
                // Preserve search value in ViewData for pagination links
                ViewData["searchUsername"] = searchUsername;
                ViewData["selectedUnitId"] = selectedUnitIdStr;
                
                viewModel = await _stockService.GetAllStocksAsync(pageNumber, currentPageSize, stockFilters);
                
                // Store selected unit ID in ViewData for JavaScript
                ViewData["selectedUnitId"] = selectedUnitIdStr;
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
                        
                        // Get measuring unit ID from form (if provided)
                        var measuringUnitIdStr = Request.Form["MeasuringUnitIdHidden"].ToString();
                        long? measuringUnitId = null;
                        if (!string.IsNullOrEmpty(measuringUnitIdStr) && long.TryParse(measuringUnitIdStr, out long parsedUnitId))
                        {
                            measuringUnitId = parsedUnitId;
                        }
                        
                        // Convert quantity to base unit if a measuring unit is selected
                        if (measuringUnitId.HasValue && stock.ProductIdFk > 0)
                        {
                            // Get product's base unit
                            var product = await _productService.GetProductByIdAsync(stock.ProductIdFk);
                            if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                            {
                                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                                var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                                long? baseUnitId = null;
                                
                                if (smallestUnit != null)
                                {
                                    baseUnitId = smallestUnit.MeasuringUnitId;
                                }
                                else if (measuringUnits.Any())
                                {
                                    baseUnitId = measuringUnits.First().MeasuringUnitId;
                                }
                                
                                // If selected unit is different from base unit, convert quantity
                                if (baseUnitId.HasValue && measuringUnitId.Value != baseUnitId.Value)
                                {
                                    _logger.LogInformation("Create Stock: Converting quantity from unit {SelectedUnitId} to base unit {BaseUnitId}", 
                                        measuringUnitId.Value, baseUnitId.Value);
                                    
                                    var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                                    
                                    // Get all conversions where FromUnitId = selected unit
                                    var conversionsFromSelectedUnit = await unitConversionService.GetConversionsByFromUnitAsync(measuringUnitId.Value);
                                    
                                    // Find conversion where ToUnitId = base unit
                                    var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == baseUnitId.Value && c.IsEnabled);
                                    
                                    if (conversion != null)
                                    {
                                        var conversionFactor = conversion.ConversionFactor;
                                        // Convert: quantity * conversionFactor (e.g., 5 bori * 50 = 250 kg)
                                        var originalQuantity = stock.TotalQuantity;
                                        stock.TotalQuantity = stock.TotalQuantity * conversionFactor;
                                        
                                        _logger.LogInformation("Create Stock: Conversion successful - {OriginalQuantity} * {ConversionFactor} = {ConvertedQuantity}", 
                                            originalQuantity, conversionFactor, stock.TotalQuantity);
                                    }
                                    else
                                    {
                                        // Try reverse conversion
                                        var conversionsFromBaseUnit = await unitConversionService.GetConversionsByFromUnitAsync(baseUnitId.Value);
                                        var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == measuringUnitId.Value && c.IsEnabled);
                                        
                                        if (reverseConversion != null)
                                        {
                                            var conversionFactor = reverseConversion.ConversionFactor;
                                            // If conversion is FromUnitId=kg, ToUnitId=Bori, Factor=0.02, then divide
                                            var originalQuantity = stock.TotalQuantity;
                                            stock.TotalQuantity = stock.TotalQuantity / conversionFactor;
                                            
                                            _logger.LogInformation("Create Stock: Reverse conversion successful - {OriginalQuantity} / {ConversionFactor} = {ConvertedQuantity}", 
                                                originalQuantity, conversionFactor, stock.TotalQuantity);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Create Stock: No conversion found from unit {SelectedUnitId} to base unit {BaseUnitId}", 
                                                measuringUnitId.Value, baseUnitId.Value);
                                        }
                                    }
                                }
                            }
                        }
                        
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
                        
                        // Get measuring unit ID from form (if provided)
                        var measuringUnitIdStr = Request.Form["MeasuringUnitIdHidden"].ToString();
                        long? measuringUnitId = null;
                        if (!string.IsNullOrEmpty(measuringUnitIdStr) && long.TryParse(measuringUnitIdStr, out long parsedUnitId))
                        {
                            measuringUnitId = parsedUnitId;
                        }
                        
                        // Convert quantities to base unit if a measuring unit is selected
                        if (measuringUnitId.HasValue && stock.ProductIdFk > 0)
                        {
                            // Get product's base unit
                            var product = await _productService.GetProductByIdAsync(stock.ProductIdFk);
                            if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                            {
                                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                                var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                                long? baseUnitId = null;
                                
                                if (smallestUnit != null)
                                {
                                    baseUnitId = smallestUnit.MeasuringUnitId;
                                }
                                else if (measuringUnits.Any())
                                {
                                    baseUnitId = measuringUnits.First().MeasuringUnitId;
                                }
                                
                                // If selected unit is different from base unit, convert quantities
                                if (baseUnitId.HasValue && measuringUnitId.Value != baseUnitId.Value)
                                {
                                    _logger.LogInformation("Edit Stock: Converting quantities from unit {SelectedUnitId} to base unit {BaseUnitId}", 
                                        measuringUnitId.Value, baseUnitId.Value);
                                    
                                    var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                                    
                                    // Get all conversions where FromUnitId = selected unit
                                    var conversionsFromSelectedUnit = await unitConversionService.GetConversionsByFromUnitAsync(measuringUnitId.Value);
                                    
                                    // Find conversion where ToUnitId = base unit
                                    var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == baseUnitId.Value && c.IsEnabled);
                                    
                                    if (conversion != null)
                                    {
                                        var conversionFactor = conversion.ConversionFactor;
                                        // Convert: quantity * conversionFactor (e.g., 5 bori * 50 = 250 kg)
                                        var originalAvailable = stock.AvailableQuantity;
                                        var originalUsed = stock.UsedQuantity;
                                        var originalTotal = stock.TotalQuantity;
                                        
                                        stock.AvailableQuantity = stock.AvailableQuantity * conversionFactor;
                                        stock.UsedQuantity = stock.UsedQuantity * conversionFactor;
                                        stock.TotalQuantity = stock.TotalQuantity * conversionFactor;
                                        
                                        _logger.LogInformation("Edit Stock: Conversion successful - Available: {Original} * {Factor} = {Converted}, Used: {OriginalUsed} * {Factor} = {ConvertedUsed}, Total: {OriginalTotal} * {Factor} = {ConvertedTotal}", 
                                            originalAvailable, conversionFactor, stock.AvailableQuantity,
                                            originalUsed, conversionFactor, stock.UsedQuantity,
                                            originalTotal, conversionFactor, stock.TotalQuantity);
                                    }
                                    else
                                    {
                                        // Try reverse conversion
                                        var conversionsFromBaseUnit = await unitConversionService.GetConversionsByFromUnitAsync(baseUnitId.Value);
                                        var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == measuringUnitId.Value && c.IsEnabled);
                                        
                                        if (reverseConversion != null)
                                        {
                                            var conversionFactor = reverseConversion.ConversionFactor;
                                            // If conversion is FromUnitId=kg, ToUnitId=Bori, Factor=0.02, then divide
                                            var originalAvailable = stock.AvailableQuantity;
                                            var originalUsed = stock.UsedQuantity;
                                            var originalTotal = stock.TotalQuantity;
                                            
                                            stock.AvailableQuantity = stock.AvailableQuantity / conversionFactor;
                                            stock.UsedQuantity = stock.UsedQuantity / conversionFactor;
                                            stock.TotalQuantity = stock.TotalQuantity / conversionFactor;
                                            
                                            _logger.LogInformation("Edit Stock: Reverse conversion successful - Available: {Original} / {Factor} = {Converted}, Used: {OriginalUsed} / {Factor} = {ConvertedUsed}, Total: {OriginalTotal} / {Factor} = {ConvertedTotal}", 
                                                originalAvailable, conversionFactor, stock.AvailableQuantity,
                                                originalUsed, conversionFactor, stock.UsedQuantity,
                                                originalTotal, conversionFactor, stock.TotalQuantity);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Edit Stock: No conversion found from unit {SelectedUnitId} to base unit {BaseUnitId}", 
                                                measuringUnitId.Value, baseUnitId.Value);
                                        }
                                    }
                                }
                            }
                        }
                        
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

        // API endpoint to get categories for Kendo ComboBox
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _stockService.GetAllEnabledCategoriesAsync();
                var result = categories.Select(c => new
                {
                    value = c.CategoryId.ToString(),
                    text = c.CategoryName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // API endpoint to get measuring units for a product
        [HttpGet]
        public async Task<IActionResult> GetMeasuringUnitsByProduct(long productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null || !product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                {
                    return Json(new List<object>());
                }

                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                
                // Filter only enabled measuring units
                var enabledMeasuringUnits = measuringUnits.Where(mu => mu.IsEnabled).ToList();
                
                var result = enabledMeasuringUnits.Select(mu => new
                {
                    value = mu.MeasuringUnitId.ToString(),
                    text = $"{mu.MeasuringUnitName} ({mu.MeasuringUnitAbbreviation})",
                    measuringUnitId = mu.MeasuringUnitId,
                    measuringUnitName = mu.MeasuringUnitName,
                    measuringUnitAbbreviation = mu.MeasuringUnitAbbreviation
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting measuring units for product {ProductId}", productId);
                return Json(new List<object>());
            }
        }

        // API endpoint to get ProductId from StockMasterId
        [HttpGet]
        public async Task<IActionResult> GetStockProductId(long stockId)
        {
            try
            {
                var stock = await _stockService.GetStockByIdAsync(stockId);
                if (stock != null)
                {
                    return Json(new { success = true, productId = stock.ProductIdFk });
                }
                return Json(new { success = false, productId = (long?)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product ID for stock {StockId}", stockId);
                return Json(new { success = false, productId = (long?)null });
            }
        }

        // API endpoint to get product's base unit
        [HttpGet]
        public async Task<IActionResult> GetProductBaseUnit(long productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null || !product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                {
                    return Json(new { success = false, baseUnitId = (long?)null });
                }

                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                
                if (smallestUnit != null)
                {
                    return Json(new { success = true, baseUnitId = smallestUnit.MeasuringUnitId });
                }
                
                return Json(new { success = false, baseUnitId = (long?)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting base unit for product {ProductId}", productId);
                return Json(new { success = false, baseUnitId = (long?)null });
            }
        }

        // API endpoint to convert stock from base unit to selected unit for display
        // Logic: Find conversion where FromUnitId = selected unit, ToUnitId = base unit
        // Then divide stock by conversion factor: stockInBaseUnit / conversionFactor
        [HttpGet]
        public async Task<IActionResult> ConvertStockForDisplay(long productId, long? fromUnitId, long? toUnitId, decimal stockInBaseUnit)
        {
            try
            {
                if (!fromUnitId.HasValue || !toUnitId.HasValue)
                {
                    _logger.LogInformation("ConvertStockForDisplay: Missing unit IDs - fromUnitId: {FromUnitId}, toUnitId: {ToUnitId}", fromUnitId, toUnitId);
                    return Json(new { success = true, convertedStock = stockInBaseUnit, conversionFactor = 1 });
                }

                // If same unit, no conversion needed
                if (fromUnitId.Value == toUnitId.Value)
                {
                    return Json(new { success = true, convertedStock = stockInBaseUnit, conversionFactor = 1 });
                }

                _logger.LogInformation("ConvertStockForDisplay: Converting {StockInBaseUnit} from base unit {FromUnitId} to selected unit {ToUnitId}", 
                    stockInBaseUnit, fromUnitId.Value, toUnitId.Value);

                // Find conversion where FromUnitId = selected unit (toUnitId) and ToUnitId = base unit (fromUnitId)
                // Example: If selected unit is Bori and base unit is kg, find: FromUnitId=Bori, ToUnitId=kg, Factor=50
                // Then: stockInBaseUnit / factor = 685 kg / 50 = 13.7 bori
                var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                
                // Get all conversions where FromUnitId = selected unit
                var conversionsFromSelectedUnit = await unitConversionService.GetConversionsByFromUnitAsync(toUnitId.Value);
                
                // Find conversion where ToUnitId = base unit
                var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == fromUnitId.Value && c.IsEnabled);
                
                if (conversion != null)
                {
                    var conversionFactor = conversion.ConversionFactor;
                    // Divide stock by conversion factor: stockInBaseUnit / conversionFactor
                    // Example: 685 kg / 50 = 13.7 bori
                    var convertedStock = stockInBaseUnit / conversionFactor;
                    
                    _logger.LogInformation("ConvertStockForDisplay: Found conversion - FromUnitId={SelectedUnitId}, ToUnitId={BaseUnitId}, Factor={Factor}", 
                        toUnitId.Value, fromUnitId.Value, conversionFactor);
                    _logger.LogInformation("ConvertStockForDisplay: Conversion calculation - {StockInBaseUnit} / {ConversionFactor} = {ConvertedStock}", 
                        stockInBaseUnit, conversionFactor, convertedStock);
                    
                    return Json(new { success = true, convertedStock = convertedStock, conversionFactor = conversionFactor });
                }
                
                // Try reverse: Find conversion where FromUnitId = base unit and ToUnitId = selected unit
                var conversionsFromBaseUnit = await unitConversionService.GetConversionsByFromUnitAsync(fromUnitId.Value);
                var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == toUnitId.Value && c.IsEnabled);
                
                if (reverseConversion != null)
                {
                    var conversionFactor = reverseConversion.ConversionFactor;
                    // If conversion is FromUnitId=kg, ToUnitId=Bori, Factor=0.02, then multiply
                    // But this scenario is less common - usually it's FromUnitId=Bori, ToUnitId=kg, Factor=50
                    var convertedStock = stockInBaseUnit * conversionFactor;
                    
                    _logger.LogInformation("ConvertStockForDisplay: Found reverse conversion - FromUnitId={BaseUnitId}, ToUnitId={SelectedUnitId}, Factor={Factor}", 
                        fromUnitId.Value, toUnitId.Value, conversionFactor);
                    _logger.LogInformation("ConvertStockForDisplay: Reverse conversion calculation - {StockInBaseUnit} * {ConversionFactor} = {ConvertedStock}", 
                        stockInBaseUnit, conversionFactor, convertedStock);
                    
                    return Json(new { success = true, convertedStock = convertedStock, conversionFactor = conversionFactor });
                }
                
                _logger.LogWarning("ConvertStockForDisplay: No conversion found - FromUnitId={SelectedUnitId} to ToUnitId={BaseUnitId} or reverse", 
                    toUnitId.Value, fromUnitId.Value);
                // No conversion found, return original stock
                return Json(new { success = false, message = "No conversion found", convertedStock = stockInBaseUnit, conversionFactor = 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting stock for display for product {ProductId}", productId);
                return Json(new { success = false, message = "Error converting stock", convertedStock = stockInBaseUnit });
            }
        }

        // API endpoint to convert quantity to base unit
        [HttpGet]
        public async Task<IActionResult> ConvertQuantityToBaseUnit(long productId, long fromUnitId, long toUnitId, decimal quantity)
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

        // API endpoint to get products by category ID
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(long categoryId)
        {
            try
            {
                var products = await _stockService.GetProductsByCategoryIdAsync(categoryId);
                return Json(products.Select(p => new { value = p.ProductId.ToString(), text = p.ProductName }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category");
                return Json(new List<object>());
            }
        }

        // API endpoint to get existing stock for a product
        // If selectedUnitId is provided, converts stock to that unit based on unit conversion configuration
        [HttpGet]
        public async Task<IActionResult> GetExistingStock(long productId, long? selectedUnitId = null)
        {
            try
            {
                var stock = await _stockService.GetStockByProductIdAsync(productId);
                if (stock != null && stock.StockMasterId > 0)
                {
                    // Get product's base unit
                    long? baseUnitId = null;
                    var product = await _productService.GetProductByIdAsync(productId);
                    if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                    {
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

                    decimal convertedStock = stock.AvailableQuantity;
                    bool conversionApplied = false;
                    
                    // If selected unit ID is provided and different from base unit, perform conversion
                    if (selectedUnitId.HasValue && baseUnitId.HasValue && selectedUnitId.Value != baseUnitId.Value)
                    {
                        _logger.LogInformation("GetExistingStock: Converting stock from base unit {BaseUnitId} to selected unit {SelectedUnitId}", 
                            baseUnitId.Value, selectedUnitId.Value);
                        
                        // Get unit conversion service
                        var unitConversionService = HttpContext.RequestServices.GetRequiredService<IUnitConversionService>();
                        
                        // Get all conversions where FromUnitId = selected unit
                        var conversionsFromSelectedUnit = await unitConversionService.GetConversionsByFromUnitAsync(selectedUnitId.Value);
                        
                        // Find conversion where ToUnitId = base unit
                        var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == baseUnitId.Value && c.IsEnabled);
                        
                        if (conversion != null)
                        {
                            var conversionFactor = conversion.ConversionFactor;
                            // Divide stock by conversion factor: stockInBaseUnit / conversionFactor
                            // Example: 685 kg / 50 = 13.7 bori
                            convertedStock = stock.AvailableQuantity / conversionFactor;
                            conversionApplied = true;
                            
                            _logger.LogInformation("GetExistingStock: Found conversion - FromUnitId={SelectedUnitId}, ToUnitId={BaseUnitId}, Factor={Factor}", 
                                selectedUnitId.Value, baseUnitId.Value, conversionFactor);
                            _logger.LogInformation("GetExistingStock: Conversion calculation - {StockInBaseUnit} / {ConversionFactor} = {ConvertedStock}", 
                                stock.AvailableQuantity, conversionFactor, convertedStock);
                        }
                        else
                        {
                            // Try reverse: Find conversion where FromUnitId = base unit and ToUnitId = selected unit
                            var conversionsFromBaseUnit = await unitConversionService.GetConversionsByFromUnitAsync(baseUnitId.Value);
                            var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == selectedUnitId.Value && c.IsEnabled);
                            
                            if (reverseConversion != null)
                            {
                                var conversionFactor = reverseConversion.ConversionFactor;
                                // If conversion is FromUnitId=kg, ToUnitId=Bori, Factor=0.02, then multiply
                                convertedStock = stock.AvailableQuantity * conversionFactor;
                                conversionApplied = true;
                                
                                _logger.LogInformation("GetExistingStock: Found reverse conversion - FromUnitId={BaseUnitId}, ToUnitId={SelectedUnitId}, Factor={Factor}", 
                                    baseUnitId.Value, selectedUnitId.Value, conversionFactor);
                                _logger.LogInformation("GetExistingStock: Reverse conversion calculation - {StockInBaseUnit} * {ConversionFactor} = {ConvertedStock}", 
                                    stock.AvailableQuantity, conversionFactor, convertedStock);
                            }
                            else
                            {
                                _logger.LogWarning("GetExistingStock: No conversion found - FromUnitId={SelectedUnitId} to ToUnitId={BaseUnitId} or reverse", 
                                    selectedUnitId.Value, baseUnitId.Value);
                            }
                        }
                    }
                    
                    return Json(new { 
                        success = true, 
                        availableQuantity = stock.AvailableQuantity,
                        convertedStock = convertedStock,
                        conversionApplied = conversionApplied,
                        totalQuantity = stock.TotalQuantity,
                        usedQuantity = stock.UsedQuantity,
                        baseUnitId = baseUnitId
                    });
                }
                else
                {
                    // Get product's base unit even if no stock exists
                    long? baseUnitId = null;
                    var product = await _productService.GetProductByIdAsync(productId);
                    if (product != null && product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                    {
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
                        success = false, 
                        message = "No existing stock found for this product",
                        availableQuantity = 0,
                        totalQuantity = 0,
                        usedQuantity = 0,
                        baseUnitId = baseUnitId
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
                    usedQuantity = 0,
                    baseUnitId = (long?)null
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
