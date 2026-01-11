using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class UnitConversionController : Controller
    {
        private readonly ILogger<UnitConversionController> _logger;
        private readonly IUnitConversionService _unitConversionService;
        private readonly IAdminMeasuringUnitService _measuringUnitService;

        public UnitConversionController(
            IUnitConversionService unitConversionService,
            IAdminMeasuringUnitService measuringUnitService,
            ILogger<UnitConversionController> logger)
        {
            _unitConversionService = unitConversionService;
            _measuringUnitService = measuringUnitService;
            _logger = logger;
        }

        // GET: UnitConversionController
        public async Task<ActionResult> Index()
        {
            try
            {
                var conversions = await _unitConversionService.GetAllUnitConversionsAsync();
                
                // Get unit names for display - use the same method as the Index page to get ALL units
                var viewModel = await _measuringUnitService.GetAllAdminMeasuringUnitAsync(1, 10000, null);
                var allUnits = viewModel.AdminMeasuringUnits ?? new List<AdminMeasuringUnit>();
                
                // Create dictionary for quick lookup
                var unitDict = allUnits.ToDictionary(u => u.MeasuringUnitId, u => u.MeasuringUnitName);
                
                _logger.LogInformation("Loaded {Count} measuring units for display", allUnits.Count);
                _logger.LogInformation("Processing {Count} unit conversions", conversions.Count);
                
                var displayViewModel = conversions.Select(c => new
                {
                    c.UnitConversionId,
                    c.FromUnitId,
                    c.ToUnitId,
                    FromUnitName = unitDict.ContainsKey(c.FromUnitId) ? unitDict[c.FromUnitId] : $"Unknown (ID: {c.FromUnitId})",
                    ToUnitName = unitDict.ContainsKey(c.ToUnitId) ? unitDict[c.ToUnitId] : $"Unknown (ID: {c.ToUnitId})",
                    c.ConversionFactor,
                    c.Description,
                    c.IsEnabled
                }).ToList();

                // Log any missing units for debugging
                var missingFromUnits = conversions.Where(c => !unitDict.ContainsKey(c.FromUnitId)).ToList();
                var missingToUnits = conversions.Where(c => !unitDict.ContainsKey(c.ToUnitId)).ToList();
                
                if (missingFromUnits.Any())
                {
                    _logger.LogWarning("Found {Count} conversions with missing FromUnitIds: {Ids}", 
                        missingFromUnits.Count, 
                        string.Join(", ", missingFromUnits.Select(c => c.FromUnitId)));
                }
                if (missingToUnits.Any())
                {
                    _logger.LogWarning("Found {Count} conversions with missing ToUnitIds: {Ids}", 
                        missingToUnits.Count, 
                        string.Join(", ", missingToUnits.Select(c => c.ToUnitId)));
                }

                ViewBag.Conversions = displayViewModel;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit conversions: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
        }

        // GET: UnitConversionController/Create
        public async Task<ActionResult> Create()
        {
            try
            {
                var allUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(null);
                ViewBag.FromUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName");
                ViewBag.ToUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UnitConversionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UnitConversion unitConversion)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    unitConversion.CreatedBy = userId;
                    unitConversion.CreatedDate = DateTime.Now;
                    unitConversion.ModifiedBy = userId;
                    unitConversion.ModifiedDate = DateTime.Now;

                    var result = await _unitConversionService.CreateUnitConversionAsync(unitConversion);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit conversion");
                TempData["ErrorMessage"] = ex.Message;
            }

            // Reload dropdowns on error
            try
            {
                var allUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(null);
                ViewBag.FromUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", unitConversion.FromUnitId);
                ViewBag.ToUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", unitConversion.ToUnitId);
            }
            catch { }

            return View(unitConversion);
        }

        // GET: UnitConversionController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid unit conversion ID.";
                    return RedirectToAction(nameof(Index));
                }

                var conversion = await _unitConversionService.GetUnitConversionByIdAsync(id);
                if (conversion == null)
                {
                    TempData["ErrorMessage"] = $"Unit conversion with ID {id} was not found.";
                    return RedirectToAction(nameof(Index));
                }

                var allUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(null);
                ViewBag.FromUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", conversion.FromUnitId);
                ViewBag.ToUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", conversion.ToUnitId);

                return View(conversion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit conversion {Id} for editing", id);
                TempData["ErrorMessage"] = "An error occurred while loading the unit conversion for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UnitConversionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, UnitConversion unitConversion)
        {
            if (id != unitConversion.UnitConversionId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    unitConversion.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    unitConversion.ModifiedBy = userId;

                    var response = await _unitConversionService.UpdateUnitConversionAsync(unitConversion);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating unit conversion {Id}", id);
                    TempData["ErrorMessage"] = ex.Message;
                }
            }

            // Reload dropdowns on error
            try
            {
                var allUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(null);
                ViewBag.FromUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", unitConversion.FromUnitId);
                ViewBag.ToUnits = new SelectList(allUnits, "MeasuringUnitId", "MeasuringUnitName", unitConversion.ToUnitId);
            }
            catch { }

            return View(unitConversion);
        }

        // GET: UnitConversionController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid unit conversion ID.";
                    return RedirectToAction(nameof(Index));
                }

                var conversion = await _unitConversionService.GetUnitConversionByIdAsync(id);
                if (conversion == null)
                {
                    TempData["ErrorMessage"] = $"Unit conversion with ID {id} was not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get unit names for display
                var allUnits = await _measuringUnitService.GetAllMeasuringUnitsByMUTIdAsync(null);
                var fromUnit = allUnits.FirstOrDefault(u => u.MeasuringUnitId == conversion.FromUnitId);
                var toUnit = allUnits.FirstOrDefault(u => u.MeasuringUnitId == conversion.ToUnitId);
                
                ViewBag.FromUnitName = fromUnit?.MeasuringUnitName ?? "Unknown";
                ViewBag.ToUnitName = toUnit?.MeasuringUnitName ?? "Unknown";

                return View(conversion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit conversion {Id} for deletion", id);
                TempData["ErrorMessage"] = "An error occurred while loading the unit conversion for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UnitConversionController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _unitConversionService.DeleteUnitConversionAsync(id);
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
                _logger.LogError(ex, "Error deleting unit conversion {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX endpoint to get measuring units for Kendo UI ComboBox
        [HttpGet]
        public async Task<IActionResult> GetMeasuringUnits()
        {
            try
            {
                // Use the same method that the Index page uses to get all units
                // Get first page with large page size to get all enabled units
                var viewModel = await _measuringUnitService.GetAllAdminMeasuringUnitAsync(1, 10000, null);
                var allUnits = viewModel.AdminMeasuringUnits ?? new List<AdminMeasuringUnit>();
                
                // Filter to only enabled units and sort by name
                var enabledMeasuringUnits = allUnits.Where(mu => mu.IsEnabled)
                    .OrderBy(mu => mu.MeasuringUnitName)
                    .ToList();
                
                _logger.LogInformation("Found {Count} enabled measuring units out of {TotalCount} total units", 
                    enabledMeasuringUnits.Count, allUnits.Count);
                
                var result = enabledMeasuringUnits.Select(mu => new
                {
                    value = mu.MeasuringUnitId.ToString(),
                    text = mu.MeasuringUnitName,
                    measuringUnitAbbreviation = mu.MeasuringUnitAbbreviation ?? ""
                }).ToList();
                
                _logger.LogInformation("Returning {Count} measuring units for dropdown", result.Count);
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting measuring units for Kendo combobox: {Message}", ex.Message);
                return Json(new List<object>());
            }
        }
    }
}



