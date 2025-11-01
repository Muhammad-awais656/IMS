using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IMS.Controllers
{
    public class MeasuringUnitController : Controller
    {
        private readonly ILogger<MeasuringUnitController> _logger;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        private readonly IAdminMeasuringUnitService _measuringUnitService;
        public MeasuringUnitController(IAdminMeasuringUnitService adminMeasuringUnitService,ILogger<MeasuringUnitController> logger) { 
        
            _measuringUnitService = adminMeasuringUnitService;
            _logger = logger;
        }
        // GET: MeasuringUnitController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? MeasuringUnitName = null)
        {
            var viewModel = new MeasuringUnitViewModel();
            
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (MeasuringUnitName == null)
                {
                    MeasuringUnitName = HttpContext.Request.Query["searchUsername"].ToString();
                }
                viewModel = await _measuringUnitService.GetAllAdminMeasuringUnitAsync(pageNumber, currentPageSize, MeasuringUnitName);
                var units = viewModel.AdminMeasuringUnits ?? new List<AdminMeasuringUnit>();
                var types = (viewModel.AdminMeasuringUnitTypes ?? new List<AdminMeasuringUnitType>())
    .GroupBy(t => t.MeasuringUnitTypeId)
    .Select(g => g.First())
    .ToList();
                var model = new MeasuringUnitViewModel
                {
                    Items = (from mu in units
                              join mut in types
                                 on mu.MeasuringUnitTypeIdFk equals mut.MeasuringUnitTypeId
                             select new MeasuringUnitListItemViewModel
                             {
                                 MeasuringUnitId = mu.MeasuringUnitId,
                                 MeasuringUnitName = mu.MeasuringUnitName,
                                 MeasuringUnitDescription = mu.MeasuringUnitDescription,
                                 MeasuringUnitTypeName = mut.MeasuringUnitTypeName,
                                 IsEnabled = mu.IsEnabled
                             }).ToList()
                };
                viewModel.Items = model.Items;


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: MeasuringUnitController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: MeasuringUnitController/Create
        public async Task<ActionResult> Create()
        {
            var measuringViewModel = new List<MeasuringUnitViewModel>();
            var res = await _measuringUnitService.AdminMeasuringUnitTypeCacheAsync();
            measuringViewModel.Add(new MeasuringUnitViewModel
            {
                AdminMeasuringUnitTypes = res
            });
            ViewBag.MeasuringUnitTypes= new SelectList(res, "MeasuringUnitTypeId", "MeasuringUnitTypeName");
            return View();
        }

        // POST: MeasuringUnitController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminMeasuringUnit adminMeasuringUnit)
        {
            try
            {
                
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminMeasuringUnit.CreatedBy = userId;
                    adminMeasuringUnit.CreatedDate = DateTime.Now;
                    
                    var result = await _measuringUnitService.CreateAdminMeasuringUnitAsync(adminMeasuringUnit);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(nameof(Create));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(adminMeasuringUnit);
            }
            return View(adminMeasuringUnit);
        }

        // GET: MeasuringUnitController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                // Validate ID
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid measuring unit ID.";
                    return RedirectToAction(nameof(Index));
                }

                var measuringViewModel = new List<MeasuringUnitViewModel>();
                var res = await _measuringUnitService.AdminMeasuringUnitTypeCacheAsync();
                measuringViewModel.Add(new MeasuringUnitViewModel
                {
                    AdminMeasuringUnitTypes = res
                });
                ViewBag.MeasuringUnitTypes = new SelectList(res, "MeasuringUnitTypeId", "MeasuringUnitTypeName");
                
                var unit = await _measuringUnitService.GetAdminMeasuringUnitByIdAsync(id);
                if (unit == null)
                {
                    TempData["ErrorMessage"] = $"Measuring unit with ID {id} was not found. It may have been deleted.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading measuring unit {Id} for editing", id);
                TempData["ErrorMessage"] = "An error occurred while loading the measuring unit for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MeasuringUnitController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id,AdminMeasuringUnit adminMeasuringUnit)
        {
            if (id != adminMeasuringUnit.MeasuringUnitId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    adminMeasuringUnit.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminMeasuringUnit.ModifiedBy = userId;
                    var response = await _measuringUnitService.UpdateAdminMeasuringUnitAsync(adminMeasuringUnit);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(adminMeasuringUnit);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(adminMeasuringUnit);
        }

        // GET: MeasuringUnitController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                // Validate ID
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid measuring unit ID.";
                    return RedirectToAction(nameof(Index));
                }

                var unit = await _measuringUnitService.GetAdminMeasuringUnitByIdAsync(id);
                if (unit == null)
                {
                    TempData["ErrorMessage"] = $"Measuring unit with ID {id} was not found. It may have been deleted.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading measuring unit {Id} for deletion", id);
                TempData["ErrorMessage"] = "An error occurred while loading the measuring unit for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MeasuringUnitController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _measuringUnitService.DeleteAdminMeasuringUnitAsync(id);
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
