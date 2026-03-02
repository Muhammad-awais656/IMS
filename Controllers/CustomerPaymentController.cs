using IMS.CommonUtilities;
using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using static IMS.Models.CustomerPaymentViewModel;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class CustomerPaymentController : Controller
    {
        private readonly ICustomerPaymentService _customerPaymentService;
        private readonly ISalesService _salesService;
        private readonly IVendorPaymentService _vendorPaymentService;
        private readonly ILogger<CustomerPaymentController> _logger;

        public CustomerPaymentController(ICustomerPaymentService customerPaymentService, ILogger<CustomerPaymentController> logger, ISalesService salesService, IVendorPaymentService vendorPaymentService)
        {
            _customerPaymentService = customerPaymentService;
            _logger = logger;
            _salesService = salesService;
            _vendorPaymentService = vendorPaymentService;
        }

        public async Task<IActionResult> Index(CustomerPaymentViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new CustomerPaymentViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new CustomerPaymentFilters();
                }

                // Don't set default dates - let them be null to show all payments on first load

                var currentPageSize = pageSize ?? 10;
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _customerPaymentService.GetAllPaymentsAsync(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments index");
                TempData["ErrorMessage"] = "An error occurred while loading payments.";
                if (model == null)
                {
                    model = new CustomerPaymentViewModel();
                }
                return View(model);
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var payment = await _customerPaymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment details");
                TempData["ErrorMessage"] = "An error occurred while loading payment details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new CustomerPaymentViewModel();
                viewModel.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                viewModel.SalesList = await _customerPaymentService.GetAllSalesAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerPaymentViewModel model)
        {
            try
            {
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("SaleId"))
                    {
                        keysToRemove.Add(key);
                    }


                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    var payment = new Payment
                    {
                        PaymentAmount = model.PaymentAmount,
                        SaleId = model.SaleId,
                        CustomerId = model.CustomerId,
                        PaymentDate = model.PaymentDate,
                        paymentMethod = model.PaymentMethod,
                        onlineAccountId = model.OnlineAccountId,
                        CreatedBy = userId, // TODO: Get from current user session
                        CreatedDate = DateTimeHelper.Now,
                        Description = model.Description,
                        SupplierId = null,
                    };

                    var result = await _customerPaymentService.CreatePaymentAsync(payment);
              
                    if (result)
                    {
                        // Process online payment transaction if payment method is Online
                        if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                        {
                            try
                            {
                                var transactionDescription = $"Added Payment - Sale Id #{model.SaleId} - {model.Description}";
                                var transactionId = await _salesService.ProcessOnlinePaymentTransactionAsync(
                                    model.OnlineAccountId.Value,
                                    model.SaleId,
                                    model.PaymentAmount, // Credit the received amount to the online account
                                    transactionDescription,
                                    userId,
                                    DateTimeHelper.Now
                                );

                                _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Sale ID: {SaleId}",
                                    transactionId, model.SaleId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing online payment transaction for Sale ID: {SaleId}", model.SaleId);
                                // Don't fail the entire sale if online payment processing fails
                                // Just log the error and continue
                            }
                        }

                        // Process General Payments - deduct from customer, credit to vendor
                        if (model.PaymentMethod == "General Payments" && model.VendorId.HasValue && model.VendorId > 0)
                        {
                            try
                            {
                                // Find the first bill with due amount for the vendor, or use 0 if no bill found
                               // var vendorBills = await _vendorPaymentService.GetSupplierBillNumbersAsync(model.VendorId.Value);
                                long billId = 0;
                                //if (vendorBills != null && vendorBills.Any(b => b.TotalDueAmount > 0))
                                //{
                                //    billId = vendorBills.First(b => b.TotalDueAmount > 0).PurchaseOrderId;
                                //}

                                var vendorPaymentDescription = $"General Payment from Customer({model.CustomerId}) - {model.Description ?? "Direct payment"}";
                                var vendorPaymentResult = await _vendorPaymentService.CreateVendorPaymentAsync(
                                    paymentAmount: model.PaymentAmount,
                                    billId: billId,
                                    supplierId: model.VendorId.Value,
                                    paymentDate: model.PaymentDate,
                                    createdBy: userId,
                                    createdDate: DateTimeHelper.Now,
                                    description: vendorPaymentDescription,
                                    paymentMethod: "General Payments",
                                    onlineAccountId: null,
                                    customerId: null
                                );

                                if (vendorPaymentResult)
                                {
                                    _logger.LogInformation("General Payment processed successfully. Customer Payment ID: {PaymentId}, Vendor ID: {VendorId}, Amount: {Amount}",
                                        payment.PaymentId, model.VendorId.Value, model.PaymentAmount);
                                }
                                else
                                {
                                    _logger.LogWarning("Customer payment created but vendor payment failed for General Payment. Customer Payment ID: {PaymentId}, Vendor ID: {VendorId}",
                                        payment.PaymentId, model.VendorId.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing General Payment vendor credit. Customer Payment ID: {PaymentId}, Vendor ID: {VendorId}",
                                    payment.PaymentId, model.VendorId.Value);
                                // Don't fail the entire payment if vendor payment processing fails
                                // Just log the error and continue
                            }
                        }

                        TempData["SuccessMessage"] = "Payment created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create payment.";
                    }
                }
                else
                {
                    // Reload dropdowns if validation fails
                    model.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                    model.SalesList = await _customerPaymentService.GetAllSalesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                TempData["ErrorMessage"] = "An error occurred while creating the payment.";
                model.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                model.SalesList = await _customerPaymentService.GetAllSalesAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var payment = await _customerPaymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new CustomerPaymentViewModel
                {
                    PaymentId = payment.PaymentId,
                    PaymentAmount = payment.PaymentAmount,
                    SaleId = payment.SaleId,
                    CustomerId = payment.CustomerId,
                    PaymentDate = payment.PaymentDate,
                    Description = payment.Description,
                    PaymentMethod = payment.paymentMethod,
                    OnlineAccountId = payment.onlineAccountId,
                    SupplierName = payment.SupplierName,
                    VendorId = payment.SupplierId
                    
                };
                viewModel.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                viewModel.SalesList = await _customerPaymentService.GetAllSalesAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerPaymentViewModel model)
        {
            try
            {
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("SaleId"))
                    {
                        keysToRemove.Add(key);
                    }


                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = string.IsNullOrEmpty(userIdStr) ? 1 : long.Parse(userIdStr);

                if (ModelState.IsValid)
                {
                    var existingPayment = await _customerPaymentService.GetPaymentByIdAsync(model.PaymentId);
                    if (existingPayment == null)
                    {
                        TempData["ErrorMessage"] = "Payment not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    var existingOnline = string.Equals(existingPayment.paymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && existingPayment.onlineAccountId.HasValue
                        && existingPayment.onlineAccountId > 0;
                    var newOnline = string.Equals(model.PaymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && model.OnlineAccountId.HasValue
                        && model.OnlineAccountId > 0;
                    var paymentChangedForOnline = existingPayment.PaymentAmount != model.PaymentAmount
                        || existingPayment.onlineAccountId != model.OnlineAccountId
                        || existingPayment.SaleId != model.SaleId;
                    var requiresReverse = existingOnline && (!newOnline || paymentChangedForOnline);
                    var requiresNewTransaction = newOnline && (!existingOnline || paymentChangedForOnline);

                    var payment = new Payment
                    {
                        PaymentId = model.PaymentId,
                        PaymentAmount = model.PaymentAmount,
                        SaleId = model.SaleId,
                        CustomerId = model.CustomerId,
                        PaymentDate = model.PaymentDate,
                        paymentMethod = model.PaymentMethod,
                        onlineAccountId = newOnline ? model.OnlineAccountId : null,
                        ModifiedBy = userId, // TODO: Get from current user session
                        ModifiedDate = DateTimeHelper.Now,
                        Description = model.Description,
                        SupplierId = model.VendorId
                    };

                    var result = await _customerPaymentService.UpdatePaymentAsync(payment);
                    if (result > 0)
                    {
                        if (requiresReverse)
                        {
                            try
                            {
                                await _salesService.ProcessOnlinePaymentTransactionAsync(
                                    existingPayment.onlineAccountId!.Value,
                                    existingPayment.SaleId,
                                    -existingPayment.PaymentAmount,
                                    $"Reversed payment for Sale #{existingPayment.SaleId}",
                                    userId,
                                    DateTimeHelper.Now
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error reversing online payment transaction for Payment ID: {PaymentId}", model.PaymentId);
                                TempData["WarningMessage"] = "Payment updated but reversing the old online transaction failed. Please verify account balances.";
                            }
                        }

                        if (requiresNewTransaction)
                        {
                            try
                            {
                                var transactionDescription = $"Updated Payment - Sale Id #{model.SaleId} - {model.Description}";
                                var transactionId = await _salesService.ProcessOnlinePaymentTransactionAsync(
                                    model.OnlineAccountId!.Value,
                                    model.SaleId,
                                    model.PaymentAmount,
                                    transactionDescription,
                                    userId,
                                    DateTimeHelper.Now
                                );

                                _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Sale ID: {SaleId}",
                                    transactionId, model.SaleId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing online payment transaction for Payment ID: {PaymentId}", model.PaymentId);
                                TempData["WarningMessage"] = "Payment updated but online transaction processing failed. Please verify account balances.";
                            }
                        }

                        TempData["SuccessMessage"] = "Payment updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update payment.";
                    }
                }
                else
                {
                    // Reload dropdowns if validation fails
                    model.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                    model.SalesList = await _customerPaymentService.GetAllSalesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                model.CustomerList = await _customerPaymentService.GetAllCustomersAsync();
                model.SalesList = await _customerPaymentService.GetAllSalesAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var payment = await _customerPaymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the delete form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = string.IsNullOrEmpty(userIdStr) ? 1 : long.Parse(userIdStr);

                var existingPayment = await _customerPaymentService.GetPaymentByIdAsync(id);
                if (existingPayment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _customerPaymentService.DeletePaymentAsync(id, DateTimeHelper.Now, userId);
                if (result > 0)
                {
                    var isOnline = string.Equals(existingPayment.paymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && existingPayment.onlineAccountId.HasValue
                        && existingPayment.onlineAccountId > 0;
                    if (isOnline)
                    {
                        try
                        {
                            await _salesService.ProcessOnlinePaymentTransactionAsync(
                                existingPayment.onlineAccountId.Value,
                                existingPayment.SaleId,
                                -existingPayment.PaymentAmount,
                                $"Deleted Payment - Sale Id #{existingPayment.SaleId}",
                                userId,
                                DateTimeHelper.Now
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reversing online payment transaction for deleted Payment ID: {PaymentId}", id);
                            TempData["WarningMessage"] = "Payment deleted but reversing the online transaction failed. Please verify account balances.";
                        }
                    }

                    TempData["SuccessMessage"] = "Payment deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete payment.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment");
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerBills(long customerId)
        {
            try
            {
                var bills = await _customerPaymentService.GetCustomerBillsAsync(customerId);
                return Json(new { success = true, bills = bills });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer bills for customer {CustomerId}", customerId);
                return Json(new { success = false, message = "Error fetching customer bills" });
            }
        }

        /// <summary>
        /// Export customer payments to Excel (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(long? customerId = null, long? saleId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new CustomerPaymentFilters
                {
                    CustomerId = customerId,
                    SaleId = saleId,
                    PaymentDateFrom = fromDate,
                    PaymentDateTo = toDate
                };
                const int exportPageSize = 100000;
                var model = await _customerPaymentService.GetAllPaymentsAsync(1, exportPageSize, filters);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Customer Payments");

                worksheet.Cell(1, 1).Value = "Payment Id";
                worksheet.Cell(1, 2).Value = "Customer Name";
                worksheet.Cell(1, 3).Value = "Bill Number";
                worksheet.Cell(1, 4).Value = "Amount";
                worksheet.Cell(1, 5).Value = "Payment Date";
                worksheet.Cell(1, 6).Value = "Description";
                worksheet.Cell(1, 7).Value = "Payment Method";
                worksheet.Cell(1, 8).Value = "Status";

                var headerRange = worksheet.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                decimal totalAmount = 0;
                foreach (var item in model.PaymentsList)
                {
                    worksheet.Cell(row, 1).Value = item.PaymentId;
                    worksheet.Cell(row, 2).Value = item.CustomerName ?? "";
                    worksheet.Cell(row, 3).Value = item.BillNumber;
                    worksheet.Cell(row, 4).Value = item.PaymentAmount;
                    worksheet.Cell(row, 5).Value = item.PaymentDate.ToString("dd-MMM-yyyy HH:mm");
                    worksheet.Cell(row, 6).Value = item.Description ?? "";
                    worksheet.Cell(row, 7).Value = item.PaymentMethod ?? "";
                    worksheet.Cell(row, 8).Value = item.IsDeleted == true ? "Deleted" : "Active";
                    totalAmount += item.PaymentAmount;
                    row++;
                }

                row++;
                worksheet.Cell(row, 3).Value = "TOTAL:";
                worksheet.Cell(row, 3).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Value = totalAmount;
                worksheet.Cell(row, 4).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                string filename = $"CustomerPayments_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customer payments to Excel");
                TempData["ErrorMessage"] = "An error occurred while exporting to Excel.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export customer payments to PDF (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(long? customerId = null, long? saleId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new CustomerPaymentFilters
                {
                    CustomerId = customerId,
                    SaleId = saleId,
                    PaymentDateFrom = fromDate,
                    PaymentDateTo = toDate
                };
                const int exportPageSize = 100000;
                var model = await _customerPaymentService.GetAllPaymentsAsync(1, exportPageSize, filters);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                document.Add(new Paragraph("Customer Payments Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                var table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 2.5f, 1.2f, 1.5f, 2f, 2.5f, 1.5f, 1f });

                string[] headers = { "Payment Id", "Customer Name", "Bill #", "Amount", "Payment Date", "Description", "Payment Method", "Status" };
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

                decimal totalAmount = 0;
                foreach (var p in model.PaymentsList)
                {
                    table.AddCell(p.PaymentId.ToString());
                    table.AddCell(p.CustomerName ?? "");
                    table.AddCell(p.BillNumber.ToString());
                    table.AddCell(p.PaymentAmount.ToString("N2"));
                    totalAmount += p.PaymentAmount;
                    table.AddCell(p.PaymentDate.ToString("dd-MMM-yyyy HH:mm"));
                    table.AddCell(p.Description ?? "");
                    table.AddCell(p.PaymentMethod ?? "");
                    table.AddCell(p.IsDeleted == true ? "Deleted" : "Active");
                }

                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                {
                    Colspan = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(totalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                for (int i = 0; i < 3; i++)
                    table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                string filename = $"CustomerPayments_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customer payments to PDF");
                TempData["ErrorMessage"] = "An error occurred while exporting to PDF.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
