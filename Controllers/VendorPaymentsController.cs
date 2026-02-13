using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
        public async Task<IActionResult> Index(int pageNumber = 1, int? pageSize = 10,
            long? vendorId = null, long? billNumber = null,
            DateTime? billDateFrom = null, DateTime? billDateTo = null, string? description = null)
        {
            try
            {
                if (billDateFrom == null && billDateTo ==null)
                {
                    billDateFrom = DateTime.Now;                //DateTime.Now.AddMonths(-1);
                    billDateTo = DateTime.Now;

                }
                var filters = new VendorPaymentFilters
                {
                    VendorId = vendorId,
                    BillNumber = billNumber,
                    BillDateFrom = billDateFrom,
                    BillDateTo = billDateTo,
                    Description = description
                };

                var viewModel = await _vendorPaymentService.GetAllBillPaymentsAsync(pageNumber, pageSize ?? 10, filters);
                viewModel.VendorList = await _vendorPaymentService.GetAllVendorsAsync();

                // Load bill numbers if vendor is selected
                if (vendorId.HasValue)
                {
                    ViewBag.BillNumbers = await _vendorPaymentService.GetSupplierBillNumbersAsync(vendorId.Value);
                }

                // Store filter values in ViewData for form persistence
                ViewData["vendorId"] = vendorId;
                ViewData["billNumber"] = billNumber;
                ViewData["billDateFrom"] = billDateFrom?.ToString("yyyy-MM-dd");
                ViewData["billDateTo"] = billDateTo?.ToString("yyyy-MM-dd");
                ViewData["description"] = description;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vendor payments");
                TempData["ErrorMessage"] = "An error occurred while loading vendor payments.";
                return View(new VendorPaymentViewModel());
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
                    createdDate: DateTime.Now,
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
                                DateTime.Now
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

                            var customerPaymentDescription = $"General Payment from Vendor - {model.Description ?? "Direct payment"}";
                            var customerPayment = new Payment
                            {
                                PaymentAmount = model.PaymentAmount,
                                SaleId = saleId,
                                CustomerId = model.CustomerId.Value,
                                PaymentDate = model.PaymentDate,
                                paymentMethod = "General Payments",
                                onlineAccountId = null,
                                CreatedBy = createdBy,
                                CreatedDate = DateTime.Now,
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
                                    DateTime.Now
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
                                    DateTime.Now
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

                var result = await _vendorPaymentService.DeletePaymentAsync(id, DateTime.Now, userId);
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
                                DateTime.Now
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
    }
}
