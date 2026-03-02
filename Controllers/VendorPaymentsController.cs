using IMS.CommonUtilities;
using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class VendorPaymentsController : Controller
    {
        private readonly IVendorPaymentService _vendorPaymentService;
        private readonly IVendor _vendorService;
        private readonly IVendorBillsService _vendorBillsService;
        private readonly ICustomerPaymentService _customerPaymentService;
        private readonly ILogger<VendorPaymentsController> _logger;

        public VendorPaymentsController(IVendorPaymentService vendorPaymentService, IVendor vendorService, IVendorBillsService vendorBillsService, ICustomerPaymentService customerPaymentService, ILogger<VendorPaymentsController> logger)
        {
            _vendorPaymentService = vendorPaymentService;
            _vendorService = vendorService;
            _vendorBillsService = vendorBillsService;
            _customerPaymentService = customerPaymentService;
            _logger = logger;
        }

        // GET: VendorPaymentsController
        public async Task<IActionResult> Index(VendorPaymentViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new VendorPaymentViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new VendorPaymentFilters();
                }

                // Don't set default dates - let them be null to show all payments on first load

                var currentPageSize = pageSize ?? 10;
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _vendorPaymentService.GetAllBillPaymentsAsync(pageNumber, currentPageSize, filters);
                
                // Reassign filters to ensure they're preserved
                model.Filters = filters;
                
                // Load vendors
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();

                // Load bill numbers if vendor is selected
                if (model.Filters.VendorId.HasValue)
                {
                    ViewBag.BillNumbers = await _vendorPaymentService.GetSupplierBillNumbersAsync(model.Filters.VendorId.Value);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vendor payments");
                TempData["ErrorMessage"] = "An error occurred while loading vendor payments.";
                if (model == null)
                {
                    model = new VendorPaymentViewModel();
                }
                return View(model);
            }
        }

        // AJAX endpoint to get vendors for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                var vendors = await _vendorPaymentService.GetAllVendorsAsync();
                var result = vendors.Select(v => new
                {
                    value = v.SupplierId.ToString(),
                    text = v.SupplierName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors for Kendo combobox");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get bill numbers for a supplier
        [HttpGet]
        public async Task<IActionResult> GetBillNumbers(long supplierId)
        {
            try
            {
                var billNumbers = await _vendorPaymentService.GetSupplierBillNumbersAsync(supplierId);
                return Json(billNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill numbers for supplier {SupplierId}", supplierId);
                return Json(new List<SupplierBillNumber>());
            }
        }

        // GET: VendorPaymentsController/Details/5
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                // For now, redirect to index with the bill ID as a filter
                return RedirectToAction(nameof(Index), new { billNumber = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bill details");
                TempData["ErrorMessage"] = "An error occurred while loading bill details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VendorPaymentsController/CreatePayment
        public async Task<IActionResult> CreatePayment()
        {
            try
            {
                var viewModel = new VendorPaymentFormViewModel();
                viewModel.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(viewName: "CreatePayment", model: viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/CreatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(VendorPaymentFormViewModel model)
        {
            try
            {
                // Remove problematic SaleDetails validation errors from ModelState since we handle them separately
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("BillId"))
                    {
                        keysToRemove.Add(key);
                    }
                 
                }
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }
                if (!ModelState.IsValid)
                {
                    model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                    return View(viewName: "CreatePayment", model: model);
                }

                var userIdStr = HttpContext.Session.GetString("UserId");
                long createdBy = string.IsNullOrEmpty(userIdStr) ? 1 : long.Parse(userIdStr);

                var result = await _vendorPaymentService.CreateVendorPaymentAsync(
                    paymentAmount: model.PaymentAmount,
                    billId: model.BillId,
                    supplierId: model.SupplierId,
                    paymentDate: model.PaymentDate,
                    createdBy: createdBy,
                    createdDate: DateTimeHelper.Now,
                    description: model.Description,
                    paymentMethod: model.PaymentMethod,
                    onlineAccountId: model.OnlineAccountId,
                    customerId: null
                );

                if (result)
                {
                    // Validate account balance for Online payment method
                    if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                    {
                        var accountBalance = await _vendorBillsService.GetAccountBalanceAsync(model.OnlineAccountId.Value);
                        
                        //if (accountBalance <= 0)
                        //{
                        //    _logger.LogWarning("Account balance validation failed - Balance is {Balance} for AccountId {AccountId}", accountBalance, model.OnlineAccountId.Value);
                        //    TempData["ErrorMessage"] = $"Account balance is insufficient. Available balance: ${accountBalance:F2}";
                        //    model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                        //    return View(viewName: "CreatePayment", model: model);
                        //}
                        
                        //if (model.PaymentAmount > accountBalance)
                        //{
                        //    _logger.LogWarning("Payment amount validation failed - PaymentAmount {PaymentAmount} exceeds balance {Balance} for AccountId {AccountId}", model.PaymentAmount, accountBalance, model.OnlineAccountId.Value);
                        //    TempData["ErrorMessage"] = $"Payment amount exceeds available account balance. Available balance: ${accountBalance:F2}, Payment amount: ${model.PaymentAmount:F2}";
                        //    model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                        //    return View(viewName: "CreatePayment", model: model);
                        //}
                        
                        // Process online payment transaction
                        try
                        {
                            var transactionDescription = $"Added Vendor Payment - Bill Id #{model.BillId} - {model.Description ?? ""}";
                            var transactionId = await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                model.OnlineAccountId.Value,
                                model.BillId,
                                model.PaymentAmount, // Debit the payment amount from the online account
                                transactionDescription,
                                createdBy,
                                DateTimeHelper.Now
                            );

                            _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Bill ID: {BillId}",
                                transactionId, model.BillId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing online payment transaction for Bill ID: {BillId}", model.BillId);
                            // Don't fail the entire payment if online payment processing fails
                            // Just log the error and continue
                        }
                    }

                    // Process General Payments - deduct from vendor, credit to customer
                    if (model.PaymentMethod == "General Payments" && model.CustomerId.HasValue && model.CustomerId > 0)
                    {
                        try
                        {
                            // Find the first sale with due amount for the customer, or use 0 if no sale found
                            //var customerSales = await _customerPaymentService.GetAllSalesAsync();
                            long saleId = 0;
                            //if (customerSales != null && customerSales.Any(s => s.CustomerIdFk == model.CustomerId.Value && s.TotalDueAmount > 0))
                            //{
                            //    saleId = customerSales.First(s => s.CustomerIdFk == model.CustomerId.Value && s.TotalDueAmount > 0).SaleId;
                            //}

                            var customerPaymentDescription = $"General Payment from Vendor({model.SupplierId}) - {model.Description ?? "Direct payment"}";
                            var customerPayment = new Payment
                            {
                                PaymentAmount = model.PaymentAmount,
                                SaleId = saleId,
                                CustomerId = model.CustomerId.Value,
                                PaymentDate = model.PaymentDate,
                                paymentMethod = "General Payments",
                                onlineAccountId = null,
                                CreatedBy = createdBy,
                                CreatedDate = DateTimeHelper.Now,
                                Description = customerPaymentDescription,
                                SupplierId = null
                            };

                            var customerPaymentResult = await _customerPaymentService.CreatePaymentAsync(customerPayment);

                            if (customerPaymentResult)
                            {
                                _logger.LogInformation("General Payment processed successfully. Vendor Payment ID: {PaymentId}, Customer ID: {CustomerId}, Amount: {Amount}",
                                    result, model.CustomerId.Value, model.PaymentAmount);
                            }
                            else
                            {
                                _logger.LogWarning("Vendor payment created but customer payment failed for General Payment. Vendor Payment ID: {PaymentId}, Customer ID: {CustomerId}",
                                    result, model.CustomerId.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing General Payment customer credit. Vendor Payment ID: {PaymentId}, Customer ID: {CustomerId}",
                                result, model.CustomerId.Value);
                            // Don't fail the entire payment if customer payment processing fails
                            // Just log the error and continue
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Payment created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "Failed to create payment.";
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(viewName: "CreatePayment", model: model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                TempData["ErrorMessage"] = "An error occurred while creating the payment.";
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(viewName: "CreatePayment", model: model);
            }
        }

        // AJAX endpoint to get online accounts
        [HttpGet]
        public async Task<JsonResult> GetOnlineAccounts()
        {
            try
            {
                var onlineAccounts = await _vendorService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                var accountOptions = onlineAccounts.PersonalPaymentList.Select(account => new
                {
                    value = account.PersonalPaymentId.ToString(),
                    text = $"{account.BankName} - {account.AccountNumber}",
                    personalPaymentId = account.PersonalPaymentId,
                    bankName = account.BankName,
                    accountNumber = account.AccountNumber,
                    accountHolderName = account.AccountHolderName
                }).ToList();

                return Json(accountOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online accounts");
                return Json(new List<object>());
            }
        }

        // AJAX endpoint to get account balance
        [HttpGet]
        public async Task<JsonResult> GetAccountBalance(long accountId)
        {
            try
            {
                _logger.LogInformation("Getting account balance for account: {AccountId}", accountId);
                
                var currentBalance = await _vendorBillsService.GetAccountBalanceAsync(accountId);
                
                _logger.LogInformation("Account balance retrieved: {Balance} for account: {AccountId}", currentBalance, accountId);
                
                return Json(new { success = true, balance = currentBalance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account balance for account: {AccountId}", accountId);
                return Json(new { success = false, message = "Error retrieving account balance", balance = 0 });
            }
        }

        // GET: VendorPaymentsController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var payment = await _vendorPaymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new VendorPaymentFormViewModel
                {
                    PaymentId = payment.PaymentId,
                    SupplierId = payment.SupplierIdFk,
                    BillId = payment.BillId,
                    PaymentAmount = payment.PaymentAmount,
                    PaymentDate = payment.PaymentDate,
                    Description = payment.Description,
                    PaymentMethod = payment.PaymentMethod,
                    OnlineAccountId = payment.onlineAccountId,
                    CustomerId = payment.CustomerId,
                    CustomerName = payment.CustomerName
                };
                viewModel.VendorList = await _vendorPaymentService.GetAllVendorsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit payment form");
                TempData["ErrorMessage"] = "An error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, VendorPaymentFormViewModel model)
        {
            try
            {
                var keysToRemove = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    if (key.StartsWith("BillId"))
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
                    var existingPayment = await _vendorPaymentService.GetPaymentByIdAsync(id);
                    if (existingPayment == null)
                    {
                        TempData["ErrorMessage"] = "Payment not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    var existingOnline = string.Equals(existingPayment.PaymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && existingPayment.onlineAccountId.HasValue
                        && existingPayment.onlineAccountId > 0;
                    var newOnline = string.Equals(model.PaymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && model.OnlineAccountId.HasValue
                        && model.OnlineAccountId > 0;
                    var paymentChangedForOnline = existingPayment.PaymentAmount != model.PaymentAmount
                        || existingPayment.onlineAccountId != model.OnlineAccountId
                        || existingPayment.BillId != model.BillId;
                    var requiresReverse = existingOnline && (!newOnline || paymentChangedForOnline);
                    var requiresNewTransaction = newOnline && (!existingOnline || paymentChangedForOnline);

                    var payment = new BillPayment
                    {
                        PaymentId = id,
                        PaymentAmount = model.PaymentAmount,
                        BillId = model.BillId,
                        SupplierIdFk = model.SupplierId,
                        PaymentDate = model.PaymentDate,
                        PaymentMethod = model.PaymentMethod,
                        onlineAccountId = newOnline ? model.OnlineAccountId : null,
                        Description = model.Description,
                        CreatedBy = existingPayment.CreatedBy,
                        CreatedDate = existingPayment.CreatedDate
                    };

                    var result = await _vendorPaymentService.UpdatePaymentAsync(payment);
                    if (result > 0)
                    {
                        if (requiresReverse)
                        {
                            try
                            {
                                await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                    existingPayment.onlineAccountId!.Value,
                                    existingPayment.BillId,
                                    existingPayment.PaymentAmount, // Reverse: add back the amount
                                    $"Reversed payment for Bill #{existingPayment.BillId}",
                                    userId,
                                    DateTimeHelper.Now
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error reversing online payment transaction for Payment ID: {PaymentId}", id);
                                TempData["WarningMessage"] = "Payment updated but reversing the old online transaction failed. Please verify account balances.";
                            }
                        }

                        if (requiresNewTransaction)
                        {
                            try
                            {
                                var transactionDescription = $"Updated Payment - Bill Id #{model.BillId} - {model.Description ?? ""}";
                                var transactionId = await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                    model.OnlineAccountId!.Value,
                                    model.BillId,
                                    -model.PaymentAmount, // Debit: subtract the payment amount
                                    transactionDescription,
                                    userId,
                                    DateTimeHelper.Now
                                );

                                _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Bill ID: {BillId}",
                                    transactionId, model.BillId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing online payment transaction for Payment ID: {PaymentId}", id);
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
                    model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
            }
            ViewBag.PaymentId = id;
            return View(model);
        }

        // GET: VendorPaymentsController/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var payment = await _vendorPaymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation");
                TempData["ErrorMessage"] = "An error occurred while loading the delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = string.IsNullOrEmpty(userIdStr) ? 1 : long.Parse(userIdStr);

                var existingPayment = await _vendorPaymentService.GetPaymentByIdAsync(id);
                if (existingPayment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _vendorPaymentService.DeletePaymentAsync(id, DateTimeHelper.Now, userId);
                if (result > 0)
                {
                    var isOnline = string.Equals(existingPayment.PaymentMethod, "Online", StringComparison.OrdinalIgnoreCase)
                        && existingPayment.onlineAccountId.HasValue
                        && existingPayment.onlineAccountId > 0;
                    if (isOnline)
                    {
                        try
                        {
                            await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                existingPayment.onlineAccountId.Value,
                                existingPayment.BillId,
                                existingPayment.PaymentAmount, // Reverse: add back the amount
                                $"Deleted Payment - Bill Id #{existingPayment.BillId}",
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

        /// <summary>
        /// Export vendor payments to Excel (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel(long? vendorId = null, long? billNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new VendorPaymentFilters
                {
                    VendorId = vendorId,
                    BillNumber = billNumber,
                    BillDateFrom = fromDate,
                    BillDateTo = toDate
                };
                const int exportPageSize = 100000;
                var model = await _vendorPaymentService.GetAllBillPaymentsAsync(1, exportPageSize, filters);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Vendor Payments");

                worksheet.Cell(1, 1).Value = "Payment Id";
                worksheet.Cell(1, 2).Value = "Vendor";
                worksheet.Cell(1, 3).Value = "Bill #";
                worksheet.Cell(1, 4).Value = "Bill Date";
                worksheet.Cell(1, 5).Value = "Total Amount";
                worksheet.Cell(1, 6).Value = "Discount Amount";
                worksheet.Cell(1, 7).Value = "Paid Amount";
                worksheet.Cell(1, 8).Value = "Total Payable Amount";
                worksheet.Cell(1, 9).Value = "Description";
                worksheet.Cell(1, 10).Value = "Payment Method";
                worksheet.Cell(1, 11).Value = "Status";

                var headerRange = worksheet.Range(1, 1, 1, 11);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var item in model.BillsList)
                {
                    worksheet.Cell(row, 1).Value = item.PaymentId;
                    worksheet.Cell(row, 2).Value = item.VendorName ?? "";
                    worksheet.Cell(row, 3).Value = item.BillNumber;
                    worksheet.Cell(row, 4).Value = item.BillDate.ToString("dd-MMM-yyyy");
                    worksheet.Cell(row, 5).Value = item.TotalAmount;
                    worksheet.Cell(row, 6).Value = item.DiscountAmount;
                    worksheet.Cell(row, 7).Value = item.PaidAmount;
                    worksheet.Cell(row, 8).Value = item.TotalPayableAmount;
                    worksheet.Cell(row, 9).Value = item.Description ?? "";
                    worksheet.Cell(row, 10).Value = item.PaymentMethod ?? "";
                    worksheet.Cell(row, 11).Value = item.IsDeleted == true ? "Deleted" : "Active";
                    row++;
                }

                row++;
                worksheet.Cell(row, 4).Value = "TOTAL:";
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 5).Value = model.TotalAmount;
                worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Value = model.TotalDiscountAmount;
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Value = model.TotalPaidAmount;
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Value = model.TotalPayableAmount;
                worksheet.Cell(row, 8).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                string filename = $"VendorPayments_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting vendor payments to Excel");
                TempData["ErrorMessage"] = "An error occurred while exporting to Excel.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export vendor payments to PDF (same filters as Index).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(long? vendorId = null, long? billNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filters = new VendorPaymentFilters
                {
                    VendorId = vendorId,
                    BillNumber = billNumber,
                    BillDateFrom = fromDate,
                    BillDateTo = toDate
                };
                const int exportPageSize = 100000;
                var model = await _vendorPaymentService.GetAllBillPaymentsAsync(1, exportPageSize, filters);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                document.Add(new Paragraph("Vendor Payments Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                var table = new PdfPTable(11);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 2.5f, 1.2f, 1.5f, 1.5f, 1.3f, 1.3f, 1.5f, 2.5f, 1.5f, 1f });

                string[] headers = { "Payment Id", "Vendor", "Bill #", "Bill Date", "Total Amount", "Discount", "Paid Amount", "Payable", "Description", "Payment Method", "Status" };
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

                foreach (var p in model.BillsList)
                {
                    table.AddCell(p.PaymentId.ToString());
                    table.AddCell(p.VendorName ?? "");
                    table.AddCell(p.BillNumber.ToString());
                    table.AddCell(p.BillDate.ToString("dd-MMM-yyyy"));
                    table.AddCell(p.TotalAmount.ToString("N2"));
                    table.AddCell(p.DiscountAmount.ToString("N2"));
                    table.AddCell(p.PaidAmount.ToString("N2"));
                    table.AddCell(p.TotalPayableAmount.ToString("N2"));
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
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDiscountAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalPaidAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalPayableAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                for (int i = 0; i < 4; i++)
                    table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                string filename = $"VendorPayments_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting vendor payments to PDF");
                TempData["ErrorMessage"] = "An error occurred while exporting to PDF.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
