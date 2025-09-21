using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly ILogger<SalesController> _logger;
        private readonly IProductService _productService;
        private readonly ICustomer _customerService;

        public SalesController(ISalesService salesService, ILogger<SalesController> logger, IProductService productService, ICustomer customerService)
        {
            _salesService = salesService;
            _logger = logger;
            _productService = productService;
            _customerService = customerService;
        }

        // GET: SalesController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string? searchCustomer = null, 
            long? customerId = null, long? billNumber = null, DateTime? saleFrom = null, DateTime? saleDateTo = null, 
            string? description = null)
        {
            try
            {
                var currentPageSize = pageSize ?? HttpContext.Session.GetInt32("PageSize") ?? 10;
                HttpContext.Session.SetInt32("PageSize", currentPageSize);

                // Check if there are any sales records at all
                var hasAnySales = await _salesService.HasAnySalesAsync();
                if (!hasAnySales)
                {
                    TempData["InfoMessage"] = "No sales records found in the database. You can create your first sale using the 'Add New Sale' button.";
                    return View(new SalesViewModel());
                }

                var stockFilters = new SalesFilters();
                if (!string.IsNullOrEmpty(searchCustomer))
                {
                    stockFilters.CustomerId = customerId;
                }
                else if (customerId.HasValue)
                {
                    stockFilters.CustomerId = customerId;
                }
                if (billNumber.HasValue)
                {
                    stockFilters.BillNumber = billNumber;
                }
                if (saleFrom.HasValue)
                {
                    stockFilters.SaleFrom = saleFrom;
                }
                if (saleDateTo.HasValue)
                {
                    stockFilters.SaleDateTo = saleDateTo;
                }
                if (!string.IsNullOrEmpty(description))
                {
                    stockFilters.Description = description;
                }

                var viewModel = await _salesService.GetAllSalesAsync(pageNumber, currentPageSize, stockFilters);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new SalesViewModel());
            }
        }

        // GET: SalesController/Details/5
        public async Task<ActionResult> Details(long id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }
                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SalesController/Create
        public async Task<ActionResult> Create()
        {
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
                
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View();
        }

        // POST: SalesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Sale sale)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    sale.CreatedBy = userId;
                    sale.CreatedDate = DateTime.Now;
                    sale.ModifiedBy = userId;
                    sale.ModifiedDate = DateTime.Now;

                    var result = await _salesService.CreateSaleAsync(sale);
                    if (result)
                    {
                        TempData["Success"] = "Sale created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create sale.";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            
            // Ensure ViewBag is populated when returning View
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
                
                var nextBillNumber = await _salesService.GetNextBillNumberAsync();
                ViewBag.NextBillNumber = nextBillNumber;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
            }
            
            return View();
        }

        // GET: SalesController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }

                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", sale.CustomerIdFk);

                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, Sale sale)
        {
            if (id != sale.SaleId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    sale.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    sale.ModifiedBy = userId;

                    var response = await _salesService.UpdateSaleAsync(sale);
                    
                    if (response != 0)
                    {
                        TempData["Success"] = "Sale updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update sale.";
                        return View(sale);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            
            // Ensure ViewBag is populated when returning View
            try
            {
                var customers = await _salesService.GetAllCustomersAsync();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", sale.CustomerIdFk);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
            }
            
            return View(sale);
        }

        // GET: SalesController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }
                return View(sale);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr);
                var modifiedDate = DateTime.Now;
                
                var res = await _salesService.DeleteSaleAsync(id, modifiedDate, userId);
                if (res != 0)
                {
                                            TempData["Success"] = "Sale deleted successfully!";
                        return RedirectToAction(nameof(Index));
                }
                else
                {
                                            TempData["ErrorMessage"] = "Failed to delete sale.";
                        return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult AddSale()
        {
            _productService.GetAllEnabledProductsAsync().ContinueWith(task =>
            {
                var products = task.Result;
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName");
            }).Wait();

        //    ViewBag.Products = new SelectList(new List<SelectListItem>
        //{
        //    new SelectListItem { Value = "", Text = "--Select a value--" }
        //}, "Value", "Text");

            ViewBag.ProductSizes = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "--Select a value--" }
        }, "Value", "Text");
            _customerService.GetAllEnabledCustomers().ContinueWith(task =>
            {
                var customers = task.Result;
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
            }).Wait();

            //    ViewBag.Customers = new SelectList(new List<SelectListItem>
            //{
            //    new SelectListItem { Value = "", Text = "--Select a value--" }
            //}, "Value", "Text");
           
            return View(new AddSaleViewModel { SaleDate = DateTime.Now });
        }
        [HttpGet]
        public async Task<JsonResult> GetProductSizes(long productId)
        {
            
           var response= _productService.GetProductRangeByProdId(productId);

            return Json(response);
        }
        //[HttpPost]
        //public async Task<IActionResult> AddSale(AddSaleViewModel model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            int returnValue;
        //            DateTime currentDateTime = DateTime.Now; // 04:11 PM PKT on September 18, 2025

        //            // Add the sale header
        //            long saleId = _salesService.CreateSaleAsync(
        //                totalAmount: model.TotalAmount,
        //                totalReceivedAmount: model.ReceivedAmount,
        //                totalDueAmount: model.DueAmount,
        //                customerId: model.CustomerId ?? 0,
        //                createdDate: currentDateTime,
        //                createdBy: 1, // Replace with actual user ID
        //                modifiedDate: currentDateTime,
        //                modifiedBy: 1, // Replace with actual user ID
        //                discountAmount: model.DiscountAmount,
        //                billNumber: long.Parse(model.BillNo ?? "0"),
        //                saleDescription: model.Description ?? "",
        //                saleDate: model.SaleDate,
        //                out returnValue
        //            );

        //            // Add sale details for each product
        //            foreach (var detail in model.SaleDetails)
        //            {
        //                long saleDetailsId = _saleService.AddSaleDetails(
        //                    saleId: saleId,
        //                    productId: detail.ProductId,
        //                    unitPrice: detail.UnitPrice,
        //                    quantity: detail.Quantity,
        //                    salePrice: detail.SalePrice,
        //                    lineDiscountAmount: detail.LineDiscountAmount,
        //                    payableAmount: detail.PayableAmount,
        //                    productRangeId: 111697, // Replace with actual range ID if dynamic
        //                    out returnValue
        //                );

        //                // Update stock and transaction for each product
        //                int stockReturn = _saleService.GetStockByProductId(detail.ProductId);
        //                int updateStockReturn = _saleService.UpdateStock(
        //                    stockMasterId: 50235, // Replace with dynamic stock ID
        //                    productId: detail.ProductId,
        //                    availableQuantity: 2.000m, // Replace with actual available quantity
        //                    usedQuantity: (decimal)detail.Quantity,
        //                    modifiedBy: 1,
        //                    modifiedDate: currentDateTime
        //                );

        //                int transactionReturn = _saleService.SaleTransactionCreate(
        //                    stockMasterId: 50235, // Replace with dynamic stock ID
        //                    quantity: (decimal)detail.Quantity,
        //                    comment: "",
        //                    createdDate: currentDateTime,
        //                    createdBy: 1,
        //                    transactionStatusId: 2,
        //                    saleId: saleId
        //                );
        //            }

        //            return RedirectToAction("Index");
        //        }
        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = ex.Message;
        //        return View(model);
        //    }

        //}

    }
}
