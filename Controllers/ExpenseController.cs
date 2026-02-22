using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly IExpenseType _expenseTypeService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes

        public ExpenseController(IExpenseService expenseService, IExpenseType expenseTypeService)
        {
            _expenseService = expenseService;
            _expenseTypeService = expenseTypeService;
        }
        // GET: ExpenseController
        public async Task<IActionResult> Index(ExpenseViewModel model,
    int pageNumber = 1,
    int? pageSize = null)
        {
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var expenseTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();

                model.EnabledExpenses = expenseTypes
                    .Select(x => new SelectListItem
                    {
                        Value = x.ExpenseTypeId.ToString(),
                        Text = x.ExpenseTypeName
                    });

                // Date validation
                if (model.ExpenseFilters.DateFrom > model.ExpenseFilters.DateTo)
                {
                    TempData["WarningMessage"] = AlertMessages.FromDateGreater;
                }

                // Get filtered data
                model = await _expenseService.GetAllExpenseAsync(
                    pageNumber,
                    currentPageSize,
                    model.ExpenseFilters);

                // Reassign dropdown again (important after service call)
                model.EnabledExpenses = expenseTypes
                    .Select(x => new SelectListItem
                    {
                        Value = x.ExpenseTypeId.ToString(),
                        Text = x.ExpenseTypeName
                    });

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(model);
        }

        // GET: ExpenseController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ExpenseController/Create
        public ActionResult Create()
        {
            
            try
            {
                //var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                //ViewBag.EnabledExpenses = new SelectList(expeneTypes, "ExpenseTypeId", "ExpenseTypeName");
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEnabledExpenseTypes()
        {
            try
            {
                var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                var result = expeneTypes.Select(p => new
                {
                    value = p.ExpenseTypeId.ToString(),
                    text = p.ExpenseTypeName,
                
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error getting products for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // POST: ExpenseController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Expense expense)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    expense.CreatedBy = userId;
                    expense.CreatedDate = DateTime.Now;
                    expense.ModifiedDate = DateTime.Now;
                    expense.ModifiedBy = userId;
                    expense.ExpenseDate= HttpContext.Request.Form["FromDate"].ToString() != "" ? Convert.ToDateTime(HttpContext.Request.Form["FromDate"].ToString()) : DateTime.Now;
                    var result = await _expenseService.CreateExpenseAsync(expense);
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
                return View(expense);
            }
            return View(expense);
        }

        // GET: ExpenseController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
           
            var user = await _expenseService.GetExpenseByIdAsync(id);
            //var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
            //ViewBag.EnabledExpenses = new SelectList(expeneTypes, "ExpenseTypeId", "ExpenseTypeName", user.ExpenseTypeIdFk);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: ExpenseController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, Expense expense)
        {
            if (id != expense.ExpenseId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    expense.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    expense.ModifiedBy = userId;
                    expense.ExpenseDate = HttpContext.Request.Form["FromDate"].ToString() != "" ? Convert.ToDateTime(HttpContext.Request.Form["FromDate"].ToString()) : DateTime.Now;
                    var response = await _expenseService.UpdateExpenseAsync(expense);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(expense);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(expense);
        }

        // GET: ExpenseController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var expense = new Expense();
            try
            {
                expense = await _expenseService.GetExpenseByIdAsync(id);
                if (expense == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(expense);
        }

        // POST: ExpenseController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long id)
        {
            try
            {
                var res = await _expenseService.DeleteExpenseAsync(id);
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
