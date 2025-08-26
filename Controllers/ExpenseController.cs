using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }
        // GET: ExpenseController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, ExpenseFilters expenseFilters=null)
        {
            var viewModel = new ExpenseViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                viewModel = await _expenseService.GetAllExpenseAsync(pageNumber, pageSize, expenseFilters);

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
        public ActionResult Create()
        {
            return View();
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
