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
        private const int DefaultPageSize = 10; // Default page size
        private static readonly int[] AllowedPageSizes = { 10, 20, 30 };
        public ReportController(IReportService reportService , ILogger<ReportController> logger, ICustomer customerService, IProductService productService)
        {
            _reportService = reportService;
            _logger = logger;
            _customerService = customerService;
            _productService = productService;
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
            worksheet.Cell(1, 1).InsertTable(model.SalesList);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"SalesReport_{DateTime.Now}.xlsx";
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

                document.Add(table);
                document.Close();
                string filename = $"SalesReport_{DateTime.Now}.pdf";
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

}
}
