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
                model.productFilters.ProductName = HttpContext.Request.Query["searchpName"].ToString();
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
                            if (range.MeasuringUnitIdFk == 0 || range.RangeFrom == 0 || range.RangeTo == 0 || range.UnitPrice == 0)
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
                        ProductCode = model.ProductCode,
                        CategoryIdFk = model.CategoryId ?? 0,
                        LabelIdFk = model.LabelId ?? 0,
                        MeasuringUnitTypeIdFk = model.MUTId,
                        SupplierIdFk = model.VendorId,
                        UnitPrice = model.Price,
                        ProductDescription = model.ProductDescription,
                        IsEnabled = model.IsEnabled ? (byte)1 : (byte)0,
                        SizeIdFk = 0 // Default value as per your SP
                    };
                    
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    product.CreatedBy = userId;
                    product.CreatedDate = DateTime.Now;
                    product.ModifiedBy = userId;
                    product.ModifiedDate = DateTime.Now;
                    
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
            var model = new ProductViewModel();
            var unit = await _productService.GetProductByIdAsync(id);
            model.ProductId = unit.ProductList.ProductId;
            model.ProductName = unit.ProductList.ProductName;
            model.ProductCode=unit.ProductList.ProductCode;
            model.ProductDescription = unit.ProductList.ProductDescription;
            model.CategoryId = unit.ProductList.CategoryIdFk;
            model.VendorId = unit.ProductList.SupplierIdFk;
            model.LabelId = unit.ProductList.LabelIdFk;
            model.IsEnabled = Convert.ToBoolean(unit.ProductList.IsEnabled);
            model.Price = unit.ProductRange.UnitPrice;
            var model1 = new ProductViewModel
            {
                CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync(),
                LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync(),
                MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync(),
                VendorsList = await _vendorService.GetAllEnabledVendors()
            };
            ViewBag.Categories = new SelectList(model1.CategoryNameList, "CategoryId", "CategoryName", unit.ProductList.CategoryIdFk);
            ViewBag.Labels = new SelectList(model1.LabelNameList, "LabelId", "LabelName", unit.ProductList.LabelIdFk);
            ViewBag.MeasuringUnitTypes = new SelectList(model1.MeasuringUnitTypeNameList, "MeasuringUnitTypeId", "MeasuringUnitTypeName",unit.ProductList.MeasuringUnitTypeIdFk);
            ViewBag.Vendors = new SelectList(model1.VendorsList, "SupplierId", "SupplierName", unit.ProductList.SupplierIdFk);
            model.productRanges.Add(unit.ProductRange);
            if (unit == null)
            {
                return NotFound();
            }
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
                            if (range.MeasuringUnitIdFk == 0 || range.RangeFrom == 0 || range.RangeTo == 0 || range.UnitPrice == 0)
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
                        ProductCode = model.ProductCode,
                        CategoryIdFk = model.CategoryId ?? 0,
                        LabelIdFk = model.LabelId ?? 0,
                        MeasuringUnitTypeIdFk = model.MUTId,
                        SupplierIdFk = model.VendorId,
                        UnitPrice = model.Price,
                        ProductDescription = model.ProductDescription,
                        IsEnabled = model.IsEnabled ? (byte)1 : (byte)0,
                        SizeIdFk = 0 // Default value as per your SP
                    };
                    
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    product.ModifiedBy = userId;
                    product.ModifiedDate = DateTime.Now;
                    
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
            var model = new ProductViewModel
            {
                MeasuringUnitNameList = await _adminMeasuringUnitService.GetAllMeasuringUnitsByMUTIdAsync(measuringUnitTypeId)
            };
            ViewBag.MeasuringUnits = new SelectList(model.MeasuringUnitNameList, "MeasuringUnitId", "MeasuringUnitName");
            return PartialView("_AddSize", new ProductRange());
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
    }
}
