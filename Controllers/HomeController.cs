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
            //var stockStatus= _dashboardService.GetStockStatusAsync();
            var enabledProducts = _productService.GetAllEnabledProductsAsync();
            await Task.WhenAll(productCount, VendorCount, categoryCount,
                last12monthsale, enabledProducts);
            
            var model = new DashboardViewModel
            {
                TotalProducts = productCount.Result,
                TotalCategories = categoryCount.Result,
                TotalVendors = VendorCount.Result,
                MonthlyRevenue = last12monthsale.Result.Sales.LastOrDefault(),//48250.75m,
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
                model.ProductList = enabledProducts.Result.Select(x => new SelectListItem
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
            var model = new DashboardViewModel();
            var result = await _dashboardService.GetStockStatusAsync(productId);

            model.InStockCount = result.FirstOrDefault()?.InStockCount;
            model.LowStockCount = result.FirstOrDefault()?.AvailableStockCount;
            model.OutOfStockCount = result.FirstOrDefault()?.OutOfStockCount;
            
            return Json(model);
        }
        [HttpGet]
        public IActionResult Ping()
        {
            // Touch session to keep it alive
            HttpContext.Session.SetString("Ping", DateTime.Now.ToString());
            return Ok();
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
