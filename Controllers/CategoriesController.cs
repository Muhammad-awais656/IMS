using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        // GET: CategoriesController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? CategoryName = null)
        {
            var viewModel = new CategoriesViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (CategoryName == null)
                {
                    CategoryName = HttpContext.Request.Query["searchUsername"].ToString();
                }
                viewModel = await _categoryService.GetAllAdminCategoryAsync(pageNumber, currentPageSize, CategoryName);


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: CategoriesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CategoriesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CategoriesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminCategory adminCategory)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminCategory.CreatedBy = userId;
                    adminCategory.CreatedDate = DateTime.Now;

                    var result = await _categoryService.CreateAdminCategoryAsync(adminCategory);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(Index);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(adminCategory);
            }
            return View(adminCategory);
        }

        // GET: CategoriesController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var user = await _categoryService.GetAdminCategoryByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: CategoriesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id , AdminCategory adminCategory)
        {
            if (id != adminCategory.CategoryId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    adminCategory.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminCategory.ModifiedBy = userId;
                    var response = await _categoryService.UpdateAdminCategoryAsync(adminCategory);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(adminCategory);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(adminCategory);
        }

        // GET: CategoriesController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var adminCategory = new AdminCategory();
            try
            {
                adminCategory = await _categoryService.GetAdminCategoryByIdAsync(id);
                if (adminCategory == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(adminCategory);
        }

        // POST: CategoriesController/Delete/5
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _categoryService.DeleteAdminCategoryAsync(id);
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
