using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

        public ProductController(IProductService productService,ILogger<ProductController> logger,ICategoryService categoryService
            , IAdminMeasuringUnitTypesService adminMeasuringUnitTypesService, IAdminLablesService adminLablesService)
        {
                _logger = logger;
                _productService = productService;
                _categoryService = categoryService;
            _adminMeasuringUnitTypesService = adminMeasuringUnitTypesService;
            _adminLablesService = adminLablesService;
        }
        // GET: ProductController
        //public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, ProductFilters? productFilters = null)
        //{

        //    var viewModelList = new ProductViewModel();

        //    try
        //    {
        //        int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
        //        if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
        //        {
        //            currentPageSize = pageSize.Value;
        //            HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
        //        }
        //        if (productFilters == null)
        //        {
        //            productFilters.ProductName = HttpContext.Request.Query["searchUsername"].ToString();
        //        }
        //       var adminCategories = await _categoryService.GetAllEnabledCategoriesAsync();

        //        ViewBag.Categories= new SelectList(adminCategories, "CategoryId", "CategoryName");

        //        viewModelList = await _productService.GetAllProductAsync(pageNumber, currentPageSize, productFilters);



        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = ex.Message;

        //    }
        //    return View(viewModelList);

        //    }
        public async Task<ActionResult> Index()
        {
            var model = new ProductViewModel
            {
                CategoryNameList = await _categoryService.GetAllEnabledCategoriesAsync(),

                LabelNameList = await _adminLablesService.GetAllEnabledAdminLablesAsync(),
                MeasuringUnitTypeNameList = await _adminMeasuringUnitTypesService.GetAllEnabledMeasuringUnitTypesAsync()
            };
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> GetFilteredProducts(int pageNumber,int? pageSize=null ,ProductFilters filters=null)
       {
            var viewModel = new ProductViewModel();

            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                viewModel = await _productService.GetAllProductAsync(pageNumber, pageSize, filters);
                
                




            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            //PartialView("_paination", viewModel);
             return PartialView("_ProductList", viewModel);
            
            //return View(viewModel);
            //return View("Index", viewModel);
        }


        // GET: ProductController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProductController/Create
        public ActionResult Create()
        {
            return View();
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
    }
}
