using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class AdminMeasuringUnitTypesController : Controller
    {
        private readonly IAdminMeasuringUnitTypesService _adminMeasuringUnitTypesService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        public AdminMeasuringUnitTypesController(IAdminMeasuringUnitTypesService adminMeasuringUnitTypesService)
        {
            _adminMeasuringUnitTypesService = adminMeasuringUnitTypesService;
        }
        // GET: AdminMeasuringUnitTypesController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? UnitTypeName = null)
        {
            var viewModel = new AdminMeasuringUnitTypesViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (UnitTypeName == null)
                {
                    UnitTypeName = HttpContext.Request.Query["searchUsername"].ToString();
                }
                viewModel = await _adminMeasuringUnitTypesService.GetAllAdminMeasuringUnitTypesAsync(pageNumber, currentPageSize, UnitTypeName);


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: AdminMeasuringUnitTypesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminMeasuringUnitTypesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminMeasuringUnitTypesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async  Task<ActionResult> Create(AdminMeasuringUnitType adminMeasuringUnitType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminMeasuringUnitType.CreatedBy = userId;
                    adminMeasuringUnitType.CreatedDate = DateTimeHelper.Now;

                    var result = await _adminMeasuringUnitTypesService.CreateAdminMeasuringUnitTypesAsync(adminMeasuringUnitType);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(adminMeasuringUnitType);
            }
            return View(adminMeasuringUnitType);
        }

        // GET: AdminMeasuringUnitTypesController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var unit = await _adminMeasuringUnitTypesService.GetAdminMeasuringUnitTypesByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return View(unit);
        }

        // POST: AdminMeasuringUnitTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, AdminMeasuringUnitType adminMeasuringUnitType)
        {
            if (id != adminMeasuringUnitType.MeasuringUnitTypeId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    adminMeasuringUnitType.ModifiedDate = DateTimeHelper.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminMeasuringUnitType.ModifiedBy = userId;
                    var response = await _adminMeasuringUnitTypesService.UpdateAdminMeasuringUnitTypesAsync(adminMeasuringUnitType);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(adminMeasuringUnitType);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(adminMeasuringUnitType);
        }

        // GET: AdminMeasuringUnitTypesController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var measuringUnitType = new AdminMeasuringUnitType();
            try
            {
                measuringUnitType = await _adminMeasuringUnitTypesService.GetAdminMeasuringUnitTypesByIdAsync(id);
                if (measuringUnitType == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(measuringUnitType);
        }

        // POST: AdminMeasuringUnitTypesController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _adminMeasuringUnitTypesService.DeleteAdminMeasuringUnitTypesAsync(id);
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
