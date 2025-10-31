using IMS.Common_Interfaces;
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
        private readonly ILogger<VendorPaymentsController> _logger;

        public VendorPaymentsController(IVendorPaymentService vendorPaymentService, IVendor vendorService, IVendorBillsService vendorBillsService, ILogger<VendorPaymentsController> logger)
        {
            _vendorPaymentService = vendorPaymentService;
            _vendorService = vendorService;
            _vendorBillsService = vendorBillsService;
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
                    billDateFrom = DateTime.Now.AddMonths(-1);
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
                    onlineAccountId: model.OnlineAccountId
                );

                if (result)
                {
                    // Validate account balance for Online payment method
                    if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                    {
                        var accountBalance = await _vendorBillsService.GetAccountBalanceAsync(model.OnlineAccountId.Value);
                        
                        if (accountBalance <= 0)
                        {
                            _logger.LogWarning("Account balance validation failed - Balance is {Balance} for AccountId {AccountId}", accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Account balance is insufficient. Available balance: ${accountBalance:F2}";
                            model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                            return View(viewName: "CreatePayment", model: model);
                        }
                        
                        if (model.PaymentAmount > accountBalance)
                        {
                            _logger.LogWarning("Payment amount validation failed - PaymentAmount {PaymentAmount} exceeds balance {Balance} for AccountId {AccountId}", model.PaymentAmount, accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Payment amount exceeds available account balance. Available balance: ${accountBalance:F2}, Payment amount: ${model.PaymentAmount:F2}";
                            model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                            return View(viewName: "CreatePayment", model: model);
                        }
                        
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

        // GET: VendorPaymentsController/GetPurchaseTransactionHistory
        [HttpGet]
        public async Task<JsonResult> GetPurchaseTransactionHistory(long personalPaymentId, int pageNumber = 1, int pageSize = 10, 
            DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            try
            {
                _logger.LogInformation("Getting purchase transaction history for PersonalPaymentId: {PersonalPaymentId}, Page: {PageNumber}", 
                    personalPaymentId, pageNumber);
                
                var result = await _vendorPaymentService.GetPurchaseTransactionHistoryAsync(
                    personalPaymentId, pageNumber, pageSize, fromDate, toDate, transactionType);
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase transaction history for PersonalPaymentId: {PersonalPaymentId}", personalPaymentId);
                return Json(new { 
                    success = false, 
                    message = "Error loading purchase transaction history: " + ex.Message,
                    transactions = new List<object>(),
                    accountSummary = new object()
                });
            }
        }

        // GET: VendorPaymentsController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                // Implementation for editing payment
                return View();
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
        public async Task<IActionResult> Edit(long id, IFormCollection collection)
        {
            try
            {
                // Implementation for updating payment
                TempData["SuccessMessage"] = "Payment updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                return View();
            }
        }

        // GET: VendorPaymentsController/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                // Implementation for delete confirmation
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation");
                TempData["ErrorMessage"] = "An error occurred while loading the delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorPaymentsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, IFormCollection collection)
        {
            try
            {
                // Implementation for deleting payment
                TempData["SuccessMessage"] = "Payment deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment");
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
                return View();
            }
        }
    }
}
