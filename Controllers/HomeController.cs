using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;


namespace IMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardService _dashboardService;
        private readonly IProductService _productService;


        public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService,IProductService productService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
            _productService = productService;
        }
      

        public async Task<IActionResult> Index()
        {
            // TODO: Replace this with real DB / service calls
            var productCount =  _dashboardService.GetTotalProductCount();
         var VendorCount   =_dashboardService.GetTotalVendorsCount();
          var categoryCount = _dashboardService.GetTotalCategoryCount();
            var last12monthsale = _dashboardService.GetLast12MonthsSalesAsync();
            var currentMonthRevenue = _dashboardService.GetCurrentMonthRevenueAsync();
            //var stockStatus= _dashboardService.GetStockStatusAsync();
            var enabledProducts = _productService.GetAllEnabledProductsAsync();
            await Task.WhenAll(productCount, VendorCount, categoryCount,
                last12monthsale, currentMonthRevenue, enabledProducts);
            
            var model = new DashboardViewModel
            {
                TotalProducts = productCount.Result,
                TotalCategories = categoryCount.Result,
                TotalVendors = VendorCount.Result,
                MonthlyRevenue = currentMonthRevenue.Result, // Use current month revenue instead of last month
                MonthlyLabels = last12monthsale.Result.Months , //new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
                MonthlySales = last12monthsale.Result.Sales,            //new List<decimal> { 4200, 3800, 5200, 6000, 4800, 5100, 7200, 6500, 7800,0,0,0 },
                ProductList = new List<SelectListItem>
                {
                    new SelectListItem{Value="0",Text="<--Select Product-->"}
                },

           
            

                TopProducts = new List<TopProductDto>
                {
                    new() { ProductId = 1, Name = "Product A", Sold = 320, Revenue = 6400 },
                    new() { ProductId = 2, Name = "Product B", Sold = 210, Revenue = 4200 },
                    new() { ProductId = 3, Name = "Product C", Sold = 150, Revenue = 3750 }
                },
                RecentTransactions = new List<RecentTransactionDto>
                {
                    new() { TransactionId = 1010, Type = "Sale", Party = "Customer X", Amount = 122.00m, Date = DateTime.Now.AddDays(-1) },
                    new() { TransactionId = 1009, Type = "Purchase", Party = "Vendor Y", Amount = 2360.00m, Date = DateTime.Now.AddDays(-5) },
                    new() { TransactionId = 1008, Type = "Sale", Party = "Customer Z", Amount = 950.00m, Date = DateTime.Now.AddDays(-7) }
                }
            };
            if (enabledProducts.Result.Any())
            {
                var firstProduct = enabledProducts.Result.First();
                var stockStatus= await _dashboardService.GetStockStatusAsync(firstProduct.ProductId);
                model.ProductList = enabledProducts.Result
                    .OrderBy(x => x.ProductName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.ProductId.ToString(),
                        Text = x.ProductName

                    }).ToList();
                var liststock=new StockStaus {
                    ProductId = firstProduct.ProductId,
                    ProductName = firstProduct.ProductName,
                    InStockCount = stockStatus.FirstOrDefault()?.InStockCount,
                    AvailableStockCount = stockStatus.FirstOrDefault()?.AvailableStockCount,
                    OutOfStockCount = stockStatus.FirstOrDefault()?.OutOfStockCount
                };
                model.StockStaus.Add(liststock);

                model.InStockCount = model.StockStaus[0].InStockCount;
                model.LowStockCount = model.StockStaus[0].AvailableStockCount;
                model.OutOfStockCount = model.StockStaus[0].OutOfStockCount;
            }
            

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetStockStatus(long? productId)
        {
            try
            {
                if (!productId.HasValue)
                {
                    return Json(new { error = "Product ID is required" });
                }

                var model = new DashboardViewModel();
                var result = await _dashboardService.GetStockStatusAsync(productId);

                var stockData = result.FirstOrDefault();
                if (stockData != null)
                {
                    model.InStockCount = stockData.InStockCount ?? 0;
                    model.LowStockCount = stockData.AvailableStockCount ?? 0;
                    model.OutOfStockCount = stockData.OutOfStockCount ?? 0;
                }
                else
                {
                    // Return default values if no data found
                    model.InStockCount = 0;
                    model.LowStockCount = 0;
                    model.OutOfStockCount = 0;
                }
                
                return Json(model);
            }
            catch (Exception )
            {
                // Log the exception (you might want to use a proper logging framework)
                return Json(new { 
                    error = "An error occurred while fetching stock data",
                    inStockCount = 0,
                    lowStockCount = 0,
                    outOfStockCount = 0
                });
            }
        }
        [HttpGet]
        public IActionResult Ping()
        {
            // Touch session to keep it alive
            HttpContext.Session.SetString("Ping", DateTime.Now.ToString());
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var currentMonthRevenue = await _dashboardService.GetCurrentMonthRevenueAsync();
                var last12MonthsSales = await _dashboardService.GetLast12MonthsSalesAsync();
                
                return Json(new 
                { 
                    success = true,
                    currentMonthRevenue = currentMonthRevenue,
                    monthlySales = last12MonthsSales.Sales,
                    monthlyLabels = last12MonthsSales.Months
                });
            }
            catch (Exception )
            {
                return Json(new { success = false, message = "Error fetching dashboard data" });
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
