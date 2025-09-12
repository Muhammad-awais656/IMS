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
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null)
        {
            ExpenseFilters expenseFilters = new ExpenseFilters();
            var viewModel = new ExpenseViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                var expensetypeid = HttpContext.Request.Query["expenseFilters.ExpenseTypeId"].ToString();

                long? selectedExpTypeId = null;
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["expenseFilters.ExpenseTypeId"].ToString()))
                {
                    selectedExpTypeId = Convert.ToInt64(HttpContext.Request.Query["expenseFilters.ExpenseTypeId"].ToString());
                }
                if (!string.IsNullOrWhiteSpace(expensetypeid))
                {

                    selectedExpTypeId = Convert.ToInt64(expensetypeid);

                }
                if (selectedExpTypeId!=null && selectedExpTypeId!=0)
                {
                    expenseFilters.ExpenseTypeId = selectedExpTypeId;
                }
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["expenseDetail"].ToString()))
                {
                    expenseFilters.Details = HttpContext.Request.Query["expenseDetail"].ToString();
                }
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchpFrom"].ToString()))
                {
                    expenseFilters.AmountFrom = Convert.ToDecimal(HttpContext.Request.Query["searchpFrom"]);
                }
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["searchpTo"].ToString()))
                {
                    expenseFilters.AmountTo = Convert.ToDecimal(HttpContext.Request.Query["searchpTo"]);
                }
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
                {
                    expenseFilters.DateFrom = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
                }
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
                {
                    expenseFilters.DateTo = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
                }
                if (expenseFilters.DateFrom > expenseFilters.DateTo)
                {
                    TempData["WarningMessage"] = AlertMessages.FromDateGreater;
                }

                ViewBag.EnabledExpenses = new SelectList(expeneTypes, "ExpenseTypeId", "ExpenseTypeName", selectedExpTypeId);
                

                viewModel = await _expenseService.GetAllExpenseAsync(pageNumber, currentPageSize, expenseFilters);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: ExpenseController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ExpenseController/Create
        public async Task<ActionResult> Create()
        {
            
            try
            {
                var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                ViewBag.EnabledExpenses = new SelectList(expeneTypes, "ExpenseTypeId", "ExpenseTypeName");
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
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
            var expeneTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
            ViewBag.EnabledExpenses = new SelectList(expeneTypes, "ExpenseTypeId", "ExpenseTypeName", user.ExpenseTypeIdFk);
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
