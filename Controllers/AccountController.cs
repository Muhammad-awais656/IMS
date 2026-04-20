using ClosedXML.Excel;
using IMS.Authorization;
using IMS.Common_Helpers;
using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace IMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IdentityHelper _identityHelper;
        private readonly IBranchService _branchService;

        public AccountController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, SignInManager<ApplicationUser> signInManager, IdentityHelper identityHelper,
            IBranchService branchService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _identityHelper = identityHelper;
            _branchService = branchService;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var branches = await _identityHelper.GetAllActiveBranches();
            var model = new LoginViewModel { Branches = branches };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {

                //if (!ModelState.IsValid)
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = AlertMessages.LoginFailed;
                //    return View(model);
                //}
                //var appUser = await _userManager.FindByNameAsync(model.Username);
                //if (appUser == null)
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = AlertMessages.LoginFailed;
                //    return View(model);
                //}

                //if (!appUser.IsActive)
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = "User is disabled.";
                //    return View(model);
                //}

                //var passwordValid = await _userManager.CheckPasswordAsync(appUser, model.Password);
                //if (!passwordValid)
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = AlertMessages.LoginFailed;
                //    return View(model);
                //}

                //var assignedBranchIds = await _dbContext.IdentityUserBranches
                //    .Where(x => x.UserId == appUser.Id)
                //    .Select(x => x.BranchId)
                //    .ToListAsync();
                //if (assignedBranchIds.Count == 0)
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = "User is not assigned to any branch.";
                //    return View(model);
                //}
                //if (!assignedBranchIds.Contains(model.BranchId))
                //{
                //    model.Branches = await _loginBranchesService.GetBranchesForLoginAsync();
                //    TempData["ErrorMessage"] = "Selected branch is not assigned to this user.";
                //    return View(model);
                //}


                //HttpContext.Session.SetString("Username", model.Username);
                //HttpContext.Session.SetString("UserId", (appUser.LegacyUserId ?? 0).ToString());
                //HttpContext.Session.SetInt32("BranchId", model.BranchId);
                //var branch = await _branchService.GetBranchByIdAsync(model.BranchId);
                //if (branch != null)
                //{
                //    HttpContext.Session.SetString("BranchName", branch.BranchName ?? branch.BranchCode ?? model.BranchId.ToString());
                //    HttpContext.Session.SetString("BranchCode", branch.BranchCode ?? "");
                //}

                //await _signInManager.SignOutAsync();
                //var claims = new List<Claim>
                //{

                //    new Claim("IsAdmin", appUser.IsAdmin.ToString()),
                //    new Claim("UserId", (appUser.LegacyUserId ?? 0).ToString()),
                //    new Claim("BranchId", model.BranchId.ToString())
                //};
                //await _signInManager.SignInWithClaimsAsync(appUser, isPersistent: false, claims);
                //TempData["Success"] = AlertMessages.LoginSuccess;
                //return RedirectToAction("Index", "Home");

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // This doesn't count login failures towards lockout only two factor authentication
                // To enable password failures to trigger lockout, change to shouldLockout: true
                bool isActive = true;
                bool ValidFlag = false;
                if (!isActive)
                {
                    ModelState.AddModelError("", "Inactive user login attempt.");
                    return View(model);
                }
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                var UserId = await _identityHelper.GetUserId(model.Username);

                if (!string.IsNullOrEmpty(UserId))
                    ValidFlag = await _identityHelper.IsAuthenticatedUser(model.Username, model.Password, model.BranchId);

                


                if (result.Succeeded && ValidFlag)
                {
                    await SetLoginSessionAsync(model);
                    TempData["Success"] = AlertMessages.LoginSuccess;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    await _signInManager.SignOutAsync();
                    if (result.Succeeded && ValidFlag == false)
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                        ViewBag.Message = "Your Account is Inactive";
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                        ViewBag.Message = "UserName Or Password is Incorrect";
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                TempData["Success"] = AlertMessages.LogOutSuccess;
                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {

                throw;
            }
           

        }

        /// <summary>
        /// Populates session after sign-in. Uses claims when present; otherwise model and <see cref="ApplicationUser"/> (same keys as Program.cs middleware).
        /// </summary>
        private async Task SetLoginSessionAsync(LoginViewModel model)
        {
            await HttpContext.Session.LoadAsync();
            var principal = HttpContext.User;
            var appUser = await _userManager.FindByNameAsync(model.Username);

            var userName = principal.Identity?.Name
                ?? appUser?.UserName
                ?? model.Username;
            var role = principal.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value
                ?? appUser?.IsAdmin.ToString()
                ?? string.Empty;
            var usrId = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                ?? (appUser?.LegacyUserId?.ToString() ?? string.Empty);

            HttpContext.Session.SetString("UserName", userName);
            HttpContext.Session.SetString("IsAdmin", role ?? string.Empty);
            HttpContext.Session.SetString("UserId", usrId ?? string.Empty);

            HttpContext.Session.SetInt32("BranchId", model.BranchId);
            var branch = await _branchService.GetBranchByIdAsync(model.BranchId);
            if (branch != null)
            {
                HttpContext.Session.SetString("BranchName", branch.BranchName ?? branch.BranchCode ?? model.BranchId.ToString());
                HttpContext.Session.SetString("BranchCode", branch.BranchCode ?? string.Empty);
            }
            else
            {
                HttpContext.Session.SetString("BranchName", model.BranchId.ToString());
                HttpContext.Session.SetString("BranchCode", string.Empty);
            }
        }
    }
}
