using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Enums;
using IMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Linq;

namespace IMS.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        
        //public string domain = string.Empty;
        private readonly IUserService _userService;
        private readonly IBranchService _branchService;
        private readonly IRoleService _roleService;
        private readonly IIdentityUserSyncService _identityUserSyncService;
        private readonly ILogger<UserController> _logger;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes

        public UserController(IUserService userService, IBranchService branchService, IRoleService roleService, IIdentityUserSyncService identityUserSyncService, ILogger<UserController> logger)
        {
            _userService = userService;
            _branchService = branchService;
            _roleService = roleService;
            _identityUserSyncService = identityUserSyncService;
            _logger = logger;
        }


        // GET: /Users/Details/5
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(int pageNumber=1, int? pageSize = null,string? UserNameSearch=null)
        {
            PagedUsersViewModel viewModel = new PagedUsersViewModel();
            _logger.LogInformation("Visited Home/Index at {Time}", DateTimeHelper.Now);

            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (UserNameSearch == null)
                {
                    UserNameSearch = HttpContext.Request.Query["searchUsername"].ToString();
                }
                viewModel = await _userService.GetPagedUsersAsync(pageNumber, currentPageSize, UserNameSearch);
                //return View(viewModel);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =ex.Message;
                _logger.LogError(ex.Message, "Error occurred while fetching users.");
            }
            
            return View(viewModel);
        }
        // GET: UserController/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            return View(user);
        }

        // GET: UserController/Create
        public async Task<IActionResult> Create()
        {
            var branches = await _branchService.GetAllActiveBranchesAsync();
            ViewBag.Branches = branches;
            ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string? BranchIds)
        {
            try
            {
                var branchIds = ParseBranchIds(BranchIds);
                if (branchIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "Please select at least one branch.";
                    ViewBag.Branches = await _branchService.GetAllActiveBranchesAsync();
                    ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
                    return View(user);
                }
                if (ModelState.IsValid)
                {
                    var result = await _userService.CreateUserAsync(user, branchIds);
                    if (result)
                    {
                        await _identityUserSyncService.SyncFromLegacyUserAsync(user, branchIds);
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(GetAllUsers));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            ViewBag.Branches = await _branchService.GetAllActiveBranchesAsync();
            ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
            return View(user);
        }

        // GET: UserController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            var branches = await _branchService.GetAllActiveBranchesAsync();
            ViewBag.Branches = branches;
            ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
            ViewBag.UserBranchIds = await _userService.GetUserBranchIdsAsync(id);
            return View(user);
        }

        // POST: UserController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, User user, string? BranchIds)
        {
            if (id != user.UserId)
                return BadRequest();

            var branchIds = ParseBranchIds(BranchIds);
            if (branchIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one branch.";
                ViewBag.Branches = await _branchService.GetAllActiveBranchesAsync();
                ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
                ViewBag.UserBranchIds = await _userService.GetUserBranchIdsAsync(id);
                return View(user);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.UserPassword))
                    {
                        var existingUser = await _userService.GetUserByIdAsync(id);
                        if (existingUser != null)
                            user.UserPassword = existingUser.UserPassword;
                    }
                    var response = await _userService.UpdateUserAsync(user, branchIds);
                    if (response != 0)
                    {
                        await _identityUserSyncService.SyncFromLegacyUserAsync(user, branchIds);
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(GetAllUsers));
                    }
                    TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _userService.GetUserByIdAsync(id) == null)
                        return NotFound();
                    throw;
                }
            }
            ViewBag.Branches = await _branchService.GetAllActiveBranchesAsync();
            ViewBag.Roles = await _roleService.GetActiveRolesForDropdownAsync();
            ViewBag.UserBranchIds = branchIds;
            return View(user);
        }

        private static List<int> ParseBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds)) return new List<int>();
            return branchIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : 0)
                .Where(n => n > 0)
                .ToList();
        }

        // GET: UserController/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
               var res= await _userService.DeleteUserAsync(id);
                if (res!=0)
                {
                    await _identityUserSyncService.DeleteByLegacyUserIdAsync(id);
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(GetAllUsers));
                }
                else
                {
                    TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                    return RedirectToAction(nameof(GetAllUsers));
                }
                
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                
            }
            
            return RedirectToAction(nameof(GetAllUsers));
        }
        

    }
}
