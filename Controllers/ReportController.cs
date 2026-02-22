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
        public async Task<IActionResult> SalesReport(int pageNumber = 1, int? pageSize = null)
        {
            SalesReportsFilters salesReportsFilters = new SalesReportsFilters();
            salesReportsFilters.FromDate = DateTime.Now;
            salesReportsFilters.ToDate = DateTime.Now;
           
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
          
            var customers = await _customerService.GetAllEnabledCustomers();
            var selectedCustomer = HttpContext.Request.Query["SalesReportsFilters.CustomerIdFk"].ToString();

            long? selectedCustomerId = null;
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["custId"].ToString()))
            {
                selectedCustomerId = Convert.ToInt64(HttpContext.Request.Query["custId"].ToString());
            }
            if (!string.IsNullOrWhiteSpace(selectedCustomer))
            {

                selectedCustomerId = Convert.ToInt64(selectedCustomer);
                
            }

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);
            //ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", HttpContext.Request.Query["SalesReportsFilters.CustomerIdFk"].ToString()!=null ? Convert.ToInt64(HttpContext.Request.Query["SalesReportsFilters.CustomerIdFk"]): null);
                
            
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
            {
                salesReportsFilters.FromDate = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
            {
                salesReportsFilters.ToDate = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["SalesReportsFilters.CustomerIdFk"].ToString()))
            {
                salesReportsFilters.CustomerId = Convert.ToInt64(HttpContext.Request.Query["SalesReportsFilters.CustomerIdFk"]);
            }
            if (selectedCustomerId!=null && selectedCustomerId.Value>0)
            {
                salesReportsFilters.CustomerId = selectedCustomerId;
            }
            var model=await _reportService.GetAllSales(pageNumber, currentPageSize, salesReportsFilters);

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
                FromDate = fromDate ?? DateTime.Now, // default today if null
                ToDate = toDate ?? DateTime.Now
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
            string filename = $"SalesReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                FromDate = fromDate ?? DateTime.Now, // default today if null
                ToDate = toDate ?? DateTime.Now
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
                string filename = $"SalesReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ProfitLossReport(int pageNumber = 1, int? pageSize = null)
        {
            ProfitLossReportFilters filters = new ProfitLossReportFilters();
            filters.FromDate = DateTime.Now;
            filters.ToDate = DateTime.Now;
           
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
          
            var products = await _productService.GetAllEnabledProductsAsync();
            var selectedProduct = HttpContext.Request.Query["ProfitLossReportFilters.ProductId"].ToString();

            long? selectedProductId = null;
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["productId"].ToString()))
            {
                selectedProductId = Convert.ToInt64(HttpContext.Request.Query["productId"].ToString());
            }
            if (!string.IsNullOrWhiteSpace(selectedProduct))
            {
                selectedProductId = Convert.ToInt64(selectedProduct);
            }

            ViewBag.Products = new SelectList(products, "ProductId", "ProductName", selectedProductId);
                
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
            {
                filters.FromDate = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
            {
                filters.ToDate = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ProfitLossReportFilters.ProductId"].ToString()))
            {
                filters.ProductId = Convert.ToInt64(HttpContext.Request.Query["ProfitLossReportFilters.ProductId"]);
            }
            if (selectedProductId != null && selectedProductId.Value > 0)
            {
                filters.ProductId = selectedProductId;
            }
            var model = await _reportService.GetProductWiseProfitLoss(pageNumber, currentPageSize, filters);
            model.Filters = filters;

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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
            string filename = $"ProfitLossReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
                string filename = $"ProfitLossReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
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
                    model.Filters.ReportDate = DateTime.Now;
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
                ReportDate = reportDate ?? DateTime.Now
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
            string filename = $"DailyStockReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                ReportDate = reportDate ?? DateTime.Now
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
                string filename = $"DailyStockReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> PurchaseReport(int pageNumber = 1, int? pageSize = null)
        {
            PurchaseReportFilters filters = new PurchaseReportFilters();
            filters.FromDate = DateTime.Now;
            filters.ToDate = DateTime.Now;
           
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
          
            var vendors = await _vendorService.GetAllEnabledVendors();
            var selectedVendor = HttpContext.Request.Query["PurchaseReportFilters.VendorId"].ToString();

            long? selectedVendorId = null;
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["vendorId"].ToString()))
            {
                selectedVendorId = Convert.ToInt64(HttpContext.Request.Query["vendorId"].ToString());
            }
            if (!string.IsNullOrWhiteSpace(selectedVendor))
            {
                selectedVendorId = Convert.ToInt64(selectedVendor);
            }

            ViewBag.Vendors = new SelectList(vendors, "SupplierId", "SupplierName", selectedVendorId);
                
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
            {
                filters.FromDate = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
            {
                filters.ToDate = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["PurchaseReportFilters.VendorId"].ToString()))
            {
                filters.VendorId = Convert.ToInt64(HttpContext.Request.Query["PurchaseReportFilters.VendorId"]);
            }
            if (selectedVendorId != null && selectedVendorId.Value > 0)
            {
                filters.VendorId = selectedVendorId;
            }
            var model = await _reportService.GetPurchaseReport(pageNumber, currentPageSize, filters);
            model.Filters = filters;

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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
            string filename = $"PurchaseReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
                string filename = $"PurchaseReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> ProductWiseSalesReport(int pageNumber = 1, int? pageSize = null)
        {
            ProductWiseSalesReportFilters filters = new ProductWiseSalesReportFilters();
            filters.FromDate = DateTime.Now;
            filters.ToDate = DateTime.Now;
           
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
          
            var products = await _productService.GetAllEnabledProductsAsync();
            var selectedProduct = HttpContext.Request.Query["ProductWiseSalesReportFilters.ProductId"].ToString();

            long? selectedProductId = null;
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["productId"].ToString()))
            {
                selectedProductId = Convert.ToInt64(HttpContext.Request.Query["productId"].ToString());
            }
            if (!string.IsNullOrWhiteSpace(selectedProduct))
            {
                selectedProductId = Convert.ToInt64(selectedProduct);
            }

            ViewBag.Products = new SelectList(products, "ProductId", "ProductName", selectedProductId);
                
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
            {
                filters.FromDate = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
            {
                filters.ToDate = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ProductWiseSalesReportFilters.ProductId"].ToString()))
            {
                filters.ProductId = Convert.ToInt64(HttpContext.Request.Query["ProductWiseSalesReportFilters.ProductId"]);
            }
            if (selectedProductId != null && selectedProductId.Value > 0)
            {
                filters.ProductId = selectedProductId;
            }
            var model = await _reportService.GetProductWiseSalesReport(pageNumber, currentPageSize, filters);
            model.Filters = filters;

            return View(model);
        }

        public async Task<IActionResult> ExportProductWiseSalesExcel(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWiseSalesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
            string filename = $"ProductWiseSalesReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProductWiseSalesPdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWiseSalesReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
                string filename = $"ProductWiseSalesReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
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

                // Set default dates if not provided
                if (!model.Filters.FromDate.HasValue)
                {
                    model.Filters.FromDate = DateTime.Now;
                }
                if (!model.Filters.ToDate.HasValue)
                {
                    model.Filters.ToDate = DateTime.Now;
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

                // Reassign dropdown again (important after service call)
                ViewBag.Products = new SelectList(products, "ProductId", "ProductName", model.Filters.ProductId);
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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
            string filename = $"ProductWisePurchaseReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProductWisePurchasePdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filters = new ProductWisePurchaseReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now
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
                string filename = $"ProductWisePurchaseReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
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

                // Don't set default dates - allow null to get all expenses

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

        public async Task<IActionResult> DailyStockPositionReport(DateTime? reportDate = null)
        {
            try
            {
                var filters = new DailyStockPositionReportFilters
                {
                    ReportDate = reportDate ?? DateTime.Now
                };

                var model = await _reportService.GetDailyStockPositionReport(filters);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new DailyStockPositionReportViewModel
                {
                    Filters = new DailyStockPositionReportFilters { ReportDate = DateTime.Now }
                });
            }
        }

        public async Task<IActionResult> ExportDailyStockPositionExcel(DateTime? reportDate = null)
        {
            var filters = new DailyStockPositionReportFilters
            {
                ReportDate = reportDate ?? DateTime.Now
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
                ReportDate = reportDate ?? DateTime.Now
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
            string filename = $"{reportName.Replace(" ", "")}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                string filename = $"{reportTitle.Replace(" ", "")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }

        public async Task<IActionResult> BankCreditDebitReport(int pageNumber = 1, int? pageSize = null)
        {
            BankCreditDebitReportFilters filters = new BankCreditDebitReportFilters();
            filters.FromDate = DateTime.Now;
            filters.ToDate = DateTime.Now;
           
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }
          
            var personalPayments = await _vendorService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
            var selectedAccount = HttpContext.Request.Query["BankCreditDebitReportFilters.PersonalPaymentId"].ToString();

            long? selectedAccountId = null;
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["accountId"].ToString()))
            {
                selectedAccountId = Convert.ToInt64(HttpContext.Request.Query["accountId"].ToString());
            }
            if (!string.IsNullOrWhiteSpace(selectedAccount))
            {
                selectedAccountId = Convert.ToInt64(selectedAccount);
            }

            ViewBag.Accounts = new SelectList(
                personalPayments.PersonalPaymentList.Select(pp => new { 
                    PersonalPaymentId = pp.PersonalPaymentId, 
                    DisplayName = $"{pp.BankName} - {pp.AccountNumber} ({pp.AccountHolderName})" 
                }), 
                "PersonalPaymentId", 
                "DisplayName", 
                selectedAccountId);
                
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["FromDate"].ToString()))
            {
                filters.FromDate = Convert.ToDateTime(HttpContext.Request.Query["FromDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ToDate"].ToString()))
            {
                filters.ToDate = Convert.ToDateTime(HttpContext.Request.Query["ToDate"]);
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["BankCreditDebitReportFilters.PersonalPaymentId"].ToString()))
            {
                filters.PersonalPaymentId = Convert.ToInt64(HttpContext.Request.Query["BankCreditDebitReportFilters.PersonalPaymentId"]);
            }
            if (selectedAccountId != null && selectedAccountId.Value > 0)
            {
                filters.PersonalPaymentId = selectedAccountId;
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["TransactionType"].ToString()))
            {
                filters.TransactionType = HttpContext.Request.Query["TransactionType"].ToString();
            }
            var model = await _reportService.GetBankCreditDebitReport(pageNumber, currentPageSize, filters);
            model.Filters = filters;

            return View(model);
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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now,
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
            string filename = $"BankCreditDebitReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                FromDate = fromDate ?? DateTime.Now,
                ToDate = toDate ?? DateTime.Now,
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
                string filename = $"BankCreditDebitReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

}
}
