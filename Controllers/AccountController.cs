using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Enums;
using IMS.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace IMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = AlertMessages.LoginFailed;
                    return View(model);
                }
                HttpContext.Session.SetString("Domain", model.Domain.ToString());
                var user = await _userService.GetUserByCredentialsAsync(model.Username, model.Password);

                if (user != null)
                {
                    
                    HttpContext.Session.SetString("Domain", model.Domain.ToString());
                    HttpContext.Session.SetString("Username", model.Username);
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    var claims = new List<Claim>
                    {
                      new Claim(ClaimTypes.Name, model.Username),
                      new Claim("Domain",model.Domain),
                      new Claim("IsAdmin" ,user.IsAdmin.ToString()),
                      new Claim("UserId",user.UserId.ToString())

                     };
                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync("CookieAuth", principal);
                    TempData["Success"] = AlertMessages.LoginSuccess;
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
            TempData["ErrorMessage"] = AlertMessages.LoginFailed;
            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("CookieAuth");
                HttpContext.Session.Clear();
                TempData["Success"] = AlertMessages.LogOutSuccess;
                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {

                throw;
            }
           

        }
    }
}
