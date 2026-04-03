using IMS.CommonUtilities;
    using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using IMS.Common_Interfaces;
using IMS.Models;
using IMS.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;



namespace IMS.Controllers
{
   
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IReportService _reportService;
        private readonly ICustomer _customerService;
        private readonly IProductService _productService;
        private readonly IVendor _vendorService;
        private readonly IExpenseType _expenseTypeService;
        private const int DefaultPageSize = 10; // Default page size
        private static readonly int[] AllowedPageSizes = { 10, 20, 30 };
        public ReportController(IReportService reportService , ILogger<ReportController> logger, ICustomer customerService, IProductService productService, IVendor vendorService, IExpenseType expenseTypeService)
        {
            _reportService = reportService;
            _logger = logger;
            _customerService = customerService;
            _productService = productService;
            _vendorService = vendorService;
            _expenseTypeService = expenseTypeService;
        }
        public async Task<IActionResult> SalesReport(ReportsViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new ReportsViewModel();
                }

                // Initialize filters if null
                if (model.SalesReportsFilters == null)
                {
                    model.SalesReportsFilters = new SalesReportsFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("SalesReportsFilters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("SalesReportsFilters.ToDate");
                
                if (!hasFromDateParam && !model.SalesReportsFilters.FromDate.HasValue)
                {
                    model.SalesReportsFilters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.SalesReportsFilters.ToDate.HasValue)
                {
                    model.SalesReportsFilters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                //var customers = await _customerService.GetAllEnabledCustomers();
                //ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.SalesReportsFilters.CustomerId);

                // Preserve filters before service call
                var salesReportsFilters = model.SalesReportsFilters;

                // Get filtered data using model.SalesReportsFilters
                model = await _reportService.GetAllSales(pageNumber, currentPageSize, salesReportsFilters);

                // Reassign filters to ensure they're preserved
                model.SalesReportsFilters = salesReportsFilters;

                // Reassign dropdown again (important after service call)
                //ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.SalesReportsFilters.CustomerId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new ReportsViewModel();
                }
            }

            return View(model);
        }
        public async Task<IActionResult> ExportExcel(int pageNumber = 1, int? pageSize = null, long? custId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            //SalesReportsFilters salesReportsFilters = new SalesReportsFilters();
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var salesReportsFilters = new SalesReportsFilters
            {
                CustomerId = custId,
                FromDate = fromDate ?? DateTimeHelper.Now, // default today if null
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetAllSalesReport(pageNumber, currentPageSize, salesReportsFilters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Sale ID";
            worksheet.Cell(1, 2).Value = "Customer Name";
            worksheet.Cell(1, 3).Value = "Bill #";
            worksheet.Cell(1, 4).Value = "Sale Date";
            worksheet.Cell(1, 5).Value = "Total Amount";
            worksheet.Cell(1, 6).Value = "Discount";
            worksheet.Cell(1, 7).Value = "Paid Amount";
            worksheet.Cell(1, 8).Value = "Total Payable";
            worksheet.Cell(1, 9).Value = "Description";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.SalesList)
            {
                worksheet.Cell(row, 1).Value = item.SaleId;
                worksheet.Cell(row, 2).Value = item.CustomerName;
                worksheet.Cell(row, 3).Value = item.BillNumber;
                worksheet.Cell(row, 4).Value = item.SaleDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 5).Value = item.TotalAmount;
                worksheet.Cell(row, 6).Value = item.DiscountAmount;
                worksheet.Cell(row, 7).Value = item.TotalReceivedAmount;
                worksheet.Cell(row, 8).Value = item.TotalDueAmount;
                worksheet.Cell(row, 9).Value = item.SaleDescription ?? "";
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 4).Value = "TOTAL:";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalAmount;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalDiscountAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalReceivedAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalDueAmount;
            worksheet.Cell(row, 8).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"SalesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }



        public async Task<IActionResult> ExportPdf(int pageNumber = 1, int? pageSize = null, long? custId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // SalesReportsFilters salesReportsFilters = new SalesReportsFilters();

            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var salesReportsFilters = new SalesReportsFilters
            {
                CustomerId = custId,
                FromDate = fromDate ?? DateTimeHelper.Now, // default today if null
                ToDate = toDate ?? DateTimeHelper.Now
            };

            var model = await _reportService.GetAllSalesReport(pageNumber, currentPageSize, salesReportsFilters);

            using (var stream = new MemoryStream())
            {
                // Standard A4 page
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Sales Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 9 columns
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1.2f, 2.5f, 1.5f, 2f, 2f, 2f, 2f, 2f, 3f });

                // Header row
                string[] headers = { "Sale Id", "Customer", "Bill #", "Sale Date",
                             "Total Amount", "Discount", "Paid Amount", "Total Payable", "Description" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var s in model.SalesList)
                {
                    table.AddCell(s.SaleId.ToString());
                    table.AddCell(s.CustomerName ?? "");
                    table.AddCell(s.BillNumber.ToString());
                    table.AddCell(s.SaleDate.ToString("dd-MMM-yyyy"));
                    table.AddCell(s.TotalAmount.ToString("N2"));
                    table.AddCell(s.DiscountAmount.ToString("N2"));
                    table.AddCell(s.TotalReceivedAmount.ToString("N2")); // Paid Amount
                    table.AddCell(s.TotalDueAmount.ToString("N2"));      // Total Payable
                    table.AddCell(s.SaleDescription ?? "");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDiscountAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalReceivedAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDueAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();
                string filename = $"SalesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ProfitLossReport(ProfitLossReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new ProfitLossReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new ProfitLossReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var products = await _productService.GetAllEnabledProductsAsync();
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", model.Filters.ProductId);

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetProductWiseProfitLoss(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", model.Filters.ProductId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new ProfitLossReportViewModel();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> ExportProfitLossExcel(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new ProfitLossReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetProductWiseProfitLossReport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Profit Loss Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Product Name";
            worksheet.Cell(1, 2).Value = "Product Code";
            worksheet.Cell(1, 3).Value = "Quantity Sold";
            worksheet.Cell(1, 4).Value = "Total Sales Amount";
            worksheet.Cell(1, 5).Value = "Total Purchase Cost";
            worksheet.Cell(1, 6).Value = "Profit/Loss";
            worksheet.Cell(1, 7).Value = "Profit/Loss %";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.ProfitLossList)
            {
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.ProductCode;
                worksheet.Cell(row, 3).Value = item.TotalQuantitySold;
                worksheet.Cell(row, 4).Value = item.TotalSalesAmount;
                worksheet.Cell(row, 5).Value = item.TotalPurchaseCost;
                worksheet.Cell(row, 6).Value = item.ProfitLoss;
                worksheet.Cell(row, 7).Value = item.ProfitLossPercentage;
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 3).Value = "TOTAL:";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalSalesAmount;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalPurchaseCost;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalProfitLoss;
            worksheet.Cell(row, 6).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"ProfitLossReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProfitLossPdf(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new ProfitLossReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };

            var model = await _reportService.GetProductWiseProfitLossReport(pageNumber, currentPageSize, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Product Wise Profit/Loss Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 7 columns
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 2f, 1.5f, 2f, 2f, 2f, 1.5f });

                // Header row
                string[] headers = { "Product Name", "Product Code", "Qty Sold", "Sales Amount", "Purchase Cost", "Profit/Loss", "P/L %" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.ProfitLossList)
                {
                    table.AddCell(item.ProductName ?? "");
                    table.AddCell(item.ProductCode ?? "");
                    table.AddCell(item.TotalQuantitySold.ToString());
                    table.AddCell(item.TotalSalesAmount.ToString("N2"));
                    table.AddCell(item.TotalPurchaseCost.ToString("N2"));
                    
                    var profitLossCell = new PdfPCell(new Phrase(item.ProfitLoss.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                    profitLossCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    if (item.ProfitLoss < 0)
                        profitLossCell.BackgroundColor = BaseColor.RED;
                    else
                        profitLossCell.BackgroundColor = BaseColor.GREEN;
                    table.AddCell(profitLossCell);
                    
                    table.AddCell(item.ProfitLossPercentage.ToString("N2") + "%");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 3,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalSalesAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalPurchaseCost.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                
                var totalProfitLossCell = new PdfPCell(new Phrase(model.TotalProfitLoss.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                totalProfitLossCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                if (model.TotalProfitLoss < 0)
                    totalProfitLossCell.BackgroundColor = BaseColor.RED;
                else
                    totalProfitLossCell.BackgroundColor = BaseColor.GREEN;
                table.AddCell(totalProfitLossCell);
                
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();
                string filename = $"ProfitLossReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> DailyStockReport(DailyStockReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new DailyStockReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new DailyStockReportFilters();
                }

                // Set default report date if not provided
                if (!model.Filters.ReportDate.HasValue)
                {
                    model.Filters.ReportDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var products = await _productService.GetAllEnabledProductsAsync();

                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", model.Filters.ProductId);

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetDailyStockReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", model.Filters.ProductId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(model);
        }

        public async Task<IActionResult> ExportDailyStockExcel(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? reportDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new DailyStockReportFilters
            {
                ProductId = productId,
                ReportDate = reportDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetDailyStockReportForExport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Stock Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Product Name";
            worksheet.Cell(1, 2).Value = "Product Code";
            worksheet.Cell(1, 3).Value = "Total Quantity";
            worksheet.Cell(1, 4).Value = "Used Quantity";
            worksheet.Cell(1, 5).Value = "Available Quantity";
            worksheet.Cell(1, 6).Value = "Unit Price";
            worksheet.Cell(1, 7).Value = "Stock Value";
            worksheet.Cell(1, 8).Value = "Stock Location";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.StockList)
            {
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.ProductCode;
                worksheet.Cell(row, 3).Value = item.TotalQuantity;
                worksheet.Cell(row, 4).Value = item.UsedQuantity;
                worksheet.Cell(row, 5).Value = item.AvailableQuantity;
                worksheet.Cell(row, 6).Value = item.UnitPrice;
                worksheet.Cell(row, 7).Value = item.StockValue;
                worksheet.Cell(row, 8).Value = item.StockLocation;
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 2).Value = "TOTAL:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalQuantity;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalUsedQuantity;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalAvailableQuantity;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalStockValue;
            worksheet.Cell(row, 7).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"DailyStockReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportDailyStockPdf(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? reportDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new DailyStockReportFilters
            {
                ProductId = productId,
                ReportDate = reportDate ?? DateTimeHelper.Now
            };

            var model = await _reportService.GetDailyStockReportForExport(pageNumber, currentPageSize, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Daily Stock Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 8 columns
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 2f, 1.5f, 1.5f, 1.5f, 1.5f, 2f, 2f });

                // Header row
                string[] headers = { "Product Name", "Product Code", "Total Qty", "Used Qty", "Available Qty", "Unit Price", "Stock Value", "Location" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.StockList)
                {
                    table.AddCell(item.ProductName ?? "");
                    table.AddCell(item.ProductCode ?? "");
                    table.AddCell(item.TotalQuantity.ToString("N2"));
                    table.AddCell(item.UsedQuantity.ToString("N2"));
                    table.AddCell(item.AvailableQuantity.ToString("N2"));
                    table.AddCell(item.UnitPrice.ToString("N2"));
                    table.AddCell(item.StockValue.ToString("N2"));
                    table.AddCell(item.StockLocation ?? "");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 2,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalQuantity.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalUsedQuantity.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalAvailableQuantity.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalStockValue.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();
                string filename = $"DailyStockReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> PurchaseReport(PurchaseReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new PurchaseReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new PurchaseReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var vendors = await _vendorService.GetAllEnabledVendors();
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", model.Filters.VendorId);

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetPurchaseReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", model.Filters.VendorId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new PurchaseReportViewModel();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> ExportPurchaseExcel(int pageNumber = 1, int? pageSize = null, long? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new PurchaseReportFilters
            {
                VendorId = vendorId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetPurchaseReportForExport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Purchase Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Purchase ID";
            worksheet.Cell(1, 2).Value = "Vendor Name";
            worksheet.Cell(1, 3).Value = "Bill #";
            worksheet.Cell(1, 4).Value = "Purchase Date";
            worksheet.Cell(1, 5).Value = "Total Amount";
            worksheet.Cell(1, 6).Value = "Discount";
            worksheet.Cell(1, 7).Value = "Paid Amount";
            worksheet.Cell(1, 8).Value = "Due Amount";
            worksheet.Cell(1, 9).Value = "Description";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.PurchaseList)
            {
                worksheet.Cell(row, 1).Value = item.PurchaseOrderId;
                worksheet.Cell(row, 2).Value = item.VendorName;
                worksheet.Cell(row, 3).Value = item.BillNumber;
                worksheet.Cell(row, 4).Value = item.PurchaseDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 5).Value = item.TotalAmount;
                worksheet.Cell(row, 6).Value = item.DiscountAmount;
                worksheet.Cell(row, 7).Value = item.PaidAmount;
                worksheet.Cell(row, 8).Value = item.DueAmount;
                worksheet.Cell(row, 9).Value = item.PurchaseDescription ?? "";
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 4).Value = "TOTAL:";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalAmount;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalDiscountAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalPaidAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalDueAmount;
            worksheet.Cell(row, 8).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"PurchaseReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportPurchasePdf(int pageNumber = 1, int? pageSize = null, long? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new PurchaseReportFilters
            {
                VendorId = vendorId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };

            var model = await _reportService.GetPurchaseReportForExport(pageNumber, currentPageSize, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Purchase Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 9 columns
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1.2f, 2.5f, 1.5f, 2f, 2f, 2f, 2f, 2f, 3f });

                // Header row
                string[] headers = { "Purchase Id", "Vendor", "Bill #", "Purchase Date",
                             "Total Amount", "Discount", "Paid Amount", "Due Amount", "Description" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var p in model.PurchaseList)
                {
                    table.AddCell(p.PurchaseOrderId.ToString());
                    table.AddCell(p.VendorName ?? "");
                    table.AddCell(p.BillNumber.ToString());
                    table.AddCell(p.PurchaseDate.ToString("dd-MMM-yyyy"));
                    table.AddCell(p.TotalAmount.ToString("N2"));
                    table.AddCell(p.DiscountAmount.ToString("N2"));
                    table.AddCell(p.PaidAmount.ToString("N2"));
                    table.AddCell(p.DueAmount.ToString("N2"));
                    table.AddCell(p.PurchaseDescription ?? "");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDiscountAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalPaidAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDueAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();
                string filename = $"PurchaseReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ProductWiseSalesReport(ProductWiseSalesReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new ProductWiseSalesReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new ProductWiseSalesReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetProductWiseSalesReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new ProductWiseSalesReportViewModel();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> ExportProductWiseSalesExcel(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWiseSalesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetProductWiseSalesReport(1, 10000, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Wise Sales Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Weight";
            worksheet.Cell(1, 4).Value = "Qty";
            worksheet.Cell(1, 5).Value = "Rate";
            worksheet.Cell(1, 6).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.SalesList)
            {
                if (item.IsTotalRow)
                {
                    // Total row - bold and different background
                    worksheet.Cell(row, 1).Value = "";
                    worksheet.Cell(row, 2).Value = $"Total Sales {item.ProductName}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = item.Weight;
                    worksheet.Cell(row, 4).Value = item.Qty;
                    worksheet.Cell(row, 5).Value = item.Rate;
                    worksheet.Cell(row, 6).Value = item.Amount;
                    
                    var totalRange = worksheet.Range(row, 1, row, 6);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.SaleDate != DateTime.MinValue ? item.SaleDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = item.ProductName;
                    worksheet.Cell(row, 3).Value = item.Weight > 0 ? item.Weight : (double?)null;
                    worksheet.Cell(row, 4).Value = item.Qty > 0 ? item.Qty : (long?)null;
                    worksheet.Cell(row, 5).Value = item.Rate > 0 ? item.Rate : (double?)null;
                    worksheet.Cell(row, 6).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = "";
            worksheet.Cell(row, 3).Value = model.TotalWeight;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalQty;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = "";
            worksheet.Cell(row, 6).Value = model.TotalAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 6);
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"ProductWiseSalesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProductWiseSalesPdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWiseSalesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetProductWiseSalesReport(1, 10000, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Product Wise Sales Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 6 columns
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 3f, 1.5f, 1.5f, 1.5f, 2f });

                // Header row
                string[] headers = { "Date", "Description", "Weight", "Qty", "Rate", "Amount" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.SalesList)
                {
                    if (item.IsTotalRow)
                    {
                        // Total row - bold and different background
                        var totalCell1 = new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell1);
                        
                        var totalCell2 = new PdfPCell(new Phrase($"Total Sales {item.ProductName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell2);
                        
                        table.AddCell(new PdfPCell(new Phrase(item.Weight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Qty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Rate.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        // Regular row
                        table.AddCell(item.SaleDate != DateTime.MinValue ? item.SaleDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(item.ProductName ?? "");
                        table.AddCell(item.Weight > 0 ? item.Weight.ToString("N2") : "");
                        table.AddCell(item.Qty > 0 ? item.Qty.ToString() : "");
                        table.AddCell(item.Rate > 0 ? item.Rate.ToString("N2") : "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell1 = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 2,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell1);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalWeight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalQty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                byte[] bytes = stream.ToArray();
                string filename = $"ProductWiseSalesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ProductWisePurchaseReport(ProductWisePurchaseReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new ProductWisePurchaseReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new ProductWisePurchaseReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Date validation - only validate if both dates are provided
                if (model.Filters.FromDate.HasValue && model.Filters.ToDate.HasValue && model.Filters.FromDate > model.Filters.ToDate)
                {
                    TempData["WarningMessage"] = "From Date cannot be greater than To Date.";
                }

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetProductWisePurchaseReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(model);
        }

        public async Task<IActionResult> ExportProductWisePurchaseExcel(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWisePurchaseReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetProductWisePurchaseReport(1, 10000, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Wise Purchase Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Weight";
            worksheet.Cell(1, 4).Value = "Qty";
            worksheet.Cell(1, 5).Value = "Rate";
            worksheet.Cell(1, 6).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.PurchaseList)
            {
                if (item.IsTotalRow)
                {
                    // Total row - bold and different background
                    worksheet.Cell(row, 1).Value = "";
                    worksheet.Cell(row, 2).Value = $"Total Purchase {item.ProductName}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = item.Weight;
                    worksheet.Cell(row, 4).Value = item.Qty;
                    worksheet.Cell(row, 5).Value = item.Rate;
                    worksheet.Cell(row, 6).Value = item.Amount;
                    
                    var totalRange = worksheet.Range(row, 1, row, 6);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.PurchaseDate != DateTime.MinValue ? item.PurchaseDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = item.ProductName;
                    worksheet.Cell(row, 3).Value = item.Weight > 0 ? item.Weight : (double?)null;
                    worksheet.Cell(row, 4).Value = item.Qty > 0 ? item.Qty : (long?)null;
                    worksheet.Cell(row, 5).Value = item.Rate > 0 ? item.Rate : (double?)null;
                    worksheet.Cell(row, 6).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = "";
            worksheet.Cell(row, 3).Value = model.TotalWeight;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalQty;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = "";
            worksheet.Cell(row, 6).Value = model.TotalAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 6);
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"ProductWisePurchaseReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProductWisePurchasePdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWisePurchaseReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetProductWisePurchaseReport(1, 10000, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Product Wise Purchase Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 6 columns
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 3f, 1.5f, 1.5f, 1.5f, 2f });

                // Header row
                string[] headers = { "Date", "Description", "Weight", "Qty", "Rate", "Amount" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.PurchaseList)
                {
                    if (item.IsTotalRow)
                    {
                        // Total row - bold and different background
                        var totalCell1 = new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell1);
                        
                        var totalCell2 = new PdfPCell(new Phrase($"Total Purchase {item.ProductName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell2);
                        
                        table.AddCell(new PdfPCell(new Phrase(item.Weight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Qty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Rate.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        // Regular row
                        table.AddCell(item.PurchaseDate != DateTime.MinValue ? item.PurchaseDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(item.ProductName ?? "");
                        table.AddCell(item.Weight > 0 ? item.Weight.ToString("N2") : "");
                        table.AddCell(item.Qty > 0 ? item.Qty.ToString() : "");
                        table.AddCell(item.Rate > 0 ? item.Rate.ToString("N2") : "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell1 = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 2,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell1);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalWeight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalQty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                byte[] bytes = stream.ToArray();
                string filename = $"ProductWisePurchaseReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }

        public async Task<IActionResult> GeneralExpensesReport(GeneralExpensesReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new GeneralExpensesReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new GeneralExpensesReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var expenseTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();

                ViewBag.ExpenseTypes = new SelectList(expenseTypes, "ExpenseTypeId", "ExpenseTypeName", model.Filters.ExpenseTypeId);

                // Date validation - only validate if both dates are provided
                if (model.Filters.FromDate.HasValue && model.Filters.ToDate.HasValue && model.Filters.FromDate > model.Filters.ToDate)
                {
                    TempData["WarningMessage"] = "From Date cannot be greater than To Date.";
                }

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetGeneralExpensesReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                ViewBag.ExpenseTypes = new SelectList(expenseTypes, "ExpenseTypeId", "ExpenseTypeName", model.Filters.ExpenseTypeId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(model);
        }
        public async Task<IActionResult> PurchaseOrderProductExpensesReport(GeneralExpensesReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new GeneralExpensesReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new GeneralExpensesReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                // If dates are cleared by user (empty string in query), they will remain null
                var fromDateParam = Request.Query["Filters.FromDate"].ToString();
                var toDateParam = Request.Query["Filters.ToDate"].ToString();
                
                // Only set defaults if this is a fresh load (no date parameters in query)
                if (string.IsNullOrEmpty(fromDateParam))
                {
                    if (!model.Filters.FromDate.HasValue)
                    {
                        // First time load - set to 1st of current month
                        model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                    }
                    // If user cleared the date (empty string), model.Filters.FromDate will be null, which is fine
                }
                
                if (string.IsNullOrEmpty(toDateParam))
                {
                    if (!model.Filters.ToDate.HasValue)
                    {
                        // First time load - set to today
                        model.Filters.ToDate = DateTimeHelper.Now;
                    }
                    // If user cleared the date (empty string), model.Filters.ToDate will be null, which is fine
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                //var expenseTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();

                //ViewBag.ExpenseTypes = new SelectList(expenseTypes, "ExpenseTypeId", "ExpenseTypeName", model.Filters.ExpenseTypeId);

                // Date validation - only validate if both dates are provided
                if (model.Filters.FromDate.HasValue && model.Filters.ToDate.HasValue && model.Filters.FromDate > model.Filters.ToDate)
                {
                    TempData["WarningMessage"] = "From Date cannot be greater than To Date.";
                }

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetPOExpensesReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                //ViewBag.ExpenseTypes = new SelectList(expenseTypes, "ExpenseTypeId", "ExpenseTypeName", model.Filters.ExpenseTypeId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return View(model);
        }

        public async Task<IActionResult> DailyStockPositionReport(DateTime? reportDate = null)
        {
            try
            {
                var filters = new DailyStockPositionReportFilters
                {
                    ReportDate = reportDate ?? DateTimeHelper.Now
                };

                var model = await _reportService.GetDailyStockPositionReport(filters);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new DailyStockPositionReportViewModel
                {
                    Filters = new DailyStockPositionReportFilters { ReportDate = DateTimeHelper.Now }
                });
            }
        }

        public async Task<IActionResult> ExportDailyStockPositionExcel(DateTime? reportDate = null)
        {
            var filters = new DailyStockPositionReportFilters
            {
                ReportDate = reportDate ?? DateTimeHelper.Now
            };
            
            var model = await _reportService.GetDailyStockPositionReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Stock Position");
            
            // Add header
            worksheet.Cell(1, 1).Value = "DAILY STOCK POSITION";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, 5).Merge();
            
            worksheet.Cell(2, 1).Value = $"Date: {filters.ReportDate.Value.ToString("dd-MMM-yyyy")}";
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Range(2, 1, 2, 5).Merge();
            
            // Column headers
            worksheet.Cell(3, 1).Value = "Particulars";
            worksheet.Cell(3, 2).Value = "Purchase";
            worksheet.Cell(3, 3).Value = "Sales";
            worksheet.Cell(3, 4).Value = "Closing";
            worksheet.Cell(3, 5).Value = "Bags";
            
            // Style header
            var headerRange = worksheet.Range(3, 1, 3, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add data
            int row = 4;
            foreach (var item in model.StockPositionList)
            {
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.PurchaseQuantity > 0 ? item.PurchaseQuantity : (double?)null;
                worksheet.Cell(row, 3).Value = item.SalesQuantity > 0 ? item.SalesQuantity : (double?)null;
                worksheet.Cell(row, 4).Value = item.ClosingStock;
                worksheet.Cell(row, 5).Value = item.Bags > 0 ? item.Bags : (long?)null;
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = model.TotalPurchase;
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalSales;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalClosing;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalBags;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 5);
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"DailyStockPosition_{filters.ReportDate.Value:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportDailyStockPositionPdf(DateTime? reportDate = null)
        {
            var filters = new DailyStockPositionReportFilters
            {
                ReportDate = reportDate ?? DateTimeHelper.Now
            };
            
            var model = await _reportService.GetDailyStockPositionReport(filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f); // Landscape orientation
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var title = new Paragraph("DAILY STOCK POSITION", titleFont) { Alignment = Element.ALIGN_CENTER };
                document.Add(title);
                
                var dateFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var datePara = new Paragraph($"Date: {filters.ReportDate.Value.ToString("dd-MMM-yyyy")}", dateFont) { Alignment = Element.ALIGN_CENTER };
                document.Add(datePara);
                document.Add(new Paragraph("\n")); // Add space

                // Table with 5 columns
                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 2f, 2f, 2f, 1.5f });

                // Header row
                string[] headers = { "Particulars", "Purchase", "Sales", "Closing", "Bags" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.StockPositionList)
                {
                    table.AddCell(item.ProductName ?? "");
                    table.AddCell(item.PurchaseQuantity > 0 ? item.PurchaseQuantity.ToString("N2") : "");
                    table.AddCell(item.SalesQuantity > 0 ? item.SalesQuantity.ToString("N2") : "");
                    table.AddCell(item.ClosingStock.ToString("N2"));
                    table.AddCell(item.Bags > 0 ? item.Bags.ToString() : "");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(model.TotalPurchase.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalSales.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalClosing.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalBags.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                byte[] bytes = stream.ToArray();
                string filename = $"DailyStockPosition_{filters.ReportDate.Value:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ExportGeneralExpensesExcel(long? expenseTypeId = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new GeneralExpensesReportFilters
            {
                ExpenseTypeId = expenseTypeId,
                ProductId = productId,
                FromDate = fromDate, // Allow null to get all expenses
                ToDate = toDate // Allow null to get all expenses
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetGeneralExpensesReport(1, 10000, filters);

            // Check if it's "Direct Product Expense" with product filter
            string reportName = "General Expenses Report";
            string worksheetName = "General Expenses Report";
            if (expenseTypeId.HasValue)
            {
                var expenseTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                var expenseType = expenseTypes.FirstOrDefault(et => et.ExpenseTypeId == expenseTypeId.Value);
                if (expenseType != null && expenseType.ExpenseTypeName == "Direct Product Expense")
                {
                    reportName = "Direct Product Expense Report";
                    worksheetName = "Direct Product Expense Report";
                }
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(worksheetName);
            
            // Add header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Expense Type";
            worksheet.Cell(1, 3).Value = "Expense Detail";
            worksheet.Cell(1, 4).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.ExpensesList)
            {
                if (item.IsTotalRow)
                {
                    // Total row - bold and different background
                    worksheet.Cell(row, 1).Value = "";
                    worksheet.Cell(row, 2).Value = $"Total - {item.ExpenseTypeName}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = "";
                    worksheet.Cell(row, 4).Value = item.Amount;
                    worksheet.Cell(row, 4).Style.Font.Bold = true;
                    
                    var totalRange = worksheet.Range(row, 1, row, 4);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = item.ExpenseTypeName;
                    worksheet.Cell(row, 3).Value = item.ExpenseDetail;
                    worksheet.Cell(row, 4).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 2).Value = "TOTAL:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalAmount;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 4);
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"{reportName.Replace(" ", "")}_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
      

        public async Task<IActionResult> ExportGeneralExpensesPdf(long? expenseTypeId = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new GeneralExpensesReportFilters
            {
                ExpenseTypeId = expenseTypeId,
                ProductId = productId,
                FromDate = fromDate, // Allow null to get all expenses
                ToDate = toDate // Allow null to get all expenses
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetGeneralExpensesReport(1, 10000, filters);

            // Check if it's "Direct Product Expense" with product filter
            string reportTitle = "General Expenses Report";
            if (expenseTypeId.HasValue && productId.HasValue)
            {
                var expenseTypes = await _expenseTypeService.GetAllEnabledExpenseTypesAsync();
                var expenseType = expenseTypes.FirstOrDefault(et => et.ExpenseTypeId == expenseTypeId.Value);
                if (expenseType != null && expenseType.ExpenseTypeName == "Direct Product Expense")
                {
                    reportTitle = "Direct Product Expense Report";
                }
            }

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph(reportTitle, titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 4 columns
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 3f, 4f, 2f });

                // Header row
                string[] headers = { "Date", "Expense Type", "Expense Detail", "Amount" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.ExpensesList)
                {
                    if (item.IsTotalRow)
                    {
                        // Total row - bold and different background
                        var totalCell1 = new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell1);
                        
                        var totalCell2 = new PdfPCell(new Phrase($"Total - {item.ExpenseTypeName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell2);
                        
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        // Regular row
                        table.AddCell(item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(item.ExpenseTypeName ?? "");
                        table.AddCell(item.ExpenseDetail ?? "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 3,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                byte[] bytes = stream.ToArray();
                string filename = $"{reportTitle.Replace(" ", "")}_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ExportPOExpensesExcel(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new GeneralExpensesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate,
                ToDate = toDate
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetPOExpensesReport(1, 10000, filters);

            string reportName = "Direct Product Expense Report";
            string worksheetName = "Direct Product Expense Report";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(worksheetName);
            
            // Add header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Expense Type";
            worksheet.Cell(1, 3).Value = "Product Name";
            worksheet.Cell(1, 4).Value = "Expense Detail";
            worksheet.Cell(1, 5).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.ExpensesList)
            {
                if (item.IsTotalRow)
                {
                    // Total row - bold and different background
                    worksheet.Cell(row, 1).Value = "";
                    worksheet.Cell(row, 2).Value = $"Total - {item.ExpenseTypeName}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = "";
                    worksheet.Cell(row, 4).Value = "";
                    worksheet.Cell(row, 5).Value = item.Amount;
                    worksheet.Cell(row, 5).Style.Font.Bold = true;
                    
                    var totalRange = worksheet.Range(row, 1, row, 5);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = item.ExpenseTypeName;
                    worksheet.Cell(row, 3).Value = item.ProductName;
                    worksheet.Cell(row, 4).Value = item.ExpenseDetail;
                    worksheet.Cell(row, 5).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 2).Value = "TOTAL:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalAmount;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 5);
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"{reportName.Replace(" ", "")}_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportPOExpensesPdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new GeneralExpensesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate,
                ToDate = toDate
            };
            
            // Get all data for export (use large page size to get all records)
            var model = await _reportService.GetPOExpensesReport(1, 10000, filters);

            string reportTitle = "Direct Product Expense Report";

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph(reportTitle, titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 5 columns
                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 2.5f, 2.5f, 3f, 2f });

                // Header row
                string[] headers = { "Date", "Expense Type", "Product Name", "Expense Detail", "Amount" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.ExpensesList)
                {
                    if (item.IsTotalRow)
                    {
                        // Total row - bold and different background
                        var totalCell1 = new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell1);
                        
                        var totalCell2 = new PdfPCell(new Phrase($"Total - {item.ExpenseTypeName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        {
                            BackgroundColor = BaseColor.BLUE
                        };
                        table.AddCell(totalCell2);
                        
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        // Regular row
                        table.AddCell(item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(item.ExpenseTypeName ?? "");
                        table.AddCell(item.ExpenseDetail ?? "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 3,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();

                byte[] bytes = stream.ToArray();
                string filename = $"{reportTitle.Replace(" ", "")}_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }
       

        public async Task<IActionResult> BankCreditDebitReport(BankCreditDebitReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                // Initialize model if null
                if (model == null)
                {
                    model = new BankCreditDebitReportViewModel();
                }

                // Initialize filters if null
                if (model.Filters == null)
                {
                    model.Filters = new BankCreditDebitReportFilters();
                }

                // Set default dates only if not provided (first time load)
                // FromDate: 1st of current month, ToDate: Today
                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                }
                
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTimeHelper.Now;
                }

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                // Load dropdown data
                var personalPayments = await _vendorService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                ViewBag.Accounts = new SelectList(
                    personalPayments.PersonalPaymentList.Select(pp => new { 
                        PersonalPaymentId = pp.PersonalPaymentId, 
                        DisplayName = $"{pp.BankName} - {pp.AccountNumber} ({pp.AccountHolderName})" 
                    }), 
                    "PersonalPaymentId", 
                    "DisplayName", 
                    model.Filters.PersonalPaymentId);

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetBankCreditDebitReport(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;

                // Reassign dropdown again (important after service call)
                ViewBag.Accounts = new SelectList(
                    personalPayments.PersonalPaymentList.Select(pp => new { 
                        PersonalPaymentId = pp.PersonalPaymentId, 
                        DisplayName = $"{pp.BankName} - {pp.AccountNumber} ({pp.AccountHolderName})" 
                    }), 
                    "PersonalPaymentId", 
                    "DisplayName", 
                    model.Filters.PersonalPaymentId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new BankCreditDebitReportViewModel();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> CustomerLedgerReport(CustomerLedgerReportViewModel model)
        {
            try
            {
                if (model == null)
                    model = new CustomerLedgerReportViewModel();
                if (model.Filters == null)
                    model.Filters = new CustomerLedgerReportFilters();

                var hasFrom = Request.Query.ContainsKey("Filters.FromDate");
                var hasTo = Request.Query.ContainsKey("Filters.ToDate");
                if (!hasFrom && !model.Filters.FromDate.HasValue)
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                if (!hasTo && !model.Filters.ToDate.HasValue)
                    model.Filters.ToDate = DateTimeHelper.Now;

                var customers = await _customerService.GetAllEnabledCustomers();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.Filters.CustomerId);

                var filters = model.Filters;
                model = await _reportService.GetCustomerLedgerReport(filters);
                model.Filters = filters;
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.Filters.CustomerId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                    model = new CustomerLedgerReportViewModel();
                var customers = await _customerService.GetAllEnabledCustomers();
                ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", model.Filters?.CustomerId);
            }
            return View(model);
        }

        public async Task<IActionResult> ExportCustomerLedgerExcel(long? customerId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new CustomerLedgerReportFilters
            {
                CustomerId = customerId,
                FromDate = fromDate ?? new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1),
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetCustomerLedgerReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customer Ledger Report");

            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "GL Account (Customer)";
            worksheet.Cell(1, 3).Value = "Debit";
            worksheet.Cell(1, 4).Value = "Credit";
            worksheet.Cell(1, 5).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var ledgerList = model.LedgerList ?? new List<CustomerLedgerReportItem>();
            bool multiCustomer = model.CustomerName == "All Customers" && ledgerList.Any();
            var excelCustomerColors = new[] { XLColor.LightGray, XLColor.LightYellow, XLColor.LightCyan, XLColor.Lavender, XLColor.LightGreen };
            string? prevCustomer = null;
            int colorIndex = -1;

            int row = 2;
            foreach (var item in ledgerList)
            {
                if (multiCustomer)
                {
                    var cust = item.CustomerName ?? "";
                    if (cust != prevCustomer) { prevCustomer = cust; colorIndex++; }
                    var fillColor = excelCustomerColors[colorIndex % excelCustomerColors.Length];
                    worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = fillColor;
                }
                worksheet.Cell(row, 1).Value = item.Date.ToString("dd-MMM-yy");
                worksheet.Cell(row, 2).Value = item.CustomerName ?? "";
                worksheet.Cell(row, 3).Value = item.Debit;
                worksheet.Cell(row, 4).Value = item.Credit;
                worksheet.Cell(row, 5).Value = item.Balance;
                row++;
            }

            row++;
            worksheet.Cell(row, 2).Value = "Total Debit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalDebit;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Total Credit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalCredit;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Closing Balance:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.ClosingBalance;
            worksheet.Cell(row, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"CustomerLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportCustomerLedgerPdf(long? customerId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new CustomerLedgerReportFilters
            {
                CustomerId = customerId,
                FromDate = fromDate ?? new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1),
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetCustomerLedgerReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("Customer Ledger Report", titleFont) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(model.CustomerName))
                document.Add(new Paragraph(model.CustomerName, FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.5f, 3f, 2f, 2f, 2f });
            string[] headers = { "Date", "GL Account (Customer)", "Debit", "Credit", "Balance" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell);
            }

            var ledgerListPdf = model.LedgerList ?? new List<CustomerLedgerReportItem>();
            bool multiCustomerPdf = model.CustomerName == "All Customers" && ledgerListPdf.Any();
            var pdfCustomerColors = new BaseColor[]
            {
                new BaseColor(240, 240, 240),   // light gray
                new BaseColor(255, 255, 220),   // light yellow
                new BaseColor(220, 240, 255),   // light blue
                new BaseColor(230, 230, 250),   // lavender
                new BaseColor(220, 255, 220)    // light green
            };
            string? prevCustomerPdf = null;
            int colorIndexPdf = -1;

            foreach (var item in ledgerListPdf)
            {
                BaseColor? rowBg = null;
                if (multiCustomerPdf)
                {
                    var cust = item.CustomerName ?? "";
                    if (cust != prevCustomerPdf) { prevCustomerPdf = cust; colorIndexPdf++; }
                    rowBg = pdfCustomerColors[colorIndexPdf % pdfCustomerColors.Length];
                }
                var dateCell = new PdfPCell(new Phrase(item.Date.ToString("dd-MMM-yy"), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var nameCell = new PdfPCell(new Phrase(item.CustomerName ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var debitCell = new PdfPCell(new Phrase(item.Debit > 0 ? item.Debit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var creditCell = new PdfPCell(new Phrase(item.Credit > 0 ? item.Credit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var balanceCell = new PdfPCell(new Phrase(item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                if (rowBg != null) { dateCell.BackgroundColor = rowBg; nameCell.BackgroundColor = rowBg; debitCell.BackgroundColor = rowBg; creditCell.BackgroundColor = rowBg; balanceCell.BackgroundColor = rowBg; }
                table.AddCell(dateCell);
                table.AddCell(nameCell);
                table.AddCell(debitCell);
                table.AddCell(creditCell);
                table.AddCell(balanceCell);
            }

            var totalLabelCell = new PdfPCell(new Phrase("Total Debit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(totalLabelCell);
            table.AddCell(new PdfPCell(new Phrase(model.TotalDebit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var creditLabelCell = new PdfPCell(new Phrase("Total Credit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(creditLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCredit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var balanceLabelCell = new PdfPCell(new Phrase("Closing Balance", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(balanceLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.ClosingBalance >= 0 ? model.ClosingBalance.ToString("N2") : "(" + (-model.ClosingBalance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            document.Add(table);
            document.Close();

            string filename = $"CustomerLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> VendorLedgerReport(VendorLedgerReportViewModel model)
        {
            try
            {
                if (model == null) model = new VendorLedgerReportViewModel();
                if (model.Filters == null) model.Filters = new VendorLedgerReportFilters();

                var hasFrom = Request.Query.ContainsKey("Filters.FromDate");
                var hasTo = Request.Query.ContainsKey("Filters.ToDate");
                if (!hasFrom && !model.Filters.FromDate.HasValue)
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                if (!hasTo && !model.Filters.ToDate.HasValue)
                    model.Filters.ToDate = DateTimeHelper.Now;

                var vendors = await _vendorService.GetAllEnabledVendors();
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", model.Filters.VendorId);

                var filters = model.Filters;
                model = await _reportService.GetVendorLedgerReport(filters);
                model.Filters = filters;
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", model.Filters.VendorId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null) model = new VendorLedgerReportViewModel();
                var vendors = await _vendorService.GetAllEnabledVendors();
                ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", model.Filters?.VendorId);
            }
            return View(model);
        }

        public async Task<IActionResult> ExportVendorLedgerExcel(long? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new VendorLedgerReportFilters
            {
                VendorId = vendorId,
                FromDate = fromDate ?? new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1),
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetVendorLedgerReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Vendor Ledger Report");

            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "GL Account (Vendor)";
            worksheet.Cell(1, 3).Value = "Debit";
            worksheet.Cell(1, 4).Value = "Credit";
            worksheet.Cell(1, 5).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var ledgerList = model.LedgerList ?? new List<VendorLedgerReportItem>();
            bool multiVendor = model.VendorName == "All Vendors" && ledgerList.Any();
            var excelColors = new[] { XLColor.LightGray, XLColor.LightYellow, XLColor.LightCyan, XLColor.Lavender, XLColor.LightGreen };
            string? prevVendor = null;
            int colorIndex = -1;

            int row = 2;
            foreach (var item in ledgerList)
            {
                if (multiVendor)
                {
                    var v = item.VendorName ?? "";
                    if (v != prevVendor) { prevVendor = v; colorIndex++; }
                    var fillColor = excelColors[colorIndex % excelColors.Length];
                    worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = fillColor;
                }
                worksheet.Cell(row, 1).Value = item.Date.ToString("dd-MMM-yy");
                worksheet.Cell(row, 2).Value = item.VendorName ?? "";
                worksheet.Cell(row, 3).Value = item.Debit;
                worksheet.Cell(row, 4).Value = item.Credit;
                worksheet.Cell(row, 5).Value = item.Balance;
                row++;
            }

            row++;
            worksheet.Cell(row, 2).Value = "Total Debit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalDebit;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Total Credit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalCredit;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Closing Balance:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.ClosingBalance;
            worksheet.Cell(row, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"VendorLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        public async Task<IActionResult> ExportVendorLedgerPdf(long? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new VendorLedgerReportFilters
            {
                VendorId = vendorId,
                FromDate = fromDate ?? new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1),
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetVendorLedgerReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            document.Add(new Paragraph("Vendor Ledger Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(model.VendorName))
                document.Add(new Paragraph(model.VendorName, FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.5f, 3f, 2f, 2f, 2f });
            string[] headers = { "Date", "GL Account (Vendor)", "Debit", "Credit", "Balance" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell);
            }

            var ledgerListPdf = model.LedgerList ?? new List<VendorLedgerReportItem>();
            bool multiVendorPdf = model.VendorName == "All Vendors" && ledgerListPdf.Any();
            var pdfColors = new BaseColor[]
            {
                new BaseColor(240, 240, 240),
                new BaseColor(255, 255, 220),
                new BaseColor(220, 240, 255),
                new BaseColor(230, 230, 250),
                new BaseColor(220, 255, 220)
            };
            string? prevVendorPdf = null;
            int colorIndexPdf = -1;

            foreach (var item in ledgerListPdf)
            {
                BaseColor? rowBg = null;
                if (multiVendorPdf)
                {
                    var v = item.VendorName ?? "";
                    if (v != prevVendorPdf) { prevVendorPdf = v; colorIndexPdf++; }
                    rowBg = pdfColors[colorIndexPdf % pdfColors.Length];
                }
                var dateCell = new PdfPCell(new Phrase(item.Date.ToString("dd-MMM-yy"), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var nameCell = new PdfPCell(new Phrase(item.VendorName ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var debitCell = new PdfPCell(new Phrase(item.Debit > 0 ? item.Debit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var creditCell = new PdfPCell(new Phrase(item.Credit > 0 ? item.Credit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var balanceCell = new PdfPCell(new Phrase(item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                if (rowBg != null) { dateCell.BackgroundColor = rowBg; nameCell.BackgroundColor = rowBg; debitCell.BackgroundColor = rowBg; creditCell.BackgroundColor = rowBg; balanceCell.BackgroundColor = rowBg; }
                table.AddCell(dateCell);
                table.AddCell(nameCell);
                table.AddCell(debitCell);
                table.AddCell(creditCell);
                table.AddCell(balanceCell);
            }

            var totalLabelCell = new PdfPCell(new Phrase("Total Debit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 2, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(totalLabelCell);
            table.AddCell(new PdfPCell(new Phrase(model.TotalDebit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var creditLabelCell = new PdfPCell(new Phrase("Total Credit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 2, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(creditLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCredit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var balanceLabelCell = new PdfPCell(new Phrase("Closing Balance", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 2, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(balanceLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.ClosingBalance >= 0 ? model.ClosingBalance.ToString("N2") : "(" + (-model.ClosingBalance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            document.Add(table);
            document.Close();

            string filename = $"VendorLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> CustomerBalanceReport(CustomerBalanceReportViewModel model)
        {
            try
            {
                if (model == null)
                    model = new CustomerBalanceReportViewModel();
                if (model.Filters == null)
                    model.Filters = new CustomerBalanceReportFilters();

                if (!Request.Query.ContainsKey("Filters.AsOfDate") && !model.Filters.AsOfDate.HasValue)
                    model.Filters.AsOfDate = DateTimeHelper.Now.Date;

                var filters = model.Filters;
                model = await _reportService.GetCustomerBalanceReport(filters);
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                    model = new CustomerBalanceReportViewModel();
            }
            return View(model);
        }

        public async Task<IActionResult> ExportCustomerBalanceExcel(long? customerId = null, DateTime? asOfDate = null)
        {
            var filters = new CustomerBalanceReportFilters
            {
                CustomerId = customerId,
                AsOfDate = asOfDate ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetCustomerBalanceReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customer Balance Report");
            worksheet.Cell(1, 1).Value = "As of Date";
            worksheet.Cell(1, 2).Value = "Customer";
            worksheet.Cell(1, 3).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rows = model.BalanceList ?? new List<CustomerBalanceReportItem>();
            int row = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(row, 1).Value = item.AsOfDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 2).Value = item.CustomerName ?? "";
                worksheet.Cell(row, 3).Value = item.Balance;
                row++;
            }
            row++;
            worksheet.Cell(row, 2).Value = "Total (sum of balances):";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalBalance;
            worksheet.Cell(row, 3).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"CustomerBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportCustomerBalancePdf(long? customerId = null, DateTime? asOfDate = null)
        {
            var filters = new CustomerBalanceReportFilters
            {
                CustomerId = customerId,
                AsOfDate = asOfDate ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetCustomerBalanceReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 40f, 40f, 40f, 40f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            document.Add(new Paragraph("Customer Balance Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(model.ScopeLabel))
                document.Add(new Paragraph(model.ScopeLabel, FontFactory.GetFont(FontFactory.HELVETICA, 11)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(3);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2f, 3f, 2f });
            foreach (var h in new[] { "As of Date", "Customer", "Balance" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                });
            }

            foreach (var item in model.BalanceList ?? new List<CustomerBalanceReportItem>())
            {
                table.AddCell(new PdfPCell(new Phrase(item.AsOfDate.ToString("dd-MMM-yyyy"), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                table.AddCell(new PdfPCell(new Phrase(item.CustomerName ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                var bal = item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")";
                table.AddCell(new PdfPCell(new Phrase(bal, FontFactory.GetFont(FontFactory.HELVETICA, 9))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var totalLabel = new PdfPCell(new Phrase("Total (sum of balances)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(totalLabel);
            var tb = model.TotalBalance;
            var totalStr = tb == 0 ? "-" : tb > 0 ? tb.ToString("N2") : "(" + (-tb).ToString("N2") + ")";
            table.AddCell(new PdfPCell(new Phrase(totalStr, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            });

            document.Add(table);
            document.Close();
            string filename = $"CustomerBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> VendorBalanceReport(VendorBalanceReportViewModel model)
        {
            try
            {
                if (model == null)
                    model = new VendorBalanceReportViewModel();
                if (model.Filters == null)
                    model.Filters = new VendorBalanceReportFilters();

                if (!Request.Query.ContainsKey("Filters.AsOfDate") && !model.Filters.AsOfDate.HasValue)
                    model.Filters.AsOfDate = DateTimeHelper.Now.Date;

                var filters = model.Filters;
                model = await _reportService.GetVendorBalanceReport(filters);
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                    model = new VendorBalanceReportViewModel();
            }
            return View(model);
        }

        public async Task<IActionResult> ExportVendorBalanceExcel(long? vendorId = null, DateTime? asOfDate = null)
        {
            var filters = new VendorBalanceReportFilters
            {
                VendorId = vendorId,
                AsOfDate = asOfDate ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetVendorBalanceReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Vendor Balance Report");
            worksheet.Cell(1, 1).Value = "As of Date";
            worksheet.Cell(1, 2).Value = "Vendor";
            worksheet.Cell(1, 3).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rows = model.BalanceList ?? new List<VendorBalanceReportItem>();
            int row = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(row, 1).Value = item.AsOfDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 2).Value = item.VendorName ?? "";
                worksheet.Cell(row, 3).Value = item.Balance;
                row++;
            }
            row++;
            worksheet.Cell(row, 2).Value = "Total (sum of balances):";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalBalance;
            worksheet.Cell(row, 3).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"VendorBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportVendorBalancePdf(long? vendorId = null, DateTime? asOfDate = null)
        {
            var filters = new VendorBalanceReportFilters
            {
                VendorId = vendorId,
                AsOfDate = asOfDate ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetVendorBalanceReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 40f, 40f, 40f, 40f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            document.Add(new Paragraph("Vendor Balance Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(model.ScopeLabel))
                document.Add(new Paragraph(model.ScopeLabel, FontFactory.GetFont(FontFactory.HELVETICA, 11)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(3);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2f, 3f, 2f });
            foreach (var h in new[] { "As of Date", "Vendor", "Balance" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                });
            }

            foreach (var item in model.BalanceList ?? new List<VendorBalanceReportItem>())
            {
                table.AddCell(new PdfPCell(new Phrase(item.AsOfDate.ToString("dd-MMM-yyyy"), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                table.AddCell(new PdfPCell(new Phrase(item.VendorName ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                var bal = item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")";
                table.AddCell(new PdfPCell(new Phrase(bal, FontFactory.GetFont(FontFactory.HELVETICA, 9))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var totalLabel = new PdfPCell(new Phrase("Total (sum of balances)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(totalLabel);
            var tb = model.TotalBalance;
            var totalStr = tb == 0 ? "-" : tb > 0 ? tb.ToString("N2") : "(" + (-tb).ToString("N2") + ")";
            table.AddCell(new PdfPCell(new Phrase(totalStr, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            });

            document.Add(table);
            document.Close();
            string filename = $"VendorBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> ExportBankCreditDebitExcel(int pageNumber = 1, int? pageSize = null, long? accountId = null, DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new BankCreditDebitReportFilters
            {
                PersonalPaymentId = accountId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now,
                TransactionType = transactionType
            };
            var model = await _reportService.GetBankCreditDebitReportForExport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bank Credit Debit Report");
            
            // Add header
            worksheet.Cell(1, 1).Value = "Transaction Date";
            worksheet.Cell(1, 2).Value = "Bank Name";
            worksheet.Cell(1, 3).Value = "Account Number";
            worksheet.Cell(1, 4).Value = "Account Holder";
            worksheet.Cell(1, 5).Value = "Branch";
            worksheet.Cell(1, 6).Value = "Type";
            worksheet.Cell(1, 7).Value = "Amount";
            worksheet.Cell(1, 8).Value = "Balance";
            worksheet.Cell(1, 9).Value = "Bill #";
            worksheet.Cell(1, 10).Value = "Description";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.TransactionList)
            {
                worksheet.Cell(row, 1).Value = item.TransactionDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 2).Value = item.BankName;
                worksheet.Cell(row, 3).Value = item.AccountNumber;
                worksheet.Cell(row, 4).Value = item.AccountHolderName;
                worksheet.Cell(row, 5).Value = item.BankBranch;
                worksheet.Cell(row, 6).Value = item.TransactionType;
                worksheet.Cell(row, 7).Value = item.Amount;
                worksheet.Cell(row, 8).Value = item.Balance;
                worksheet.Cell(row, 9).Value = item.BillNumber ?? 0;
                worksheet.Cell(row, 10).Value = item.TransactionDescription ?? "";
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 6).Value = "TOTAL:";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.NetBalance;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 6).Value = "Total Credit:";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalCreditAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 6).Value = "Total Debit:";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalDebitAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"BankCreditDebitReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportBankCreditDebitPdf(int pageNumber = 1, int? pageSize = null, long? accountId = null, DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
            var filters = new BankCreditDebitReportFilters
            {
                PersonalPaymentId = accountId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now,
                TransactionType = transactionType
            };

            var model = await _reportService.GetBankCreditDebitReportForExport(pageNumber, currentPageSize, filters);

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(document, stream);

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Bank Credit / Debit Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n")); // Add space

                // Table with 10 columns
                PdfPTable table = new PdfPTable(10);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 2f, 1.5f, 2f, 1.5f, 1f, 1.5f, 1.5f, 1f, 2.5f });

                // Header row
                string[] headers = { "Date", "Bank", "Account #", "Holder", "Branch", "Type", "Amount", "Balance", "Bill #", "Description" };

                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                // Data rows
                foreach (var item in model.TransactionList)
                {
                    table.AddCell(item.TransactionDate.ToString("dd-MMM-yyyy"));
                    table.AddCell(item.BankName ?? "");
                    table.AddCell(item.AccountNumber ?? "");
                    table.AddCell(item.AccountHolderName ?? "");
                    table.AddCell(item.BankBranch ?? "");
                    
                    var typeCell = new PdfPCell(new Phrase(item.TransactionType ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                    typeCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    if (item.TransactionType == "Credit")
                        typeCell.BackgroundColor = BaseColor.GREEN;
                    else if (item.TransactionType == "Debit")
                        typeCell.BackgroundColor = BaseColor.RED;
                    table.AddCell(typeCell);
                    
                    table.AddCell(item.Amount.ToString("N2"));
                    table.AddCell(item.Balance.ToString("N2"));
                    table.AddCell((item.BillNumber ?? 0).ToString());
                    table.AddCell(item.TransactionDescription ?? "");
                }

                // Summary rows
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 6,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                table.AddCell(new PdfPCell(new Phrase(model.NetBalance.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                var creditCell = new PdfPCell(new Phrase("Total Credit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 6,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(creditCell);
                table.AddCell(new PdfPCell(new Phrase(model.TotalCreditAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.GREEN });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                var debitCell = new PdfPCell(new Phrase("Total Debit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 6,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(debitCell);
                table.AddCell(new PdfPCell(new Phrase(model.TotalDebitAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.RED });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                document.Add(table);
                document.Close();
                string filename = $"BankCreditDebitReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

}
}
