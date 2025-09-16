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
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProductController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
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

        public async Task<ActionResult> AddSizePartial(long? MUTId)
        {
            var model = new ProductViewModel
            {

                MeasuringUnitNameList = await _adminMeasuringUnitService.GetAllMeasuringUnitsByMUTIdAsync(MUTId)

            };
            ViewBag.MeasuringUnits = new SelectList(model.MeasuringUnitNameList, "MeasuringUnitId", "MeasuringUnitName");
            //await GetMeasuringUnits(MUTId);
            return PartialView("_AddSize", new ProductRange());
        }

        [HttpPost]
        public IActionResult SaveSize(ProductRange model)
        {
            if (ModelState.IsValid)
            {
                // Save to DB
                //_context.ProductRanges.Add(model);
                //_context.SaveChanges();

                // return the new row as partial
                return PartialView("_SizeRow", model);
            }
            return BadRequest();
        }
        //[HttpGet]
        //public async Task<ActionResult> GetMeasuringUnits(int MUTId)
        //{
            
        //}

    }
}
