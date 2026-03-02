using IMS.Common_Interfaces;
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
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly IExpenseType _expenseTypeService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        private readonly ILogger<ExpenseController> _logger;

        public ExpenseController(IExpenseService expenseService, IExpenseType expenseTypeService, ILogger<ExpenseController> logger)
        {
            _expenseService = expenseService;
            _expenseTypeService = expenseTypeService;
            _logger = logger;
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
                    expense.CreatedDate = DateTimeHelper.Now;
                    expense.ModifiedDate = DateTimeHelper.Now;
                    expense.ModifiedBy = userId;
                    expense.ExpenseDate= HttpContext.Request.Form["FromDate"].ToString() != "" ? Convert.ToDateTime(HttpContext.Request.Form["FromDate"].ToString()) : DateTimeHelper.Now;
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
                    expense.ModifiedDate = DateTimeHelper.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    expense.ModifiedBy = userId;
                    expense.ExpenseDate = HttpContext.Request.Form["FromDate"].ToString() != "" ? Convert.ToDateTime(HttpContext.Request.Form["FromDate"].ToString()) : DateTimeHelper.Now;
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

        [HttpGet]
        public async Task<IActionResult> ExportExcel(long? expenseTypeId = null, long? productId = null, string? details = null, decimal? amountFrom = null, decimal? amountTo = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            try
            {
                var filters = new ExpenseFilters { ExpenseTypeId = expenseTypeId, ProductId = productId, Details = details, AmountFrom = amountFrom, AmountTo = amountTo, DateFrom = dateFrom, DateTo = dateTo };
                const int exportPageSize = 100000;
                var model = await _expenseService.GetAllExpenseAsync(1, exportPageSize, filters);
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Expenses");
                worksheet.Cell(1, 1).Value = "Expense Id"; worksheet.Cell(1, 2).Value = "Expense Type"; worksheet.Cell(1, 3).Value = "Product"; worksheet.Cell(1, 4).Value = "Detail"; worksheet.Cell(1, 5).Value = "Amount"; worksheet.Cell(1, 6).Value = "Date";
                var headerRange = worksheet.Range(1, 1, 1, 6); headerRange.Style.Font.Bold = true; headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                int row = 2;
                decimal total = 0;
                foreach (var item in model.ExpenseList ?? new List<ExpenseModel>())
                {
                    worksheet.Cell(row, 1).Value = item.ExpenseId; worksheet.Cell(row, 2).Value = item.ExpenseType ?? ""; worksheet.Cell(row, 3).Value = item.ProductName ?? ""; worksheet.Cell(row, 4).Value = item.ExpenseDetail ?? ""; worksheet.Cell(row, 5).Value = item.Amount; worksheet.Cell(row, 6).Value = item.ExpenseDate.ToString("dd-MMM-yyyy");
                    total += item.Amount; row++;
                }
                row++; worksheet.Cell(row, 4).Value = "TOTAL:"; worksheet.Cell(row, 4).Style.Font.Bold = true; worksheet.Cell(row, 5).Value = total; worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Expenses_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting expenses to Excel"); TempData["ErrorMessage"] = "An error occurred while exporting to Excel."; return RedirectToAction(nameof(Index)); }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(long? expenseTypeId = null, long? productId = null, string? details = null, decimal? amountFrom = null, decimal? amountTo = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            try
            {
                var filters = new ExpenseFilters { ExpenseTypeId = expenseTypeId, ProductId = productId, Details = details, AmountFrom = amountFrom, AmountTo = amountTo, DateFrom = dateFrom, DateTo = dateTo };
                const int exportPageSize = 100000;
                var model = await _expenseService.GetAllExpenseAsync(1, exportPageSize, filters);
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                PdfWriter.GetInstance(document, stream); document.Open();
                document.Add(new Paragraph("Expense Management Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));
                var table = new PdfPTable(6); table.WidthPercentage = 100; table.SetWidths(new float[] { 1f, 1.5f, 1.5f, 2.5f, 1.2f, 1.2f });
                foreach (var h in new[] { "Expense Id", "Expense Type", "Product", "Detail", "Amount", "Date" })
                { var cell = new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = BaseColor.LIGHT_GRAY }; table.AddCell(cell); }
                decimal total = 0;
                foreach (var e in model.ExpenseList ?? new List<ExpenseModel>())
                { table.AddCell(e.ExpenseId.ToString()); table.AddCell(e.ExpenseType ?? ""); table.AddCell(e.ProductName ?? ""); table.AddCell(e.ExpenseDetail ?? ""); table.AddCell(e.Amount.ToString("N2")); table.AddCell(e.ExpenseDate.ToString("dd-MMM-yy")); total += e.Amount; }
                var sumCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { Colspan = 4, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY }; table.AddCell(sumCell); table.AddCell(new PdfPCell(new Phrase(total.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY }); table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                document.Add(table); document.Close();
                return File(stream.ToArray(), "application/pdf", $"Expenses_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting expenses to PDF"); TempData["ErrorMessage"] = "An error occurred while exporting to PDF."; return RedirectToAction(nameof(Index)); }
        }
    }
}
