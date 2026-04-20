using IMS.CommonUtilities;
using IMS.Common_Interfaces;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;


namespace IMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardService _dashboardService;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService, IProductService productService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
            _productService = productService;
        }

        /// <summary>Admins see all branches; other users are scoped to session <c>BranchId</c> (set at login).</summary>
        private (bool IsAdmin, int? BranchId) ResolveDashboardScope()
        {
            var isAdmin = bool.TryParse(HttpContext.Session.GetString("IsAdmin"), out var ia) && ia;
            if (isAdmin)
                return (true, null);
            return (false, HttpContext.Session.GetInt32("BranchId"));
        }

        private static (DateTime From, DateTime To) ResolveAnalyticsRange(string? preset, DateTime? from, DateTime? to)
        {
            var today = DateTimeHelper.Today;
            preset = (preset ?? "today").Trim().ToLowerInvariant();
            switch (preset)
            {
                case "today":
                    return (today, today);
                case "yesterday":
                    var y = today.AddDays(-1);
                    return (y, y);
                case "last7":
                    return (today.AddDays(-6), today);
                case "last30":
                    return (today.AddDays(-29), today);
                case "thismonth":
                    return (new DateTime(today.Year, today.Month, 1), today);
                case "custom" when from.HasValue && to.HasValue:
                    var f = from.Value.Date;
                    var t = to.Value.Date;
                    if (f > t)
                        (f, t) = (t, f);
                    return (f, t);
                default:
                    return (today, today);
            }
        }

        public async Task<IActionResult> Index(string? range = "today", DateTime? from = null, DateTime? to = null)
        {
            var (isAdmin, branchId) = ResolveDashboardScope();
            var (fromDate, toDate) = ResolveAnalyticsRange(range, from, to);
            var summary = await _dashboardService.GetAnalyticsSummaryAsync(fromDate, toDate, branchId);
            var trend = await _dashboardService.GetSalesTrendAsync(fromDate, toDate, branchId);
            var products = await _productService.GetAllEnabledProductsAsync(branchId);

            var model = new AnalyticsDashboardPageModel
            {
                Summary = summary,
                SalesTrend = trend,
                FromDate = fromDate,
                ToDate = toDate,
                Preset = range ?? "today",
                IsAdmin = isAdmin,
                BranchName = HttpContext.Session.GetString("BranchName"),
                StockProducts = products.Select(p => new StockProductOption
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductName
                }).ToList()
            };

            return View(model);
        }

        /// <summary>JSON endpoint for the analytics dashboard (date presets and custom range).</summary>
        [HttpGet]
        public async Task<IActionResult> AnalyticsData(string? range = "today", DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var (_, branchId) = ResolveDashboardScope();
                var (fromDate, toDate) = ResolveAnalyticsRange(range, from, to);
                var summary = await _dashboardService.GetAnalyticsSummaryAsync(fromDate, toDate, branchId);
                var trend = await _dashboardService.GetSalesTrendAsync(fromDate, toDate, branchId);
                return Json(new
                {
                    success = true,
                    summary,
                    trend,
                    from = fromDate.ToString("yyyy-MM-dd"),
                    to = toDate.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyticsData failed");
                return Json(new { success = false, message = "Could not load analytics." });
            }
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
                var (_, branchId) = ResolveDashboardScope();
                var result = await _dashboardService.GetStockStatusAsync(productId, branchId);

                var stockData = result.FirstOrDefault();
                if (stockData != null)
                {
                    model.InStockCount = stockData.InStockCount ?? 0;
                    model.LowStockCount = stockData.AvailableStockCount ?? 0;
                    model.OutOfStockCount = stockData.OutOfStockCount ?? 0;
                }
                else
                {
                    model.InStockCount = 0;
                    model.LowStockCount = 0;
                    model.OutOfStockCount = 0;
                }

                return Json(model);
            }
            catch (Exception)
            {
                return Json(new
                {
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
            HttpContext.Session.SetString("Ping", DateTimeHelper.Now.ToString());
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var (_, branchId) = ResolveDashboardScope();
                var currentMonthRevenue = await _dashboardService.GetCurrentMonthRevenueAsync(branchId);
                var last12MonthsSales = await _dashboardService.GetLast12MonthsSalesAsync(branchId);

                return Json(new
                {
                    success = true,
                    currentMonthRevenue,
                    monthlySales = last12MonthsSales.Sales,
                    monthlyLabels = last12MonthsSales.Months
                });
            }
            catch (Exception)
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
