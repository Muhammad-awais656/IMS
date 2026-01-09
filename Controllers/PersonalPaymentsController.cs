using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class PersonalPaymentsController : Controller
    {
        private readonly IPersonalPaymentService _personalPaymentService;
        private const int DefaultPageSize = 5;
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };

        public PersonalPaymentsController(IPersonalPaymentService personalPaymentService)
        {
            _personalPaymentService = personalPaymentService;
        }

        // GET: PersonalPaymentsController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null)
        {
            var filters = new PersonalPaymentFilters();
            var viewModel = new PersonalPaymentViewModel();

            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Apply filters from query parameters
                if (!string.IsNullOrEmpty(HttpContext.Request.Query["PaymentFilters.BankName"]))
                {
                    filters.BankName = HttpContext.Request.Query["PaymentFilters.BankName"];
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["PaymentFilters.AccountNumber"]))
                {
                    filters.AccountNumber = HttpContext.Request.Query["PaymentFilters.AccountNumber"];
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["PaymentFilters.TransactionType"]))
                {
                    filters.TransactionType = HttpContext.Request.Query["PaymentFilters.TransactionType"];
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["PaymentFilters.PaymentDescription"]))
                {
                    filters.PaymentDescription = HttpContext.Request.Query["PaymentFilters.PaymentDescription"];
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["CreditAmountFrom"]))
                {
                    filters.CreditAmountFrom = Convert.ToDecimal(HttpContext.Request.Query["CreditAmountFrom"]);
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["CreditAmountTo"]))
                {
                    filters.CreditAmountTo = Convert.ToDecimal(HttpContext.Request.Query["CreditAmountTo"]);
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["DebitAmountFrom"]))
                {
                    filters.DebitAmountFrom = Convert.ToDecimal(HttpContext.Request.Query["DebitAmountFrom"]);
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["DebitAmountTo"]))
                {
                    filters.DebitAmountTo = Convert.ToDecimal(HttpContext.Request.Query["DebitAmountTo"]);
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"]))
                {
                    filters.DateFrom = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
                }

                if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"]))
                {
                    filters.DateTo = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
                }

                if (filters.DateFrom > filters.DateTo && filters.DateTo != default(DateTime))
                {
                    TempData["WarningMessage"] = AlertMessages.FromDateGreater;
                }

                // Populate dropdowns
                var bankNames = await _personalPaymentService.GetBankNamesAsync();
                var transactionTypes = new List<string> { "All", "Credit", "Debit" };

                ViewBag.TransactionTypes = new SelectList(transactionTypes, filters.TransactionType);
                ViewBag.BankNames = new SelectList(bankNames, filters.BankName);

                viewModel = await _personalPaymentService.GetAllPersonalPaymentsAsync(pageNumber, currentPageSize, filters);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(viewModel);
        }

        // GET: PersonalPaymentsController/Details/5
        public async Task<ActionResult> Details(long id)
        {
            try
            {
                var personalPayment = await _personalPaymentService.GetPersonalPaymentByIdAsync(id);
                if (personalPayment == null)
                {
                    return NotFound();
                }
                return View(personalPayment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PersonalPaymentsController/Create
        public async Task<ActionResult> Create()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
        }

        // POST: PersonalPaymentsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PersonalPayment personalPayment)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr ?? "1");
                    
                    personalPayment.CreatedBy = userId;
                    personalPayment.CreatedDate = DateTimeHelper.Now;
                    personalPayment.ModifiedDate = DateTimeHelper.Now;
                    personalPayment.ModifiedBy = userId;
                    personalPayment.PaymentDate = HttpContext.Request.Form["PaymentDate"].ToString() != "" 
                        ? Convert.ToDateTime(HttpContext.Request.Form["PaymentDate"]) 
                        : DateTimeHelper.Now;

                    var result = await _personalPaymentService.CreatePersonalPaymentAsync(personalPayment);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            // No need to repopulate dropdowns since we removed PaymentType
            
            return View(personalPayment);
        }

        // GET: PersonalPaymentsController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                var personalPayment = await _personalPaymentService.GetPersonalPaymentByIdAsync(id);
                if (personalPayment == null)
                {
                    return NotFound();
                }

                // No need for PaymentType dropdown
                
                return View(personalPayment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PersonalPaymentsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, PersonalPayment personalPayment)
        {
            if (id != personalPayment.PersonalPaymentId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    personalPayment.ModifiedDate = DateTimeHelper.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr ?? "1");
                    personalPayment.ModifiedBy = userId;
                    personalPayment.PaymentDate = HttpContext.Request.Form["PaymentDate"].ToString() != "" 
                        ? Convert.ToDateTime(HttpContext.Request.Form["PaymentDate"]) 
                        : personalPayment.PaymentDate;

                    var response = await _personalPaymentService.UpdatePersonalPaymentAsync(personalPayment);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }

            // No need to repopulate dropdowns since we removed PaymentType
            
            return View(personalPayment);
        }

        // GET: PersonalPaymentsController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                var personalPayment = await _personalPaymentService.GetPersonalPaymentByIdAsync(id);
                if (personalPayment == null)
                {
                    return NotFound();
                }
                return View(personalPayment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PersonalPaymentsController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long id)
        {
            try
            {
                var res = await _personalPaymentService.DeletePersonalPaymentAsync(id);
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

        // GET: PersonalPaymentsController/GetTransactionHistory
        [HttpGet]
        public async Task<JsonResult> GetTransactionHistory(long personalPaymentId, int pageNumber = 1, int pageSize = 10, 
            DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            try
            {
                var result = await _personalPaymentService.GetTransactionHistoryAsync(
                    personalPaymentId, pageNumber, pageSize, fromDate, toDate, transactionType);
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Error loading transaction history: " + ex.Message,
                    transactions = new List<object>(),
                    accountSummary = new object()
                });
            }
        }
    }
}
