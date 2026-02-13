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
                // Check for PersonalPaymentId first (from Kendo dropdown)
                var bankIdParam = HttpContext.Request.Query["PaymentFilters.PersonalPaymentId"].ToString();
                if (!string.IsNullOrEmpty(bankIdParam) && long.TryParse(bankIdParam, out long bankId))
                {
                    // Get BankName from PersonalPaymentId
                    var bankName = await _personalPaymentService.GetBankNameByIdAsync(bankId);
                    if (!string.IsNullOrEmpty(bankName))
                    {
                        filters.BankName = bankName;
                    }
                }
                else if (!string.IsNullOrEmpty(HttpContext.Request.Query["PaymentFilters.BankName"]))
                {
                    // Fallback to direct BankName filter
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

                // Preserve filter parameters in ViewData for pagination links
                // Store PersonalPaymentId if we filtered by it
                if (!string.IsNullOrEmpty(bankIdParam) && long.TryParse(bankIdParam, out long storedBankId))
                {
                    ViewData["PaymentFilters.PersonalPaymentId"] = storedBankId.ToString();
                }
                else
                {
                    ViewData["PaymentFilters.PersonalPaymentId"] = "";
                }
                ViewData["PaymentFilters.BankName"] = filters.BankName;
                ViewData["PaymentFilters.AccountNumber"] = filters.AccountNumber;
                ViewData["PaymentFilters.TransactionType"] = filters.TransactionType;
                ViewData["PaymentFilters.PaymentDescription"] = filters.PaymentDescription;
                ViewData["CreditAmountFrom"] = filters.CreditAmountFrom?.ToString();
                ViewData["CreditAmountTo"] = filters.CreditAmountTo?.ToString();
                ViewData["DebitAmountFrom"] = filters.DebitAmountFrom?.ToString();
                ViewData["DebitAmountTo"] = filters.DebitAmountTo?.ToString();
                ViewData["FromDate"] = filters.DateFrom != default(DateTime) ? filters.DateFrom.ToString("yyyy-MM-dd") : "";
                ViewData["ToDate"] = filters.DateTo != default(DateTime) ? filters.DateTo.ToString("yyyy-MM-dd") : "";

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
                    personalPayment.CreatedDate = DateTime.Now;
                    personalPayment.ModifiedDate = DateTime.Now;
                    personalPayment.ModifiedBy = userId;
                    personalPayment.PaymentDate = HttpContext.Request.Form["PaymentDate"].ToString() != "" 
                        ? Convert.ToDateTime(HttpContext.Request.Form["PaymentDate"]) 
                        : DateTime.Now;

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
                    personalPayment.ModifiedDate = DateTime.Now;
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

        // GET: PersonalPaymentsController/GetBankNames
        [HttpGet]
        public async Task<JsonResult> GetBankNames()
        {
            try
            {
                var bankAccounts = await _personalPaymentService.GetBankAccountsAsync();
                
                if (bankAccounts == null || bankAccounts.Count == 0)
                {
                    // Return at least the "All" option
                    return Json(new List<object> { new { value = "", text = "--All Banks--" } });
                }

                var bankOptions = bankAccounts.Select(account => new
                {
                    value = account.PersonalPaymentId.ToString(),
                    text = account.BankName
                }).OrderBy(b => b.text)
                  .ToList();

                // Add "All" option at the beginning
                bankOptions.Insert(0, new { value = "", text = "--All Banks--" });

                return Json(bankOptions);
            }
            catch (Exception ex)
            {
                // Log error and return at least the "All" option
                return Json(new List<object> { new { value = "", text = "--All Banks--" } });
            }
        }

        // GET: PersonalPaymentsController/GetAccountBalance
        [HttpGet]
        public async Task<JsonResult> GetAccountBalance(long accountId)
        {
            try
            {
                var balance = await _personalPaymentService.GetAccountBalanceAsync(accountId);
                return Json(new { success = true, balance = balance });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, balance = 0, message = ex.Message });
            }
        }

        // POST: PersonalPaymentsController/BankDeposit
        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON endpoints don't support standard anti-forgery token validation
        public async Task<JsonResult> BankDeposit([FromBody] BankDepositWithdrawRequest request)
        {
            try
            {
                if (request == null || request.PersonalPaymentId <= 0 || request.Amount <= 0)
                {
                    return Json(new { success = false, message = "Invalid request parameters" });
                }

                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr ?? "1");

                var result = await _personalPaymentService.ProcessBankDepositAsync(
                    request.PersonalPaymentId, 
                    request.Amount, 
                    request.Description ?? "Bank Deposit",
                    userId,
                    request.PaymentDate ?? DateTime.Now);

                if (result)
                {
                   TempData["Success"] = "Deposit processed successfully";
                    return Json(new { success = true, message = "Deposit processed successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to process deposit" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: PersonalPaymentsController/BankWithdraw
        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON endpoints don't support standard anti-forgery token validation
        public async Task<JsonResult> BankWithdraw([FromBody] BankDepositWithdrawRequest request)
        {
            try
            {
                if (request == null || request.PersonalPaymentId <= 0 || request.Amount <= 0)
                {
                    return Json(new { success = false, message = "Invalid request parameters" });
                }

                // Check if sufficient balance exists
                var currentBalance = await _personalPaymentService.GetAccountBalanceAsync(request.PersonalPaymentId);
                //if (request.Amount > currentBalance)
                //{
                //    return Json(new { success = false, message = "Insufficient balance. Available balance: " + currentBalance.ToString("N2") });
                //}

                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr ?? "1");

                var result = await _personalPaymentService.ProcessBankWithdrawAsync(
                    request.PersonalPaymentId, 
                    request.Amount, 
                    request.Description ?? "Bank Withdraw",
                    userId,
                    request.PaymentDate ?? DateTime.Now);

                if (result)
                {
                    TempData["Success"] = "Withdraw processed successfully";
                    return Json(new { success = true, message = "Withdraw processed successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to process withdraw" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Request model for Bank Deposit/Withdraw
    public class BankDepositWithdrawRequest
    {
        public long PersonalPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
