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
                    billDateFrom = DateTime.Now;   //DateTime.Now.AddMonths(-1);  
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
                // Redirect to VendorBills Details action which has the full details view
                return RedirectToAction("Details", "VendorBills", new { id = id });
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

        // GET: VendorPaymentsController/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                // Get the bill data
                var vendorBill = await _vendorBillsService.GetVendorBillByPaymentIdAsync(id);
                if (vendorBill == null)
                {
                    TempData["ErrorMessage"] = "Bill not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Populate the form view model with bill payment information
                var viewModel = new VendorPaymentFormViewModel
                {
                    SupplierId = vendorBill.SupplierIdFk,
                    BillId = vendorBill.BillId,
                    PaymentAmount = vendorBill.PaymentAmount,
                    PaymentDate = vendorBill.PaymentDate,
                    Description = vendorBill.Description,
                    PaymentMethod = vendorBill.PaymentMethod,
                    OnlineAccountId = vendorBill.onlineAccountId
                };

                // Load vendor list
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
                if (!ModelState.IsValid)
                {
                    model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                    return View(model);
                }

                // Get the existing bill to check current payment info
                var existingBill = await _vendorBillsService.GetVendorBillByPaymentIdAsync(id);
                if (existingBill == null)
                {
                    TempData["ErrorMessage"] = "Bill not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get user ID from session
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = string.IsNullOrEmpty(userIdStr) ? 1 : long.Parse(userIdStr);

                // If payment method changed from Online to something else, or amount changed for Online payment
                // we need to handle the transaction reversal/update
                if (existingBill.PaymentMethod == "Online" && existingBill.onlineAccountId.HasValue)
                {
                    // If changing from Online to Cash or changing the amount, we need to reverse the old transaction
                    if (model.PaymentMethod != "Online" || model.PaymentAmount != existingBill.PaymentAmount)
                    {
                        try
                        {
                            // Reverse the old online payment transaction (credit back the amount)
                            await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                existingBill.onlineAccountId.Value,
                                id,
                                -existingBill.PaymentAmount, // Negative to reverse
                                $"Reversed payment for Bill #{existingBill.BillId}",
                                userId,
                                DateTime.Now
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reversing old online payment transaction for Bill ID: {BillId}", id);
                            // Continue with update even if reversal fails
                        }
                    }
                }

                // Update the bill's payment information using UpdateVendorBillAsync
                // We need to get the bill items first
                var billItems = await _vendorBillsService.GetVendorBillItemsAsync(id);
                
                // Create a minimal VendorBillGenerationViewModel for updating payment info
                var updateModel = new VendorBillGenerationViewModel
                {
                    BillId = id,
                    VendorId = model.SupplierId,
                    BillNumber = existingBill.BillId,
                    BillDate = existingBill.PaymentDate,
                    TotalAmount = existingBill.PaymentAmount,
                    DiscountAmount = existingBill.PaymentAmount,
                    PaidAmount = model.PaymentAmount,
                    DueAmount = existingBill.PaymentAmount - model.PaymentAmount,
                    Description = model.Description ?? existingBill.Description,
                    PaymentMethod = model.PaymentMethod,
                    OnlineAccountId = model.PaymentMethod == "Online" ? model.OnlineAccountId : null,
                    IsEditMode = true,
                    ModifiedBy = userId,
                    ModifiedDate = DateTime.Now,
                    BillDetails = billItems.Select(item => new VendorBillDetailViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductCode = item.ProductCode,
                        ProductSize = item.ProductSize,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        PurchasePrice = item.UnitPrice,
                        SalePrice = item.UnitPrice,
                        LineDiscountAmount = item.DiscountAmount,
                        PayableAmount = item.PayableAmount,
                        ProductRangeId = item.ProductRangeId
                    }).ToList()
                };

                var updateResult = await _vendorBillsService.UpdateVendorBillAsync(id, updateModel);

                if (updateResult)
                {
                    // If new payment method is Online, process the transaction
                    if (model.PaymentMethod == "Online" && model.OnlineAccountId.HasValue && model.OnlineAccountId > 0)
                    {
                        // Validate account balance
                        var accountBalance = await _vendorBillsService.GetAccountBalanceAsync(model.OnlineAccountId.Value);
                        
                        if (accountBalance <= 0)
                        {
                            _logger.LogWarning("Account balance validation failed - Balance is {Balance} for AccountId {AccountId}", accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Account balance is insufficient. Available balance: ${accountBalance:F2}";
                            model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                            return View(model);
                        }
                        
                        if (model.PaymentAmount > accountBalance)
                        {
                            _logger.LogWarning("Payment amount validation failed - PaymentAmount {PaymentAmount} exceeds balance {Balance} for AccountId {AccountId}", model.PaymentAmount, accountBalance, model.OnlineAccountId.Value);
                            TempData["ErrorMessage"] = $"Payment amount exceeds available account balance. Available balance: ${accountBalance:F2}, Payment amount: ${model.PaymentAmount:F2}";
                            model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                            return View(model);
                        }
                        
                        // Process online payment transaction
                        try
                        {
                            var transactionDescription = $"Updated Vendor Payment - Bill Id #{id} - {model.Description ?? ""}";
                            var transactionId = await _vendorService.ProcessOnlinePaymentTransactionAsync(
                                model.OnlineAccountId.Value,
                                id,
                                model.PaymentAmount, // Debit the payment amount from the online account
                                transactionDescription,
                                userId,
                                DateTime.Now
                            );

                            _logger.LogInformation("Online payment transaction processed successfully. Transaction ID: {TransactionId}, Bill ID: {BillId}",
                                transactionId, id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing online payment transaction for Bill ID: {BillId}", id);
                            TempData["WarningMessage"] = "Payment updated but online transaction processing failed. Please check the account balance.";
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Payment updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                
                TempData["ErrorMessage"] = "Failed to update payment.";
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                model.VendorList = await _vendorPaymentService.GetAllVendorsAsync();
                return View(model);
            }
        }

        // GET: VendorPaymentsController/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var vendorBill = await _vendorBillsService.GetVendorBillByPaymentIdAsync(id);
                if (vendorBill == null)
                {
                    TempData["ErrorMessage"] = "Bill not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Calculate TotalPayableAmount if not set
                if (vendorBill.PaymentAmount == 0)
                {
                    vendorBill.PaymentAmount = vendorBill.PaymentAmount;
                }

                return View(vendorBill);
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
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(long id, IFormCollection collection)
        {
            try
            {
                // Get user ID from session
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr ?? "1"); // Default to 1 if not found
                
                // Call service to delete the bill
                var result = await _vendorBillsService.DeleteVendorBillAsync(id, userId);
                
                if (result)
                {
                    _logger.LogInformation("Vendor bill deleted successfully with ID: {BillId}", id);
                    TempData["SuccessMessage"] = "Bill deleted successfully. Stock has been restored and transactions marked as deleted.";
                }
                else
                {
                    _logger.LogWarning("Failed to delete vendor bill with ID: {BillId}", id);
                    TempData["ErrorMessage"] = "Failed to delete bill.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment");
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
