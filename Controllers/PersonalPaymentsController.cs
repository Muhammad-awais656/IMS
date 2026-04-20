using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class PersonalPaymentsController : Controller
    {
        private readonly IPersonalPaymentService _personalPaymentService;
        private const int DefaultPageSize = 5;
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };
        private readonly ILogger<PersonalPaymentsController> _logger;

        public PersonalPaymentsController(IPersonalPaymentService personalPaymentService, ILogger<PersonalPaymentsController> logger)
        {
            _personalPaymentService = personalPaymentService;
            _logger = logger;

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
                //if (ModelState.IsValid)
                //{
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
                //}
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
                    request.PaymentDate ?? DateTimeHelper.Now);

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
                    request.PaymentDate ?? DateTimeHelper.Now);

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

        [HttpGet]
        public async Task<IActionResult> ExportExcel(long? personalPaymentId = null, string? accountNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new PersonalPaymentFilters { BankName = null, AccountNumber = accountNumber, DateFrom = fromDate ?? default, DateTo = toDate ?? default };
                if (personalPaymentId.HasValue && personalPaymentId > 0)
                {
                    var bankName = await _personalPaymentService.GetBankNameByIdAsync(personalPaymentId.Value);
                    filters.BankName = bankName;
                }
                const int exportPageSize = 100000;
                var model = await _personalPaymentService.GetAllPersonalPaymentsAsync(1, exportPageSize, filters);
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Personal Payment Accounts");
                worksheet.Cell(1, 1).Value = "Id"; worksheet.Cell(1, 2).Value = "Bank Name"; worksheet.Cell(1, 3).Value = "Account Number"; worksheet.Cell(1, 4).Value = "Account Holder"; worksheet.Cell(1, 5).Value = "Credit"; worksheet.Cell(1, 6).Value = "Debit"; worksheet.Cell(1, 7).Value = "Balance"; worksheet.Cell(1, 8).Value = "Description"; worksheet.Cell(1, 9).Value = "Date"; worksheet.Cell(1, 10).Value = "Active";
                var headerRange = worksheet.Range(1, 1, 1, 10); headerRange.Style.Font.Bold = true; headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                int row = 2;
                foreach (var item in model.PersonalPaymentList ?? new List<PersonalPaymentModel>())
                {
                    worksheet.Cell(row, 1).Value = item.PersonalPaymentId; worksheet.Cell(row, 2).Value = item.BankName ?? ""; worksheet.Cell(row, 3).Value = item.AccountNumber ?? ""; worksheet.Cell(row, 4).Value = item.AccountHolderName ?? ""; worksheet.Cell(row, 5).Value = item.CreditAmount; worksheet.Cell(row, 6).Value = item.DebitAmount; worksheet.Cell(row, 7).Value = item.NetAmount; worksheet.Cell(row, 8).Value = item.PaymentDescription ?? ""; worksheet.Cell(row, 9).Value = item.PaymentDate.ToString("dd-MMM-yyyy"); worksheet.Cell(row, 10).Value = item.IsActive ? "Yes" : "No";
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PersonalPaymentAccounts_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting personal payment accounts to Excel"); TempData["ErrorMessage"] = "An error occurred while exporting to Excel."; return RedirectToAction(nameof(Index)); }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(long? personalPaymentId = null, string? accountNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new PersonalPaymentFilters { BankName = null, AccountNumber = accountNumber, DateFrom = fromDate ?? default, DateTo = toDate ?? default };
                if (personalPaymentId.HasValue && personalPaymentId > 0)
                {
                    var bankName = await _personalPaymentService.GetBankNameByIdAsync(personalPaymentId.Value);
                    filters.BankName = bankName;
                }
                const int exportPageSize = 100000;
                var model = await _personalPaymentService.GetAllPersonalPaymentsAsync(1, exportPageSize, filters);
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 12f, 12f, 12f, 12f);
                PdfWriter.GetInstance(document, stream); document.Open();
                document.Add(new Paragraph("Personal Payment Accounts Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));
                var table = new PdfPTable(10); table.WidthPercentage = 100; table.SetWidths(new float[] { 0.55f, 1.35f, 1.1f, 1.35f, 0.95f, 0.95f, 0.95f, 1.85f, 0.95f, 0.5f });
                foreach (var h in new[] { "Id", "Bank", "Account #", "Holder", "Credit", "Debit", "Balance", "Description", "Date", "Active" })
                { var cell = new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7))) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = BaseColor.LIGHT_GRAY }; table.AddCell(cell); }
                foreach (var p in model.PersonalPaymentList ?? new List<PersonalPaymentModel>())
                { table.AddCell(p.PersonalPaymentId.ToString()); table.AddCell(p.BankName ?? ""); table.AddCell(p.AccountNumber ?? ""); table.AddCell(p.AccountHolderName ?? ""); table.AddCell(p.CreditAmount.ToString("N2")); table.AddCell(p.DebitAmount.ToString("N2")); table.AddCell(p.NetAmount.ToString("N2")); table.AddCell(p.PaymentDescription ?? ""); table.AddCell(p.PaymentDate.ToString("dd-MMM-yy")); table.AddCell(p.IsActive ? "Yes" : "No"); }
                document.Add(table); document.Close();
                return File(stream.ToArray(), "application/pdf", $"PersonalPaymentAccounts_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting personal payment accounts to PDF"); TempData["ErrorMessage"] = "An error occurred while exporting to PDF."; return RedirectToAction(nameof(Index)); }
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
