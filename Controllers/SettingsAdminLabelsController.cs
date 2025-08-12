using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;

namespace IMS.Controllers
{
    public class SettingsAdminLabelsController : Controller
    {

        private readonly IAdminLablesService _adminLabelsService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes

        public SettingsAdminLabelsController(IAdminLablesService adminLablesService)
        {
            _adminLabelsService = adminLablesService;
        }
        // GET: SettingsAdminLabelsController
        [HttpGet]
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null,string? AdminlabelName = null)
        {
            var viewModel = new AdminLablesViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (AdminlabelName == null)
                {
                    AdminlabelName = HttpContext.Request.Query.FirstOrDefault().Value;
                }
                viewModel = await _adminLabelsService.GetAllAdminLablesAsync(pageNumber, currentPageSize, AdminlabelName);


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: SettingsAdminLabelsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SettingsAdminLabelsController/Create
        public ActionResult Create()
        {

            return View();
        }

        // POST: SettingsAdminLabelsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminLabel adminLabel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminLabel.CreatedBy = userId;
                    adminLabel.CreatedDate = DateTime.Now;
                    adminLabel.ModifiedBy = userId;
                    adminLabel.ModifiedDate = DateTime.Now;
                    var result = await _adminLabelsService.CreateAdminLablesAsync(adminLabel);
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
                return View(adminLabel);
            }
            return View(adminLabel);
        }

        // GET: SettingsAdminLabelsController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var user = await _adminLabelsService.GetAdminLablesByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: SettingsAdminLabelsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, AdminLabel AdminLabels)
        {
            if (id != AdminLabels.LabelId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    AdminLabels.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    AdminLabels.ModifiedBy = userId;
                    var response = await _adminLabelsService.UpdateAdminLablesAsync(AdminLabels);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(AdminLabels);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(AdminLabels);
        }

        // GET: SettingsAdminLabelsController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var AdminLabels = new AdminLabel();
            try
            {
                AdminLabels = await _adminLabelsService.GetAdminLablesByIdAsync(id);
                if (AdminLabels == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(AdminLabels);
        }

        // POST: SettingsAdminLabelsController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _adminLabelsService.DeleteAdminLablesAsync(id);
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
