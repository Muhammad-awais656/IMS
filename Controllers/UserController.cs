using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace IMS.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        
        //public string domain = string.Empty;
        private readonly IUserService _userService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes


        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        // GET: /Users/Details/5
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(int pageNumber=1, int? pageSize = null,string? UserNameSearch=null)
        {
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
                    UserNameSearch = HttpContext.Request.Query.FirstOrDefault().Value;
                }
                var viewModel = await _userService.GetPagedUsersAsync(pageNumber, currentPageSize, UserNameSearch);
                return View(viewModel);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =ex.Message;
                throw;
            }
            
        }
        // GET: UserController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserController/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _userService.CreateUserAsync(user);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(GetAllUsers));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(user);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(user);
            }
            return View(user);
        }

        // GET: UserController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: UserController/Edit/5
        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle password: only update if provided
                    if (string.IsNullOrEmpty(user.UserPassword))
                    {
                        var existingUser = await _userService.GetUserByIdAsync(id);
                        user.UserPassword = existingUser.UserPassword; // Retain existing password
                    }
                    else
                    {
                        // In production, hash the password here
                        // user.UserPassword = HashPassword(user.UserPassword);
                    }
                    var response = await _userService.UpdateUserAsync(user);
                    if (response!=0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(GetAllUsers));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(user);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _userService.GetUserByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(user);
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
