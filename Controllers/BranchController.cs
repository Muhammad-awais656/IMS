using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace IMS.Controllers
{
    public class BranchController : Controller
    {
        private readonly IBranchService _branchService;
        private const int DefaultPageSize = 10;
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        /// <summary>
        /// Returns active branches as JSON for Kendo MultiSelect. Format: [{ Id, Name }].
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveBranchesJson()
        {
            var branches = await _branchService.GetAllActiveBranchesAsync();
            var list = branches.Select(b => new { Id = b.BranchId, Name = b.BranchName }).ToList();
            return Json(list);
        }

        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? search = null)
        {
            var viewModel = new BranchViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (string.IsNullOrEmpty(search))
                    search = HttpContext.Request.Query["search"].ToString();

                viewModel = await _branchService.GetAllBranchesAsync(pageNumber, currentPageSize, string.IsNullOrWhiteSpace(search) ? null : search);
                ViewData["search"] = search;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(viewModel);
        }

        public ActionResult Create()
        {
            return View(new Branch());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Branch branch)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                    {
                        branch.CreatedBy = userId;
                        branch.ModifiedBy = userId;
                    }
                    branch.CreatedDate = DateTimeHelper.Now;
                    branch.ModifiedDate = DateTimeHelper.Now;

                    var result = await _branchService.CreateBranchAsync(branch);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                    return View(branch);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(branch);
            }
            return View(branch);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            if (branch == null)
                return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Branch branch)
        {
            if (id != branch.BranchId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                        branch.ModifiedBy = userId;
                    branch.ModifiedDate = DateTimeHelper.Now;

                    var response = await _branchService.UpdateBranchAsync(branch);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                    return View(branch);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                    return View(branch);
                }
            }
            return View(branch);
        }

        public async Task<ActionResult> Delete(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            if (branch == null)
                return NotFound();
            return View(branch);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var res = await _branchService.DeleteBranchAsync(id);
                if (res != 0)
                {
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
