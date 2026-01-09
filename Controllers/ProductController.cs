using DocumentFormat.OpenXml.Office2010.Excel;
using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Linq;
using static NuGet.Packaging.PackagingConstants;

namespace IMS.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IAdminMeasuringUnitTypesService _adminMeasuringUnitTypesService;
        private readonly IAdminLablesService _adminLablesService;
        private readonly IVendor _vendorService;
        private readonly IAdminMeasuringUnitService _adminMeasuringUnitService;

        public ProductController(IProductService productService,ILogger<ProductController> logger,ICategoryService categoryService
            , IAdminMeasuringUnitTypesService adminMeasuringUnitTypesService, 
            IAdminLablesService adminLablesService,
            IVendor vendor,
            IAdminMeasuringUnitService adminMeasuringUnitService)
        {
                _logger = logger;
                _productService = productService;
                _categoryService = categoryService;
            _adminMeasuringUnitTypesService = adminMeasuringUnitTypesService;
            _adminLablesService = adminLablesService;
            _vendorService = vendor;
            _adminMeasuringUnitService = adminMeasuringUnitService;
        }
        
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null )
        {
            
            var model = new ProductViewModel
            {
                CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync(),
                LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync(),
                MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync()
            };
            ViewBag.Categories = new SelectList(model.CategoryNameList, "CategoryId", "CategoryName");
            ViewBag.Labels = new SelectList(model.LabelNameList, "LabelId", "LabelName");
            ViewBag.MeasuringUnitTypes = new SelectList(model.MeasuringUnitTypeNameList, "MeasuringUnitTypeId", "MeasuringUnitTypeName");
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchpName"].ToString()))
            {
                model.productFilters.ProductId = Convert.ToInt64(HttpContext.Request.Query["searchpName"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchpCode"].ToString()))
            {
                model.productFilters.ProductCode = HttpContext.Request.Query["searchpCode"].ToString();
            }
            if (!string.IsNullOrEmpty( HttpContext.Request.Query["searchpFrom"].ToString()))
            {
                model.productFilters.PriceFrom = Convert.ToDecimal(HttpContext.Request.Query["searchpFrom"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchpTo"].ToString()))
            {
                model.productFilters.PriceTo = Convert.ToDecimal(HttpContext.Request.Query["searchpTo"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["catId"].ToString()))
            {
                model.productFilters.CategoryId = Convert.ToInt64(HttpContext.Request.Query["catId"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchULabelId"].ToString()))
            {
                model.productFilters.LabelId = Convert.ToInt64(HttpContext.Request.Query["searchULabelId"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchUMUTId"].ToString()))
            {
                model.productFilters.MeasuringUnitTypeId = Convert.ToInt64(HttpContext.Request.Query["searchUMUTId"]);
            }


            model = await _productService.GetAllProductAsync(pageNumber, currentPageSize, model.productFilters);
            return View(model);
        }
      


        // GET: ProductController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProductController/Create
        public async Task<ActionResult> Create()
        {
            var model = new ProductViewModel
            {
                CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync(),
                LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync(),
                MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync(),
                VendorsList = await _vendorService.GetAllEnabledVendors()
            };
            ViewBag.Categories = new SelectList(model.CategoryNameList, "CategoryId", "CategoryName");
            ViewBag.Labels = new SelectList(model.LabelNameList, "LabelId", "LabelName");
            ViewBag.MeasuringUnitTypes = new SelectList(model.MeasuringUnitTypeNameList, "MeasuringUnitTypeId", "MeasuringUnitTypeName");
            ViewBag.Vendors = new SelectList(model.VendorsList, "SupplierId", "SupplierName");
            return View(model);
        }

        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductViewModel model)
        {
            try
            {
                // Remove ProductRange validation errors from ModelState since we handle them separately
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("ProductRange"))
                    {
                        keysToRemove.Add(key);
                    }
                    if (key.StartsWith("ProductList"))
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                // Validate only the main product properties
                if (ModelState.IsValid)
                {
                    // Check if product code already exists
                    if (!string.IsNullOrEmpty(model.ProductCode))
                    {
                        var codeExists = await _productService.ProductCodeExistsAsync(model.ProductCode);
                        if (codeExists)
                        {
                            TempData["WarningMessage"] = "Product code already exists. Please use a different code.";
                            TempData["InfoMessage"] = "Product code already exists. Please use a different code.";

                        }
                    }

                    // Validate product ranges if any exist
                    if (model.productRanges != null && model.productRanges.Any())
                    {
                        for (int i = 0; i < model.productRanges.Count; i++)
                        {
                            var range = model.productRanges[i];
                            if (range.MeasuringUnitIdFk == 0 || range.UnitPrice == 0)
                            {
                                
                                TempData["WarningMessage"] = $"productRanges[{i}]"+ "All size fields are required and must be greater than 0.";
                            }
                        }
                    }

                    // If there are validation errors, reload the view
                    if (!ModelState.IsValid)
                    {
                        await ReloadViewDataAsync(model);
                        return View(model);
                    }

                    var product = new Product
                    {
                        ProductName = model.ProductName,
                        ProductCode = string.IsNullOrWhiteSpace(model.ProductCode) ? null : model.ProductCode,
                        CategoryIdFk = model.CategoryId ?? 0,
                        LabelIdFk = model.LabelId ?? 0,
                        MeasuringUnitTypeIdFk = model.MUTId ?? 0,
                        SupplierIdFk = model.VendorId ?? 0,
                        UnitPrice = model.Price ?? 0,
                        ProductDescription = string.IsNullOrWhiteSpace(model.ProductDescription) ? null : model.ProductDescription,
                        Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location,
                        IsEnabled = model.IsEnabled ? (byte)1 : (byte)0,
                        SizeIdFk = 0 // Default value as per your SP
                    };
                    
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    product.CreatedBy = userId;
                    product.CreatedDate = DateTimeHelper.Now;
                    product.ModifiedBy = userId;
                    product.ModifiedDate = DateTimeHelper.Now;
                    
                    var result = await _productService.CreateProductAsync(product);
                    if (result)
                    {
                        // Get the created product ID
                        var createdProduct = await _productService.GetProductByCodeAsync(model.ProductCode);
                        if (createdProduct != null && model.productRanges != null && model.productRanges.Any())
                        {
                            // Save all product ranges
                            foreach (var range in model.productRanges)
                            {
                                range.ProductIdFk = createdProduct.ProductId;
                                await _productService.CreateProductRange(range);
                            }
                        }
                        
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        await ReloadViewDataAsync(model);
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                await ReloadViewDataAsync(model);
                return View(model);
            }
            
            // Reload the view with necessary data if validation fails
            await ReloadViewDataAsync(model);
            return View(model);
        }

        private async Task ReloadViewDataAsync(ProductViewModel model)
        {
            model.CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync();
            model.LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync();
            model.MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync();
            model.VendorsList = await _vendorService.GetAllEnabledVendors();
            
            ViewBag.Categories = new SelectList(model.CategoryNameList, "CategoryId", "CategoryName");
            ViewBag.Labels = new SelectList(model.LabelNameList, "LabelId", "LabelName");
            ViewBag.MeasuringUnitTypes = new SelectList(model.MeasuringUnitTypeNameList, "MeasuringUnitTypeId", "MeasuringUnitTypeName");
            ViewBag.Vendors = new SelectList(model.VendorsList, "SupplierId", "SupplierName");
        }

        // GET: ProductController/Edit/5
        public async Task<ActionResult> Edit(long id=0)
        {
            var unit = await _productService.GetProductByIdAsync(id);
            
            if (unit == null || unit.ProductList == null)
            {
                return NotFound();
            }

            var model = new ProductViewModel
            {
                ProductId = unit.ProductList.ProductId,
                ProductName = unit.ProductList.ProductName,
                ProductCode = unit.ProductList.ProductCode,
                ProductDescription = unit.ProductList.ProductDescription,
                Location = unit.ProductList.Location,
                CategoryId = unit.ProductList.CategoryIdFk,
                VendorId = unit.ProductList.SupplierIdFk,
                LabelId = unit.ProductList.LabelIdFk,
                MUTId = unit.ProductList.MeasuringUnitTypeIdFk,
                IsEnabled = Convert.ToBoolean(unit.ProductList.IsEnabled),
                Price = unit.ProductList?.UnitPrice ?? 0,
                CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync(),
                LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync(),
                MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync(),
                VendorsList = await _vendorService.GetAllEnabledVendors()
            };

            // Add product ranges if they exist
            if (unit.productRanges != null && unit.productRanges.Count > 0)
            {
                model.productRanges.AddRange(unit.productRanges);
            }

            ViewBag.Categories = new SelectList(model.CategoryNameList, "CategoryId", "CategoryName", unit.ProductList.CategoryIdFk);
            ViewBag.Labels = new SelectList(model.LabelNameList, "LabelId", "LabelName", unit.ProductList.LabelIdFk);
            ViewBag.MeasuringUnitTypes = new SelectList(model.MeasuringUnitTypeNameList, "MeasuringUnitTypeId", "MeasuringUnitTypeName", unit.ProductList.MeasuringUnitTypeIdFk);
            ViewBag.Vendors = new SelectList(model.VendorsList, "SupplierId", "SupplierName", unit.ProductList.SupplierIdFk);
            
            return View(model);
        }

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProductViewModel model)
        {
            try
            {
                // Remove ProductRange validation errors from ModelState since we handle them separately
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("ProductRange"))
                    {
                        keysToRemove.Add(key);
                    }
                    if (key.StartsWith("ProductList"))
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                // Validate only the main product properties
                if (ModelState.IsValid)
                {
                    // Check if product code already exists (excluding current product)
                    if (!string.IsNullOrEmpty(model.ProductCode))
                    {
                        var codeExists = await _productService.ProductCodeExistsAsync(model.ProductCode, model.ProductId);
                        if (codeExists)
                        {
                            
                            TempData["WarningMessage"] = "Product code already exists. Please use a different code.";
                        }
                    }

                    // Validate product ranges if any exist
                    if (model.productRanges != null && model.productRanges.Any())
                    {
                        for (int i = 0; i < model.productRanges.Count; i++)
                        {
                            var range = model.productRanges[i];
                            if (range.MeasuringUnitIdFk == 0 || range.UnitPrice == 0)
                            {
                               
                                TempData["WarningMessage"] = $"productRanges[{i}]"+ "All size fields are required and must be greater than 0.";
                            }
                        }
                    }

                    // If there are validation errors, reload the view
                    if (!ModelState.IsValid)
                    {
                        await ReloadViewDataAsync(model);
                        return View(model);
                    }

                    var product = new Product
                    {
                        ProductId = model.ProductId,
                        ProductName = model.ProductName,
                        ProductCode = string.IsNullOrWhiteSpace(model.ProductCode) ? null : model.ProductCode,
                        CategoryIdFk = model.CategoryId ?? 0,
                        LabelIdFk = model.LabelId ?? 0,
                        MeasuringUnitTypeIdFk = model.MUTId ?? 0,
                        SupplierIdFk = model.VendorId ?? 0,
                        UnitPrice = model.Price ?? 0,
                        ProductDescription = string.IsNullOrWhiteSpace(model.ProductDescription) ? null : model.ProductDescription,
                        Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location,
                        IsEnabled = model.IsEnabled ? (byte)1 : (byte)0,
                        SizeIdFk = 0 // Default value as per your SP
                    };
                    
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    product.ModifiedBy = userId;
                    product.ModifiedDate = DateTimeHelper.Now;
                    
                    var result = await _productService.UpdateProductAsync(product);
                    if (result > 0)
                    {
                        // Update product ranges
                        if (model.productRanges != null && model.productRanges.Any())
                        {
                            // First, delete existing product ranges for this product
                            await _productService.DeleteProductRangesByProductIdAsync(model.ProductId);
                            
                            // Then add the updated product ranges
                            foreach (var range in model.productRanges)
                            {
                                range.ProductIdFk = model.ProductId;
                                await _productService.CreateProductRange(range);
                            }
                        }
                        
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        await ReloadViewDataAsync(model);
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                await ReloadViewDataAsync(model);
                return View(model);
            }
            
            // Reload the view with necessary data if validation fails
            await ReloadViewDataAsync(model);
            return View(model);
        }

        // GET: ProductController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var unit = await _productService.GetProductByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return View(unit);
        }

        // POST: ProductController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {

                var res = await _productService.DeleteProductAsync(id);
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

        public async Task<ActionResult> AddSizePartial(long? measuringUnitTypeId)
        {
            ViewBag.MeasuringUnitTypeId = measuringUnitTypeId;
            return PartialView("_AddSize", new ProductRange());
        }

        // AJAX endpoint to get measuring units by type for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetMeasuringUnitsByType(long? measuringUnitTypeId)
        {
            try
            {
                _logger.LogInformation("GetMeasuringUnitsByType called with measuringUnitTypeId: {MeasuringUnitTypeId}", measuringUnitTypeId);
                
                if (!measuringUnitTypeId.HasValue || measuringUnitTypeId.Value == 0)
                {
                    _logger.LogWarning("No valid measuringUnitTypeId provided: {MeasuringUnitTypeId}", measuringUnitTypeId);
                    return Json(new List<object>());
                }

                var measuringUnits = await _adminMeasuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(measuringUnitTypeId);
                _logger.LogInformation("Found {Count} measuring units for type {MeasuringUnitTypeId}", measuringUnits.Count, measuringUnitTypeId);
                
                // Filter only enabled measuring units
                var enabledMeasuringUnits = measuringUnits.Where(mu => mu.IsEnabled).ToList();
                _logger.LogInformation("Found {Count} enabled measuring units for type {MeasuringUnitTypeId}", enabledMeasuringUnits.Count, measuringUnitTypeId);
                
                var result = enabledMeasuringUnits.Select(mu => new
                {
                    value = mu.MeasuringUnitId.ToString(),
                    text = mu.MeasuringUnitName
                }).ToList();
                
                _logger.LogInformation("Returning {Count} enabled measuring units: {Result}", result.Count, string.Join(", ", result.Select(r => $"{r.text}({r.value})")));
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting measuring units by type for Kendo combobox");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckProductCode(string productCode, long? excludeProductId = null)
        {
            if (string.IsNullOrEmpty(productCode))
            {
                return Json(false);
            }

            var exists = await _productService.ProductCodeExistsAsync(productCode, excludeProductId);
            return Json(exists);
        }

        // AJAX endpoint to get categories for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllEnabledCategoriesAsync();
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

        // AJAX endpoint to get labels for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetLabels()
        {
            try
            {
                var labels = await _adminLablesService.GetAllEnabledAdminLablesAsync();
                var result = labels.Select(l => new
                {
                    value = l.LabelId.ToString(),
                    text = l.LabelName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting labels for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get measuring unit types for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetMeasuringUnitTypes()
        {
            try
            {
                var measuringUnitTypes = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync();
                var result = measuringUnitTypes.Select(m => new
                {
                    value = m.MeasuringUnitTypeId.ToString(),
                    text = m.MeasuringUnitTypeName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting measuring unit types for Kendo combobox");
                return Json(new List<object>());
            }
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
                        text = p.ProductName,
                        code = p.ProductCode
                    }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for Kendo combobox");
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

    }
}
