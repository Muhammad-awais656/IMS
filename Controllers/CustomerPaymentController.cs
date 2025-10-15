using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;
using static IMS.Models.CustomerPaymentViewModel;

namespace IMS.Controllers
{
    public class CustomerPaymentController : Controller
    {
        private readonly ICustomerPaymentService _customerPaymentService;
        private readonly ILogger<CustomerPaymentController> _logger;

        public CustomerPaymentController(ICustomerPaymentService customerPaymentService, ILogger<CustomerPaymentController> logger)
        {
            _customerPaymentService = customerPaymentService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int? pageSize = 10,
            long? customerId = null, long? saleId = null,
            DateTime? paymentDateFrom = null, DateTime? paymentDateTo = null)
        {
            try
            {
                var filters = new CustomerPaymentFilters
                {
                    CustomerId = customerId,
                    SaleId = saleId,
                    PaymentDateFrom = paymentDateFrom,
                    PaymentDateTo = paymentDateTo
                };
                if (filters.PaymentDateFrom == null && filters.PaymentDateTo==null)
                {
                    filters.PaymentDateFrom = DateTime.Now;
                    filters.PaymentDateTo = DateTime.Now;
                }
                var viewModel = await _customerPaymentService.GetAllPaymentsAsync(pageNumber, pageSize ?? 10, filters);

                // Store filter values in ViewData for form persistence
                ViewData["customerId"] = customerId;
                ViewData["saleId"] = saleId;
                ViewData["paymentDateFrom"] = paymentDateFrom?.ToString("yyyy-MM-dd");
                ViewData["paymentDateTo"] = paymentDateTo?.ToString("yyyy-MM-dd");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments index");
                TempData["ErrorMessage"] = "An error occurred while loading payments.";
                return View(new CustomerPaymentViewModel());
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
                if (ModelState.IsValid)
                {
                    var payment = new Payment
                    {
                        PaymentAmount = model.PaymentAmount,
                        SaleId = model.SaleId,
                        CustomerId = model.CustomerId,
                        PaymentDate = model.PaymentDate,
                        CreatedBy = 1, // TODO: Get from current user session
                        CreatedDate = DateTime.Now,
                        Description = model.Description
                    };

                    var result = await _customerPaymentService.CreatePaymentAsync(payment);
                    if (result)
                    {
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
                    Description = payment.Description
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
                if (ModelState.IsValid)
                {
                    var payment = new Payment
                    {
                        PaymentId = model.PaymentId,
                        PaymentAmount = model.PaymentAmount,
                        SaleId = model.SaleId,
                        CustomerId = model.CustomerId,
                        PaymentDate = model.PaymentDate,
                        ModifiedBy = 1, // TODO: Get from current user session
                        ModifiedDate = DateTime.Now,
                        Description = model.Description
                    };

                    var result = await _customerPaymentService.UpdatePaymentAsync(payment);
                    if (result > 0)
                    {
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
                var result = await _customerPaymentService.DeletePaymentAsync(id, DateTime.Now, 1); // TODO: Get from current user session
                if (result > 0)
                {
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
        public async Task<IActionResult> GetCustomerBills(long customerId)
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
    }
}
