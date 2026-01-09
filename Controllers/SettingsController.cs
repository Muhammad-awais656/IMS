using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IExpenseType _expenseType;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        public SettingsController(IExpenseType expenseType)
        {
               _expenseType= expenseType; 
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllExpenseTypes(int pageNumber = 1, int? pageSize = null,string? ExpenseTypeName=null)
        {
            var viewModel = new ExpenseTypesViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (ExpenseTypeName == null)
                {
                    ExpenseTypeName = HttpContext.Request.Query["searchUsername"].ToString();
                }
                viewModel = await _expenseType.GetAllExpenseTypes(pageNumber, currentPageSize, ExpenseTypeName);
                

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                
            }
            return View(viewModel);
        }

        public ActionResult CreateExpenseType()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpenseType(AdminExpenseType expenseType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    // Handle null values for optional fields
                    expenseType.CreatedBy = userId;
                    expenseType.CreatedDate = DateTimeHelper.Now;
                    expenseType.ModifiedBy = userId;
                    expenseType.ModifiedDate = DateTimeHelper.Now;
                    
                    // Ensure optional fields are properly handled
                    if (string.IsNullOrWhiteSpace(expenseType.ExpenseTypeDescription))
                        expenseType.ExpenseTypeDescription = null;
                    
                    var result = await _expenseType.CreateExpenseTypeAsync(expenseType);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(GetAllExpenseTypes));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(expenseType);
                    }
                }
                else
                {
                    // Log validation errors for debugging
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(expenseType);
            }
            return View(expenseType);
        }


        public async Task<IActionResult> EditExpenseType(long id)
        {
            
            var user = await _expenseType.GetExpenseTypeByIdAsync(id);
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
        public async Task<IActionResult> EditExpenseType(long id, AdminExpenseType ExpenseType)
        {
            if (id != ExpenseType.ExpenseTypeId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ExpenseType.ModifiedDate = DateTimeHelper.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    ExpenseType.ModifiedBy = userId;
                    var response = await _expenseType.UpdateExpenseTypeAsync(ExpenseType);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(GetAllExpenseTypes));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(ExpenseType);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(ExpenseType);
        }



        public  async Task<IActionResult> DeleteExpenseType(long id)
        {
            var expenseType = new AdminExpenseType();
            try
            {
                expenseType = await _expenseType.GetExpenseTypeByIdAsync(id);
                if (expenseType == null)
                {
                    return NotFound();
                }
                

            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(expenseType);
        }

        // POST: /Users/Delete/5
        [HttpPost, ActionName("DeleteExpenseTypes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpenseTypes(long ExpenseTypeid)
        {
            try
            {
                var res = await _expenseType.DeleteExpenseTypeAsync(ExpenseTypeid);
                if (res != 0)
                {
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(GetAllExpenseTypes));
                }
                else
                {
                    TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                    return RedirectToAction(nameof(GetAllExpenseTypes));
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }

            return RedirectToAction(nameof(GetAllExpenseTypes));
        }


    }
}
