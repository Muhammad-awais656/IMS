using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using System.Threading.Tasks;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly ICustomer _customerService;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };
        public CustomerController(ICustomer repository, ILogger<CustomerController> logger)
        {
            _customerService = repository;
            _logger = logger;
        }
        // GET: CustomerController
        public async Task<ActionResult> Index(CustomerViewModel model, int pageNumber = 1, int? pageSize = null, string sidx = "Id", string sord = "asc", bool _search = false)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new CustomerViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new CustomerFilters();
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _customerService.GetCustomers(pageNumber, currentPageSize, 
                    string.IsNullOrWhiteSpace(filters.CustomerName) ? null : filters.CustomerName,
                    string.IsNullOrWhiteSpace(filters.PhoneNumber) ? null : filters.PhoneNumber,
                    string.IsNullOrWhiteSpace(filters.Email) ? null : filters.Email);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new CustomerViewModel();
                }
            }
            return View(model);
        }
        


        // GET: CustomerController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CustomerController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CustomerController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Customer customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    // Handle null values for optional fields
                    customer.CreatedBy = userId;
                    customer.CreatedDate = DateTimeHelper.Now;
                    customer.ModifiedBy = userId;
                    customer.ModifiedDate = DateTimeHelper.Now;
                    
                    // Ensure optional fields are properly handled
                    if (string.IsNullOrWhiteSpace(customer.CustomerEmail))
                        customer.CustomerEmail = null;
                    
                    if (string.IsNullOrWhiteSpace(customer.CustomerEmailCc))
                        customer.CustomerEmailCc = null;
                    
                    if (string.IsNullOrWhiteSpace(customer.CustomerAddress))
                        customer.CustomerAddress = null;
                    
                    if (customer.InvoiceCreditPeriod == 0)
                        customer.InvoiceCreditPeriod = null;
                    
                    if (customer.StartWorkingTime == default(TimeOnly))
                        customer.StartWorkingTime = null;
                    
                    if (customer.EndWorkingTime == default(TimeOnly))
                        customer.EndWorkingTime = null;

                    var result = await _customerService.CreateCustomerAsync(customer);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(customer);
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
                return View(customer);
            }
            return View(customer);
        }

        // GET: CustomerController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var unit = await _customerService.GetCustomerIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return View(unit);
        }

        // POST: CustomerController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    // Handle null values for optional fields
                    customer.ModifiedDate = DateTimeHelper.Now;
                    customer.ModifiedBy = userId;
                    
                    // Ensure optional fields are properly handled
                    if (string.IsNullOrWhiteSpace(customer.CustomerEmail))
                        customer.CustomerEmail = null;
                    
                    if (string.IsNullOrWhiteSpace(customer.CustomerEmailCc))
                        customer.CustomerEmailCc = null;
                    
                    if (string.IsNullOrWhiteSpace(customer.CustomerAddress))
                        customer.CustomerAddress = null;
                    
                    if (customer.InvoiceCreditPeriod == 0)
                        customer.InvoiceCreditPeriod = null;
                    
                    if (customer.StartWorkingTime == default(TimeOnly))
                        customer.StartWorkingTime = null;
                    
                    if (customer.EndWorkingTime == default(TimeOnly))
                        customer.EndWorkingTime = null;
                    
                    var response = await _customerService.UpdateCustomerAsync(customer); 
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(customer);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                    return View(customer);
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
            return View(customer);
        }

        // GET: CustomerController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var customer = new Customer();
            try
            {
                customer = await _customerService.GetCustomerIdAsync(id);
                if (customer == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(customer);
        }

        // POST: CustomerController/Delete/5
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long id)
        {
            try
            {
                var res = await _customerService.DeleteCustomerAsync(id);
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

        /// <summary>
        /// Export customers to Excel (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? customerName = null, string? phoneNumber = null, string? email = null)
        {
            try
            {
                const int exportPageSize = 100000;
                var model = await _customerService.GetCustomers(1, exportPageSize, customerName, phoneNumber, email);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Customers");

                worksheet.Cell(1, 1).Value = "Customer Id";
                worksheet.Cell(1, 2).Value = "Customer Name";
                worksheet.Cell(1, 3).Value = "Contact Number";
                worksheet.Cell(1, 4).Value = "Email";
                worksheet.Cell(1, 5).Value = "Address";
                worksheet.Cell(1, 6).Value = "Enabled";

                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var item in model.Customers ?? new List<Customer>())
                {
                    worksheet.Cell(row, 1).Value = item.CustomerId;
                    worksheet.Cell(row, 2).Value = item.CustomerName ?? "";
                    worksheet.Cell(row, 3).Value = item.CustomerContactNumber ?? "";
                    worksheet.Cell(row, 4).Value = item.CustomerEmail ?? "";
                    worksheet.Cell(row, 5).Value = item.CustomerAddress ?? "";
                    worksheet.Cell(row, 6).Value = item.IsEnabled ? "Yes" : "No";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                string filename = $"Customers_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customers to Excel");
                TempData["ErrorMessage"] = "An error occurred while exporting to Excel.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export customers to PDF (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(string? customerName = null, string? phoneNumber = null, string? email = null)
        {
            try
            {
                const int exportPageSize = 100000;
                var model = await _customerService.GetCustomers(1, exportPageSize, customerName, phoneNumber, email);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                document.Add(new Paragraph("Customer Management Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                var table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 2.5f, 1.5f, 2f, 2.5f, 0.8f });

                string[] headers = { "Customer Id", "Customer Name", "Contact Number", "Email", "Address", "Enabled" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                foreach (var c in model.Customers ?? new List<Customer>())
                {
                    table.AddCell(c.CustomerId.ToString());
                    table.AddCell(c.CustomerName ?? "");
                    table.AddCell(c.CustomerContactNumber ?? "");
                    table.AddCell(c.CustomerEmail ?? "");
                    table.AddCell(c.CustomerAddress ?? "");
                    table.AddCell(c.IsEnabled ? "Yes" : "No");
                }

                document.Add(table);
                document.Close();

                string filename = $"Customers_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customers to PDF");
                TempData["ErrorMessage"] = "An error occurred while exporting to PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get customers for Kendo dropdown (e.g. Customer Open Balance modal).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomersForDropdown()
        {
            try
            {
                var customers = await _customerService.GetAllEnabledCustomers();
                var result = customers?.Select(c => new
                {
                    value = c.CustomerId.ToString(),
                    text = c.CustomerName
                }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for dropdown");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Get current balance for a customer (e.g. for Customer Open Balance modal). Placeholder returns 0; implement from sales/payments as needed.
        /// </summary>
        [HttpGet]
        public Task<IActionResult> GetCustomerBalance(long customerId)
        {
            try
            {
                // TODO: Implement from sales receivable / payments when business logic is defined
                decimal balance = 0;
                return Task.FromResult<IActionResult>(Json(new { success = true, balance = balance }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer balance");
                return Task.FromResult<IActionResult>(Json(new { success = false, balance = 0m, message = ex.Message }));
            }
        }
    }
}
