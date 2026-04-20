using IMS.Authorization;
using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Enums;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    [CustomAuthorize(privilege: EnumPrivilegesName.VIEW_ROLES)]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;
        private const int DefaultPageSize = 10;
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? search = null)
        {
            var viewModel = new RoleViewModel();
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

                viewModel = await _roleService.GetAllRolesAsync(pageNumber, currentPageSize, string.IsNullOrWhiteSpace(search) ? null : search);
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
            return View(new AdminRole());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminRole role)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                    {
                        role.CreatedBy = userId;
                        role.ModifiedBy = userId;
                    }
                    role.CreatedDate = DateTimeHelper.Now;
                    role.ModifiedDate = DateTimeHelper.Now;

                    var result = await _roleService.CreateRoleAsync(role);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                    return View(role);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(role);
            }
            return View(role);
        }

        public async Task<ActionResult> Edit(long id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
                return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, AdminRole role)
        {
            if (id != role.RoleId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                        role.ModifiedBy = userId;
                    role.ModifiedDate = DateTimeHelper.Now;

                    var response = await _roleService.UpdateRoleAsync(role);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                    return View(role);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                    return View(role);
                }
            }
            return View(role);
        }

        public async Task<ActionResult> Delete(long id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
                return NotFound();
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var res = await _roleService.DeleteRoleAsync(id);
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
