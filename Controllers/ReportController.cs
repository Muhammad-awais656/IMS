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
        private readonly IPersonalPaymentService _personalPaymentService;
        private readonly IStockService _stockService;
        private const int DefaultPageSize = 10; // Default page size
        private static readonly int[] AllowedPageSizes = { 10, 20, 30 };
        public ReportController(IReportService reportService , ILogger<ReportController> logger, ICustomer customerService, IProductService productService, IVendor vendorService, IExpenseType expenseTypeService, IPersonalPaymentService personalPaymentService, IStockService stockService)
        {
            _reportService = reportService;
            _logger = logger;
            _customerService = customerService;
            _productService = productService;
            _vendorService = vendorService;
            _expenseTypeService = expenseTypeService;
            _personalPaymentService = personalPaymentService;
            _stockService = stockService;
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
            worksheet.Cell(1, 3).Value = "Customer Urdu";
            worksheet.Cell(1, 4).Value = "Bill #";
            worksheet.Cell(1, 5).Value = "Sale Date";
            worksheet.Cell(1, 6).Value = "Total Amount";
            worksheet.Cell(1, 7).Value = "Discount";
            worksheet.Cell(1, 8).Value = "Paid Amount";
            worksheet.Cell(1, 9).Value = "Total Payable";
            worksheet.Cell(1, 10).Value = "Description";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.SalesList)
            {
                worksheet.Cell(row, 1).Value = item.SaleId;
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.CustomerName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.CustomerUrduName);
                worksheet.Cell(row, 4).Value = item.BillNumber;
                worksheet.Cell(row, 5).Value = item.SaleDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 6).Value = item.TotalAmount;
                worksheet.Cell(row, 7).Value = item.DiscountAmount;
                worksheet.Cell(row, 8).Value = item.TotalReceivedAmount;
                worksheet.Cell(row, 9).Value = item.TotalDueAmount;
                worksheet.Cell(row, 10).Value = item.SaleDescription ?? "";
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 5).Value = "TOTAL:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 7).Value = model.TotalDiscountAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalReceivedAmount;
            worksheet.Cell(row, 8).Style.Font.Bold = true;
            worksheet.Cell(row, 9).Value = model.TotalDueAmount;
            worksheet.Cell(row, 9).Style.Font.Bold = true;

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

                // Table with 10 columns
                PdfPTable table = new PdfPTable(10);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 2f, 2f, 1.2f, 1.8f, 1.8f, 1.8f, 1.8f, 1.8f, 2.5f });

                // Header row
                string[] headers = { "Sale Id", "Customer", "Customer Urdu", "Bill #", "Sale Date",
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
                    table.AddCell(NameDisplayHelper.EnglishNameCell(s.CustomerName));
                    table.AddCell(NameDisplayHelper.UrduNameCell(s.CustomerUrduName));
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
                    Colspan = 5,
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

                // Preserve filters before service call
                var filters = model.Filters;

                // Get filtered data using model.Filters
                model = await _reportService.GetProductWiseProfitLoss(pageNumber, currentPageSize, filters);

                // Reassign filters to ensure they're preserved
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                if (model == null)
                {
                    model = new ProfitLossReportViewModel();
                }
                model.Filters ??= new ProfitLossReportFilters();
                model.ProfitLossList ??= new List<ProfitLossReportItem>();
                model.ProductDetailSections ??= new List<ProductWiseProfitLossDetailSection>();
            }

            return View(model);
        }

        public async Task<IActionResult> ExportProfitLossExcel(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null, int? viewMode = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }

            if (viewMode == (int)ProfitLossReportViewMode.Overall)
            {
                var filtersOverall = new ProfitLossReportFilters
                {
                    ProductId = productId,
                    FromDate = fromDate ?? DateTimeHelper.Now,
                    ToDate = toDate ?? DateTimeHelper.Now,
                    ViewMode = ProfitLossReportViewMode.Overall
                };
                var modelOverall = await _reportService.GetProductWiseProfitLoss(1, null, filtersOverall);

                using var workbookOv = new XLWorkbook();
                var ws = workbookOv.Worksheets.Add("Overall P&L");
                int r = 1;
                ws.Cell(r, 1).Value = "Overall Profit and Loss Report";
                ws.Range(r, 1, r, 5).Merge();
                ws.Cell(r, 1).Style.Font.Bold = true;
                r += 2;
                ws.Cell(r, 1).Value = "Product";
                ws.Cell(r, 2).Value = "Sale amount";
                ws.Cell(r, 3).Value = "Total stock amount";
                ws.Cell(r, 4).Value = "Profit / (loss)";
                ws.Cell(r, 5).Value = "%";
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                r++;
                foreach (var item in modelOverall.ProfitLossList ?? new List<ProfitLossReportItem>())
                {
                    ws.Cell(r, 1).Value = NameDisplayHelper.EnglishNameCell(item.ProductName ?? "");
                    ws.Cell(r, 2).Value = item.TotalSalesAmount;
                    ws.Cell(r, 3).Value = item.TotalPurchaseCost;
                    ws.Cell(r, 4).Value = item.ProfitLoss;
                    ws.Cell(r, 5).Value = item.ProfitLossPercentage;
                    r++;
                }
                r++;
                ws.Cell(r, 1).Value = "Grand total";
                ws.Cell(r, 2).Value = modelOverall.TotalSalesAmount;
                ws.Cell(r, 3).Value = modelOverall.TotalPurchaseCost;
                ws.Cell(r, 4).Value = modelOverall.TotalProfitLoss;
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Columns().AdjustToContents();
                using var streamOv = new MemoryStream();
                workbookOv.SaveAs(streamOv);
                var filenameOv = $"ProfitLossReport_Overall_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
                return File(streamOv.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filenameOv);
            }

            var filters = new ProfitLossReportFilters
            {
                ProductId = productId,
                FromDate = fromDate ?? DateTimeHelper.Now,
                ToDate = toDate ?? DateTimeHelper.Now
            };
            var model = await _reportService.GetProductWiseProfitLossReport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Wise P&L");

            void writeRow(int r, string d, decimal? w, decimal? a, decimal? rate, bool bold = false)
            {
                worksheet.Cell(r, 1).Value = d;
                if (w.HasValue)
                    worksheet.Cell(r, 2).Value = w.Value;
                else
                    worksheet.Cell(r, 2).Value = string.Empty;
                if (a.HasValue)
                    worksheet.Cell(r, 3).Value = a.Value;
                else
                    worksheet.Cell(r, 3).Value = string.Empty;
                if (rate.HasValue)
                    worksheet.Cell(r, 4).Value = rate.Value;
                else
                    worksheet.Cell(r, 4).Value = string.Empty;
                if (bold)
                    worksheet.Range(r, 1, r, 4).Style.Font.Bold = true;
            }

            int row = 1;
            worksheet.Cell(row, 1).Value = "Product Wise Profit and Loss Report";
            worksheet.Range(row, 1, row, 4).Merge();
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row += 2;

            worksheet.Cell(row, 1).Value = "Description";
            worksheet.Cell(row, 2).Value = "Weight";
            worksheet.Cell(row, 3).Value = "Amount";
            worksheet.Cell(row, 4).Value = "Rate";
            worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
            row++;

            foreach (var p in model.ProductDetailSections ?? new List<ProductWiseProfitLossDetailSection>())
            {
                worksheet.Cell(row, 1).Value = NameDisplayHelper.EnglishNameCell(p.ProductName);
                worksheet.Range(row, 1, row, 4).Merge();
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
                writeRow(row++, "Previous", p.PreviousWeight, p.PreviousAmount, p.PreviousRate);
                writeRow(row++, "Purchase", p.PurchaseWeight, p.PurchaseAmount, p.PurchaseRate);
                writeRow(row++, "Purchase Exp", null, p.PurchaseExpenseAmount, null);
                writeRow(row++, "Total Stock", p.TotalStockWeight, p.TotalStockAmount, p.TotalStockRate, true);
                writeRow(row++, "Sale", p.SaleWeight, p.SaleAmount, p.SaleRate);
                writeRow(row++, "Balance Stock", p.BalanceStockWeight, p.BalanceStockAmount, p.BalanceRate, true);
                if (p.BagsWeight != 0 || p.BagsRate != 0)
                    writeRow(row++, "Bags", p.BagsWeight, p.BagsAmount, p.BagsRate, true);
                worksheet.Cell(row, 1).Value = "Profit";
                worksheet.Cell(row, 3).Value = p.Profit;
                worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.Black;
                worksheet.Range(row, 1, row, 4).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(row, 3).Style.Font.Bold = true;
                row += 2;
            }

            row++;
            worksheet.Cell(row, 1).Value = "GRAND TOTAL";
            worksheet.Cell(row, 2).Value = "Sales";
            worksheet.Cell(row, 3).Value = model.TotalSalesAmount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
            worksheet.Cell(row, 2).Value = "Stock value";
            worksheet.Cell(row, 3).Value = model.TotalPurchaseCost;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
            worksheet.Cell(row, 2).Value = "Profit";
            worksheet.Cell(row, 3).Value = model.TotalProfitLoss;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(row - 2, 1, row, 4).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"ProfitLossReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportProfitLossPdf(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? fromDate = null, DateTime? toDate = null, int? viewMode = null)
        {
            int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
            if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
            {
                currentPageSize = pageSize.Value;
                HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
            }

            if (viewMode == (int)ProfitLossReportViewMode.Overall)
            {
                var filtersOverall = new ProfitLossReportFilters
                {
                    ProductId = productId,
                    FromDate = fromDate ?? DateTimeHelper.Now,
                    ToDate = toDate ?? DateTimeHelper.Now,
                    ViewMode = ProfitLossReportViewMode.Overall
                };
                var modelOverall = await _reportService.GetProductWiseProfitLoss(1, null, filtersOverall);

                using (var streamOv = new MemoryStream())
                {
                    var documentOv = new Document(PageSize.A4.Rotate(), 24f, 24f, 24f, 24f);
                    PdfWriter.GetInstance(documentOv, streamOv);
                    documentOv.Open();
                    var titleFontOv = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    documentOv.Add(new Paragraph("Overall Profit and Loss Report", titleFontOv) { Alignment = Element.ALIGN_CENTER });
                    documentOv.Add(new Paragraph("\n"));

                    var tableOv = new PdfPTable(5);
                    tableOv.WidthPercentage = 100;
                    tableOv.SetWidths(new float[] { 3f, 1.4f, 1.4f, 1.4f, 1f });

                    void hdrOv(string a, string b, string c, string d, string e)
                    {
                        tableOv.AddCell(H2(a));
                        tableOv.AddCell(H2(b));
                        tableOv.AddCell(H2(c));
                        tableOv.AddCell(H2(d));
                        tableOv.AddCell(H2(e));
                    }
                    PdfPCell H2(string t) => new PdfPCell(new Phrase(t, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    hdrOv("Product", "Sale amount", "Total stock amount", "Profit / (loss)", "%");

                    var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                    foreach (var item in modelOverall.ProfitLossList ?? new List<ProfitLossReportItem>())
                    {
                        tableOv.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.ProductName ?? ""), cellFont)));
                        tableOv.AddCell(new PdfPCell(new Phrase(item.TotalSalesAmount.ToString("N2"), cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                        tableOv.AddCell(new PdfPCell(new Phrase(item.TotalPurchaseCost.ToString("N2"), cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                        tableOv.AddCell(new PdfPCell(new Phrase(item.ProfitLoss.ToString("N2"), cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                        tableOv.AddCell(new PdfPCell(new Phrase(item.ProfitLossPercentage.ToString("N2"), cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    }
                    var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                    tableOv.AddCell(new PdfPCell(new Phrase("Grand total", boldFont)));
                    tableOv.AddCell(new PdfPCell(new Phrase(modelOverall.TotalSalesAmount.ToString("N2"), boldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    tableOv.AddCell(new PdfPCell(new Phrase(modelOverall.TotalPurchaseCost.ToString("N2"), boldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    tableOv.AddCell(new PdfPCell(new Phrase(modelOverall.TotalProfitLoss.ToString("N2"), boldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    tableOv.AddCell(new PdfPCell(new Phrase("", boldFont)));

                    documentOv.Add(tableOv);
                    documentOv.Close();
                    var filenameOv = $"ProfitLossReport_Overall_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                    return File(streamOv.ToArray(), "application/pdf", filenameOv);
                }
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
                document.Add(new Paragraph("Product Wise Profit and Loss Report", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3.2f, 1.6f, 1.6f, 1.6f });

                void hdr(string a, string b, string c, string d)
                {
                    table.AddCell(H(a));
                    table.AddCell(H(b));
                    table.AddCell(H(c));
                    table.AddCell(H(d));
                }
                PdfPCell H(string t) => new PdfPCell(new Phrase(t, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                void row(string desc, string w, string amt, string rate, bool bold = false)
                {
                    var f = FontFactory.GetFont(bold ? FontFactory.HELVETICA_BOLD : FontFactory.HELVETICA, 8);
                    table.AddCell(new PdfPCell(new Phrase(desc, f)));
                    table.AddCell(new PdfPCell(new Phrase(w, f)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(amt, f)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(rate, f)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                foreach (var p in model.ProductDetailSections ?? new List<ProductWiseProfitLossDetailSection>())
                {
                    var titleCell = new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(p.ProductName), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    {
                        Colspan = 4,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(titleCell);
                    hdr("Description", "Weight", "Amount", "Rate");
                    row("Previous", p.PreviousWeight.ToString("N2"), p.PreviousAmount.ToString("N2"), p.PreviousRate.ToString("N2"));
                    row("Purchase", p.PurchaseWeight.ToString("N2"), p.PurchaseAmount.ToString("N2"), p.PurchaseRate.ToString("N2"));
                    row("Purchase Exp", "", p.PurchaseExpenseAmount.ToString("N2"), "");
                    row("Total Stock", p.TotalStockWeight.ToString("N2"), p.TotalStockAmount.ToString("N2"), p.TotalStockRate.ToString("N2"), true);
                    row("Sale", p.SaleWeight.ToString("N2"), p.SaleAmount.ToString("N2"), p.SaleRate.ToString("N2"));
                    row("Balance Stock", p.BalanceStockWeight.ToString("N2"), p.BalanceStockAmount.ToString("N2"), p.BalanceRate.ToString("N2"), true);
                    if (p.BagsWeight != 0 || p.BagsRate != 0)
                        row("Bags", p.BagsWeight.ToString("N2"), p.BagsAmount.ToString("N2"), p.BagsRate.ToString("N2"), true);
                    var wf = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                    table.AddCell(new PdfPCell(new Phrase("Profit", wf)) { BackgroundColor = BaseColor.BLACK });
                    table.AddCell(new PdfPCell(new Phrase("—", wf)) { BackgroundColor = BaseColor.BLACK, HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(p.Profit.ToString("N2"), wf))
                    {
                        Colspan = 2,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        BackgroundColor = BaseColor.BLACK
                    });
                    table.AddCell(new PdfPCell(new Phrase("\n")) { Colspan = 4, MinimumHeight = 6f });
                }

                row("GRAND TOTAL Sales", "", model.TotalSalesAmount.ToString("N2"), "", true);
                row("GRAND TOTAL Stock value", "", model.TotalPurchaseCost.ToString("N2"), "", true);
                row("GRAND TOTAL Profit", "", model.TotalProfitLoss.ToString("N2"), "", true);

                document.Add(table);
                document.Close();
                string filename = $"ProfitLossReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", filename);
            }
        }

        public async Task<IActionResult> BankBalancesReport(BankBalancesReportViewModel model)
        {
            try
            {
                if (model == null)
                    model = new BankBalancesReportViewModel();
                if (model.Filters == null)
                    model.Filters = new BankBalancesReportFilters();

                var hasReportDateParam = Request.Query.ContainsKey("Filters.ReportDate");
                if (!hasReportDateParam && !model.Filters.ReportDate.HasValue)
                    model.Filters.ReportDate = DateTimeHelper.Now;

                var personalPayments = await _vendorService.GetAllPersonalPaymentsAsync(1, 1000, new PersonalPaymentFilters { IsActive = true });
                var filters = model.Filters;

                var bankAccountItems = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- All bank accounts --" }
                };
                foreach (var pp in personalPayments.PersonalPaymentList)
                {
                    bankAccountItems.Add(new SelectListItem
                    {
                        Value = pp.PersonalPaymentId.ToString(),
                        Text = $"{pp.BankName} - {pp.AccountNumber} ({pp.AccountHolderName})"
                    });
                }
                ViewBag.BankAccounts = bankAccountItems;

                model = await _reportService.GetBankBalancesReport(filters);
                model.Filters = filters;

                foreach (var it in bankAccountItems)
                {
                    it.Selected = string.IsNullOrEmpty(it.Value)
                        ? (!model.Filters.PersonalPaymentId.HasValue || model.Filters.PersonalPaymentId.Value <= 0)
                        : model.Filters.PersonalPaymentId.HasValue && it.Value == model.Filters.PersonalPaymentId.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model ??= new BankBalancesReportViewModel();
                if (model.Filters == null)
                    model.Filters = new BankBalancesReportFilters();
                model.Rows ??= new List<BankBalancesReportRow>();
                ViewBag.BankAccounts = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- All bank accounts --" } };
            }

            return View(model);
        }

        public async Task<IActionResult> ExportBankBalancesExcel(DateTime? reportDate = null, long? personalPaymentId = null)
        {
            var filters = new BankBalancesReportFilters
            {
                ReportDate = reportDate ?? DateTimeHelper.Now,
                PersonalPaymentId = personalPaymentId
            };
            var model = await _reportService.GetBankBalancesReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bank Balances");

            worksheet.Cell(1, 1).Value = "Bank Balances Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 5).Merge();

            worksheet.Cell(2, 1).Value = "As of";
            worksheet.Cell(2, 2).Value = filters.ReportDate?.ToString("dd-MMM-yyyy") ?? "";

            int row = 4;
            worksheet.Cell(row, 1).Value = "Bank";
            worksheet.Cell(row, 2).Value = "Account #";
            worksheet.Cell(row, 3).Value = "Account holder";
            worksheet.Cell(row, 4).Value = "Branch";
            worksheet.Cell(row, 5).Value = "Balance";
            var headerRange = worksheet.Range(row, 1, row, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var r in model.Rows)
            {
                worksheet.Cell(row, 1).Value = r.BankName;
                worksheet.Cell(row, 2).Value = r.AccountNumber;
                worksheet.Cell(row, 3).Value = r.AccountHolderName;
                worksheet.Cell(row, 4).Value = r.BankBranch ?? "";
                worksheet.Cell(row, 5).Value = r.BalanceAsOf;
                row++;
            }

            row++;
            worksheet.Cell(row, 4).Value = "Total";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalBalance;
            worksheet.Cell(row, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var filename = $"BankBalancesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportBankBalancesPdf(DateTime? reportDate = null, long? personalPaymentId = null)
        {
            var filters = new BankBalancesReportFilters
            {
                ReportDate = reportDate ?? DateTimeHelper.Now,
                PersonalPaymentId = personalPaymentId
            };
            var model = await _reportService.GetBankBalancesReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 30f, 30f, 30f, 30f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            document.Add(new Paragraph("Bank Balances Report", titleFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph(
                $"As of: {filters.ReportDate:dd-MMM-yyyy}",
                FontFactory.GetFont(FontFactory.HELVETICA, 10)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2.2f, 1.5f, 2f, 1.5f, 1.5f });

            void AddHeader(string text)
            {
                table.AddCell(new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }

            AddHeader("Bank");
            AddHeader("Account #");
            AddHeader("Account holder");
            AddHeader("Branch");
            AddHeader("Balance");

            foreach (var r in model.Rows)
            {
                table.AddCell(new PdfPCell(new Phrase(r.BankName, FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(r.AccountNumber, FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(r.AccountHolderName, FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(r.BankBranch ?? "", FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(r.BalanceAsOf.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA, 8)))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
            }

            var totalCell = new PdfPCell(new Phrase("Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 4,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            table.AddCell(totalCell);
            table.AddCell(new PdfPCell(new Phrase(model.TotalBalance.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT
            });

            document.Add(table);
            document.Close();
            var filename = $"BankBalancesReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> BankLedgerReport(BankLedgerReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                if (model == null)
                    model = new BankLedgerReportViewModel();
                if (model.Filters == null)
                    model.Filters = new BankLedgerReportFilters();

                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                    model.Filters.ToDate = DateTimeHelper.Now;

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                var filters = model.Filters;

                if (filters.PersonalPaymentId.HasValue && filters.PersonalPaymentId.Value > 0)
                {
                    var tt = string.IsNullOrWhiteSpace(filters.TransactionType) ? null : filters.TransactionType;
                    var data = await _personalPaymentService.GetBankLedgerReportAsync(
                        filters.PersonalPaymentId.Value,
                        pageNumber,
                        currentPageSize,
                        filters.FromDate,
                        filters.ToDate,
                        tt);
                    data.Filters = filters;
                    model = data;
                }
                else
                {
                    model.Transactions = new List<PersonalPaymentTransactionViewModel>();
                    model.AccountSummary = null;
                    model.TotalCount = 0;
                    model.CurrentPage = 1;
                    model.TotalPages = 1;
                    model.PageSize = currentPageSize;
                    model.Filters = filters;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model ??= new BankLedgerReportViewModel();
                if (model.Filters == null)
                    model.Filters = new BankLedgerReportFilters();
            }

            return View(model);
        }

        public async Task<IActionResult> ExportBankLedgerExcel(long? personalPaymentId = null, DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            if (!personalPaymentId.HasValue || personalPaymentId.Value <= 0)
                return BadRequest("Select a bank account.");

            var tt = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;
            var data = await _personalPaymentService.GetBankLedgerReportAsync(personalPaymentId.Value, 1, 50000, fromDate, toDate, tt);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bank Ledger");
            worksheet.Cell(1, 1).Value = "Bank Ledger Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Value = data.AccountSummary != null
                ? $"{data.AccountSummary.BankName} — {data.AccountSummary.AccountNumber}"
                : "";
            int row = 4;
            worksheet.Cell(row, 1).Value = "Date";
            worksheet.Cell(row, 2).Value = "Type";
            worksheet.Cell(row, 3).Value = "Amount";
            worksheet.Cell(row, 4).Value = "Balance";
            worksheet.Cell(row, 5).Value = "Description";
            worksheet.Cell(row, 6).Value = "Sale reference";
            row++;
            foreach (var t in data.Transactions)
            {
                worksheet.Cell(row, 1).Value = t.TransactionDate;
                worksheet.Cell(row, 2).Value = t.TransactionType;
                worksheet.Cell(row, 3).Value = t.Amount;
                worksheet.Cell(row, 4).Value = t.Balance;
                worksheet.Cell(row, 5).Value = t.TransactionDescription ?? "";
                worksheet.Cell(row, 6).Value = FormatBankLedgerSaleReference(t);
                row++;
            }
            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var filename = $"BankLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportBankLedgerPdf(long? personalPaymentId = null, DateTime? fromDate = null, DateTime? toDate = null, string? transactionType = null)
        {
            if (!personalPaymentId.HasValue || personalPaymentId.Value <= 0)
                return BadRequest("Select a bank account.");

            var tt = string.IsNullOrWhiteSpace(transactionType) ? null : transactionType;
            var data = await _personalPaymentService.GetBankLedgerReportAsync(personalPaymentId.Value, 1, 50000, fromDate, toDate, tt);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);
            document.Open();
            document.Add(new Paragraph("Bank Ledger Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)) { Alignment = Element.ALIGN_CENTER });
            if (data.AccountSummary != null)
            {
                document.Add(new Paragraph($"{data.AccountSummary.BankName} — {data.AccountSummary.AccountNumber} ({data.AccountSummary.AccountHolderName})",
                    FontFactory.GetFont(FontFactory.HELVETICA, 10)) { Alignment = Element.ALIGN_CENTER });
            }
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.2f, 1f, 1.2f, 1.2f, 2.5f, 1.5f });
            string[] headers = { "Date", "Type", "Amount", "Balance", "Description", "Sale ref." };
            foreach (var h in headers)
            {
                table.AddCell(new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }
            foreach (var t in data.Transactions)
            {
                table.AddCell(t.TransactionDate.ToString("dd-MMM-yyyy"));
                table.AddCell(t.TransactionType);
                table.AddCell(t.Amount.ToString("N2"));
                table.AddCell(t.Balance.ToString("N2"));
                table.AddCell(t.TransactionDescription ?? "");
                table.AddCell(FormatBankLedgerSaleReference(t));
            }
            document.Add(table);
            document.Close();
            var filename = $"BankLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> StockTransactionsReport(StockTransactionsReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                if (model == null)
                    model = new StockTransactionsReportViewModel();
                if (model.Filters == null)
                    model.Filters = new StockHistoryFilters();

                var hasFromDateParam = Request.Query.ContainsKey("Filters.FromDate");
                var hasToDateParam = Request.Query.ContainsKey("Filters.ToDate");
                if (!hasFromDateParam && !model.Filters.FromDate.HasValue)
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                if (!hasToDateParam && !model.Filters.ToDate.HasValue)
                    model.Filters.ToDate = DateTimeHelper.Now;

                if (!model.Filters.TransactionTypeId.HasValue || model.Filters.TransactionTypeId == 0)
                    model.Filters.TransactionTypeId = null;

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                await PopulateStockTransactionsReportAsync(model, pageNumber, currentPageSize);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model ??= new StockTransactionsReportViewModel();
                if (model.Filters == null)
                    model.Filters = new StockHistoryFilters();
            }

            return View(model);
        }

        public async Task<IActionResult> ExportStockTransactionsExcel(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null, long? transactionTypeId = null)
        {
            if (!productId.HasValue || productId.Value <= 0)
                return BadRequest("Select a product.");

            var model = new StockTransactionsReportViewModel
            {
                Filters = new StockHistoryFilters
                {
                    ProductId = productId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TransactionTypeId = transactionTypeId.HasValue && transactionTypeId.Value > 0 ? transactionTypeId : null
                }
            };
            await PopulateStockTransactionsReportAsync(model, 1, int.MaxValue);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stock Transactions");
            worksheet.Cell(1, 1).Value = "Stock Transactions Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Value = string.IsNullOrEmpty(model.ProductName)
                ? ""
                : $"{model.ProductName} ({model.ProductCode ?? ""})";
            int row = 4;
            worksheet.Cell(row, 1).Value = "Transaction ID";
            worksheet.Cell(row, 2).Value = "Quantity";
            worksheet.Cell(row, 3).Value = "Date";
            worksheet.Cell(row, 4).Value = "Type";
            worksheet.Cell(row, 5).Value = "Description";
            row++;
            foreach (var t in model.TransactionList)
            {
                worksheet.Cell(row, 1).Value = t.StockTransactionId;
                worksheet.Cell(row, 2).Value = t.StockQuantity;
                worksheet.Cell(row, 3).Value = t.TransactionDate;
                worksheet.Cell(row, 4).Value = t.TransactionType;
                worksheet.Cell(row, 5).Value = t.Description ?? "";
                row++;
            }
            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var filename = $"StockTransactionsReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportStockTransactionsPdf(long? productId = null, DateTime? fromDate = null, DateTime? toDate = null, long? transactionTypeId = null)
        {
            if (!productId.HasValue || productId.Value <= 0)
                return BadRequest("Select a product.");

            var model = new StockTransactionsReportViewModel
            {
                Filters = new StockHistoryFilters
                {
                    ProductId = productId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TransactionTypeId = transactionTypeId.HasValue && transactionTypeId.Value > 0 ? transactionTypeId : null
                }
            };
            await PopulateStockTransactionsReportAsync(model, 1, int.MaxValue);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);
            document.Open();
            document.Add(new Paragraph("Stock Transactions Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrEmpty(model.ProductName))
            {
                document.Add(new Paragraph($"{model.ProductName} ({model.ProductCode ?? ""})",
                    FontFactory.GetFont(FontFactory.HELVETICA, 10)) { Alignment = Element.ALIGN_CENTER });
            }
            document.Add(new Paragraph("\n"));
            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.2f, 1.2f, 1.5f, 1.5f, 3f });
            foreach (var h in new[] { "Txn ID", "Quantity", "Date", "Type", "Description" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }
            foreach (var t in model.TransactionList)
            {
                table.AddCell(t.StockTransactionId.ToString());
                table.AddCell(t.StockQuantity.ToString("N4"));
                table.AddCell(t.TransactionDate.ToString("dd-MMM-yyyy"));
                table.AddCell(t.TransactionType ?? "");
                table.AddCell(t.Description ?? "");
            }
            document.Add(table);
            document.Close();
            var filename = $"StockTransactionsReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        private async Task PopulateStockTransactionsReportAsync(StockTransactionsReportViewModel model, int pageNumber, int currentPageSize)
        {
            model.TransactionList = new List<StockTransactionHistoryViewModel>();
            model.TotalCount = 0;
            model.CurrentPage = pageNumber;
            model.TotalPages = 1;
            model.PageSize = currentPageSize;
            model.ProductName = null;
            model.ProductCode = null;
            model.AvailableQuantity = null;

            if (model.Filters == null || !model.Filters.ProductId.HasValue || model.Filters.ProductId.Value <= 0)
                return;

            var stock = await _stockService.GetStockByProductIdAsync(model.Filters.ProductId.Value);
            if (stock == null || stock.StockMasterId <= 0)
            {
                TempData["ErrorMessage"] = "No stock record exists for this product. Add stock first.";
                return;
            }

            try
            {
                var pv = await _productService.GetProductByIdAsync(model.Filters.ProductId.Value);
                if (pv?.ProductList != null)
                {
                    model.ProductName = pv.ProductList.ProductName;
                    model.ProductCode = pv.ProductList.ProductCode;
                }
            }
            catch
            {
            }

            model.AvailableQuantity = stock.AvailableQuantity;

            var histFilters = new StockHistoryFilters
            {
                StockMasterId = stock.StockMasterId,
                FromDate = model.Filters.FromDate,
                ToDate = model.Filters.ToDate,
                TransactionTypeId = model.Filters.TransactionTypeId.HasValue && model.Filters.TransactionTypeId.Value > 0
                    ? model.Filters.TransactionTypeId
                    : null
            };

            var raw = await _stockService.GetStockHistoryAsync(1, null, histFilters);
            var all = raw.TransactionList ?? new List<StockTransactionHistoryViewModel>();
            var total = raw.TotalCount > 0 ? raw.TotalCount : all.Count;
            model.TotalCount = total;
            model.TotalPages = currentPageSize > 0 ? Math.Max(1, (int)Math.Ceiling(total / (double)currentPageSize)) : 1;
            model.CurrentPage = pageNumber;
            var skip = Math.Max(0, (pageNumber - 1) * currentPageSize);
            model.TransactionList = all.Skip(skip).Take(currentPageSize).ToList();
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

        public async Task<IActionResult> StockAvailableBalanceReport(DailyStockReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                if (model == null)
                    model = new DailyStockReportViewModel();
                if (model.Filters == null)
                    model.Filters = new DailyStockReportFilters();
                if (!model.Filters.ReportDate.HasValue)
                    model.Filters.ReportDate = DateTimeHelper.Now;

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                var filters = model.Filters;
                model = await _reportService.GetDailyStockReport(pageNumber, currentPageSize, filters);
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model ??= new DailyStockReportViewModel();
                if (model.Filters == null)
                    model.Filters = new DailyStockReportFilters();
                model.StockList ??= new List<DailyStockReportItem>();
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
            worksheet.Cell(1, 2).Value = "Urdu Name";
            worksheet.Cell(1, 3).Value = "Product Code";
            worksheet.Cell(1, 4).Value = "Total Quantity";
            worksheet.Cell(1, 5).Value = "Used Quantity";
            worksheet.Cell(1, 6).Value = "Available Quantity";
            worksheet.Cell(1, 7).Value = "Unit Price";
            worksheet.Cell(1, 8).Value = "Stock Value";
            worksheet.Cell(1, 9).Value = "Stock Location";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.StockList)
            {
                worksheet.Cell(row, 1).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                worksheet.Cell(row, 2).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                worksheet.Cell(row, 3).Value = item.ProductCode;
                worksheet.Cell(row, 4).Value = item.TotalQuantity;
                worksheet.Cell(row, 5).Value = item.UsedQuantity;
                worksheet.Cell(row, 6).Value = item.AvailableQuantity;
                worksheet.Cell(row, 7).Value = item.UnitPrice;
                worksheet.Cell(row, 8).Value = item.StockValue;
                worksheet.Cell(row, 9).Value = item.StockLocation;
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 3).Value = "TOTAL:";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalQuantity;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalUsedQuantity;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalAvailableQuantity;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalStockValue;
            worksheet.Cell(row, 8).Style.Font.Bold = true;

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

                // Table with 9 columns
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2.5f, 2.5f, 1.5f, 1.2f, 1.2f, 1.2f, 1.2f, 1.5f, 1.5f });

                // Header row
                string[] headers = { "Product Name", "Urdu Name", "Product Code", "Total Qty", "Used Qty", "Available Qty", "Unit Price", "Stock Value", "Location" };

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
                    table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                    table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
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
                    Colspan = 3,
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

        public async Task<IActionResult> ExportStockAvailableBalanceExcel(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? reportDate = null, long? displayMeasuringUnitId = null)
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
                ReportDate = reportDate ?? DateTimeHelper.Now,
                DisplayMeasuringUnitId = displayMeasuringUnitId
            };
            var model = await _reportService.GetDailyStockReportForExport(pageNumber, currentPageSize, filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stock Available Balance");

            var qtySuffix = StockAvailableBalanceExportQtyHeaderSuffix(model);
            const int headerRow = 3;
            worksheet.Cell(1, 1).Value = "Stock Available Balance Report" + StockAvailableBalanceExportTitleSuffix(model);
            worksheet.Range(1, 1, 1, 9).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = StockAvailableBalanceExportMeasuringUnitNote(model);
            worksheet.Range(2, 1, 2, 9).Merge();
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, 1).Style.Font.Italic = true;

            worksheet.Cell(headerRow, 1).Value = "Product Name";
            worksheet.Cell(headerRow, 2).Value = "Urdu Name";
            worksheet.Cell(headerRow, 3).Value = "Product Code";
            worksheet.Cell(headerRow, 4).Value = "Total Quantity" + qtySuffix;
            worksheet.Cell(headerRow, 5).Value = "Used Quantity" + qtySuffix;
            worksheet.Cell(headerRow, 6).Value = "Available Quantity" + qtySuffix;
            worksheet.Cell(headerRow, 7).Value = "Unit Price";
            worksheet.Cell(headerRow, 8).Value = "Stock Value";
            worksheet.Cell(headerRow, 9).Value = "Stock Location";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = headerRow + 1;
            foreach (var item in model.StockList)
            {
                worksheet.Cell(row, 1).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                worksheet.Cell(row, 2).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                worksheet.Cell(row, 3).Value = item.ProductCode;
                worksheet.Cell(row, 4).Value = item.TotalQuantity;
                worksheet.Cell(row, 5).Value = item.UsedQuantity;
                worksheet.Cell(row, 6).Value = item.AvailableQuantity;
                worksheet.Cell(row, 7).Value = item.UnitPrice;
                worksheet.Cell(row, 8).Value = item.StockValue;
                worksheet.Cell(row, 9).Value = item.StockLocation;
                row++;
            }

            row++;
            worksheet.Cell(row, 3).Value = "TOTAL:";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalQuantity;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalUsedQuantity;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalAvailableQuantity;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalStockValue;
            worksheet.Cell(row, 8).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"StockAvailableBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportStockAvailableBalancePdf(int pageNumber = 1, int? pageSize = null, long? productId = null, DateTime? reportDate = null, long? displayMeasuringUnitId = null)
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
                ReportDate = reportDate ?? DateTimeHelper.Now,
                DisplayMeasuringUnitId = displayMeasuringUnitId
            };

            var model = await _reportService.GetDailyStockReportForExport(pageNumber, currentPageSize, filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("Stock Available Balance Report" + StockAvailableBalanceExportTitleSuffix(model), titleFont) { Alignment = Element.ALIGN_CENTER });
            var noteFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9);
            document.Add(new Paragraph(StockAvailableBalanceExportMeasuringUnitNote(model), noteFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            PdfPTable table = new PdfPTable(9);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2.5f, 2.5f, 1.5f, 1.2f, 1.2f, 1.2f, 1.2f, 1.5f, 1.5f });

            var pdfQtySuf = StockAvailableBalanceExportQtyHeaderSuffix(model);
            string[] headers =
            {
                "Product Name", "Urdu Name", "Product Code",
                "Total Qty" + pdfQtySuf, "Used Qty" + pdfQtySuf, "Available Qty" + pdfQtySuf,
                "Unit Price", "Stock Value", "Location"
            };

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

            foreach (var item in model.StockList)
            {
                table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
                table.AddCell(item.ProductCode ?? "");
                table.AddCell(item.TotalQuantity.ToString("N2"));
                table.AddCell(item.UsedQuantity.ToString("N2"));
                table.AddCell(item.AvailableQuantity.ToString("N2"));
                table.AddCell(item.UnitPrice.ToString("N2"));
                table.AddCell(item.StockValue.ToString("N2"));
                table.AddCell(item.StockLocation ?? "");
            }

            var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 3,
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
            string filename = $"StockAvailableBalanceReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        private static string StockAvailableBalanceExportQtyHeaderSuffix(DailyStockReportViewModel m)
        {
            var abbr = m.DisplayMeasuringUnitAbbreviation?.Trim();
            var name = m.DisplayMeasuringUnitName?.Trim();
            if (!string.IsNullOrEmpty(abbr)) return $" ({abbr})";
            if (!string.IsNullOrEmpty(name)) return $" ({name})";
            return string.Empty;
        }

        private static string StockAvailableBalanceExportMeasuringUnitNote(DailyStockReportViewModel m)
        {
            var name = m.DisplayMeasuringUnitName?.Trim();
            var abbr = m.DisplayMeasuringUnitAbbreviation?.Trim();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(abbr))
                return "Measuring unit for Total, Used, and Available columns: each product's base (smallest) unit.";
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(abbr) && !string.Equals(name, abbr, StringComparison.OrdinalIgnoreCase))
                return $"Measuring unit for Total, Used, and Available columns: {name} ({abbr}).";
            var label = !string.IsNullOrEmpty(abbr) ? abbr : name;
            return $"Measuring unit for Total, Used, and Available columns: {label}.";
        }

        private static string StockAvailableBalanceExportTitleSuffix(DailyStockReportViewModel m)
        {
            var name = m.DisplayMeasuringUnitName?.Trim();
            var abbr = m.DisplayMeasuringUnitAbbreviation?.Trim();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(abbr))
                return string.Empty;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(abbr) && !string.Equals(name, abbr, StringComparison.OrdinalIgnoreCase))
                return $" — quantities in {name} ({abbr})";
            var label = !string.IsNullOrEmpty(abbr) ? abbr : name;
            return $" — quantities in {label}";
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
            worksheet.Cell(1, 3).Value = "Vendor Urdu";
            worksheet.Cell(1, 4).Value = "Customer Name";
            worksheet.Cell(1, 5).Value = "Customer Urdu";
            worksheet.Cell(1, 6).Value = "Bill #";
            worksheet.Cell(1, 7).Value = "Purchase Date";
            worksheet.Cell(1, 8).Value = "Total Amount";
            worksheet.Cell(1, 9).Value = "Discount";
            worksheet.Cell(1, 10).Value = "Paid Amount";
            worksheet.Cell(1, 11).Value = "Due Amount";
            worksheet.Cell(1, 12).Value = "Description";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data
            int row = 2;
            foreach (var item in model.PurchaseList)
            {
                worksheet.Cell(row, 1).Value = item.PurchaseOrderId;
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.VendorName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.VendorUrduName);
                worksheet.Cell(row, 4).Value = NameDisplayHelper.EnglishNameCell(item.CustomerName);
                worksheet.Cell(row, 5).Value = NameDisplayHelper.UrduNameCell(item.CustomerUrduName);
                worksheet.Cell(row, 6).Value = item.BillNumber;
                worksheet.Cell(row, 7).Value = item.PurchaseDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 8).Value = item.TotalAmount;
                worksheet.Cell(row, 9).Value = item.DiscountAmount;
                worksheet.Cell(row, 10).Value = item.PaidAmount;
                worksheet.Cell(row, 11).Value = item.DueAmount;
                worksheet.Cell(row, 12).Value = item.PurchaseDescription ?? "";
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 7).Value = "TOTAL:";
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            worksheet.Cell(row, 8).Value = model.TotalAmount;
            worksheet.Cell(row, 8).Style.Font.Bold = true;
            worksheet.Cell(row, 9).Value = model.TotalDiscountAmount;
            worksheet.Cell(row, 9).Style.Font.Bold = true;
            worksheet.Cell(row, 10).Value = model.TotalPaidAmount;
            worksheet.Cell(row, 10).Style.Font.Bold = true;
            worksheet.Cell(row, 11).Value = model.TotalDueAmount;
            worksheet.Cell(row, 11).Style.Font.Bold = true;

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

                // Table with 12 columns
                PdfPTable table = new PdfPTable(12);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 0.9f, 1.5f, 1.5f, 1.5f, 1.5f, 1f, 1.5f, 1.3f, 1.3f, 1.3f, 1.3f, 1.8f });

                // Header row
                string[] headers = { "Purchase Id", "Vendor", "Vendor Urdu", "Customer", "Customer Urdu", "Bill #", "Purchase Date",
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
                    table.AddCell(NameDisplayHelper.EnglishNameCell(p.VendorName));
                    table.AddCell(NameDisplayHelper.UrduNameCell(p.VendorUrduName));
                    table.AddCell(NameDisplayHelper.EnglishNameCell(p.CustomerName));
                    table.AddCell(NameDisplayHelper.UrduNameCell(p.CustomerUrduName));
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
                    Colspan = 7,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(summaryCell);
                
                table.AddCell(new PdfPCell(new Phrase(model.TotalAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDiscountAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalPaidAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase(model.TotalDueAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
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
            worksheet.Cell(1, 2).Value = "Product Name";
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Weight";
            worksheet.Cell(1, 5).Value = "Qty";
            worksheet.Cell(1, 6).Value = "Rate";
            worksheet.Cell(1, 7).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 7);
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
                    worksheet.Cell(row, 2).Value = $"Total — {NameDisplayHelper.EnglishNameCell(item.ProductName)}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                    worksheet.Cell(row, 3).Style.Font.Bold = true;
                    worksheet.Cell(row, 4).Value = item.Weight;
                    worksheet.Cell(row, 5).Value = item.Qty;
                    worksheet.Cell(row, 6).Value = item.Rate;
                    worksheet.Cell(row, 7).Value = item.Amount;
                    
                    var totalRange = worksheet.Range(row, 1, row, 7);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.SaleDate != DateTime.MinValue ? item.SaleDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                    worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                    worksheet.Cell(row, 4).Value = item.Weight > 0 ? item.Weight : (double?)null;
                    worksheet.Cell(row, 5).Value = item.Qty > 0 ? item.Qty : (long?)null;
                    worksheet.Cell(row, 6).Value = item.Rate > 0 ? item.Rate : (double?)null;
                    worksheet.Cell(row, 7).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = "";
            worksheet.Cell(row, 3).Value = "";
            worksheet.Cell(row, 4).Value = model.TotalWeight;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalQty;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = "";
            worksheet.Cell(row, 7).Value = model.TotalAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 7);
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

                // Table with 7 columns
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1.8f, 2.5f, 2.5f, 1.2f, 1.2f, 1.2f, 1.8f });

                // Header row
                string[] headers = { "Date", "Product Name", "Urdu Name", "Weight", "Qty", "Rate", "Amount" };

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
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase($"Total — {NameDisplayHelper.EnglishNameCell(item.ProductName)}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.ProductUrduName), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Weight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Qty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Rate.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        table.AddCell(item.SaleDate != DateTime.MinValue ? item.SaleDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                        table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
                        table.AddCell(item.Weight > 0 ? item.Weight.ToString("N2") : "");
                        table.AddCell(item.Qty > 0 ? item.Qty.ToString() : "");
                        table.AddCell(item.Rate > 0 ? item.Rate.ToString("N2") : "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell1 = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 3,
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
            worksheet.Cell(1, 2).Value = "Product Name";
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Weight";
            worksheet.Cell(1, 5).Value = "Qty";
            worksheet.Cell(1, 6).Value = "Rate";
            worksheet.Cell(1, 7).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 7);
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
                    worksheet.Cell(row, 2).Value = $"Total — {NameDisplayHelper.EnglishNameCell(item.ProductName)}";
                    worksheet.Cell(row, 2).Style.Font.Bold = true;
                    worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                    worksheet.Cell(row, 3).Style.Font.Bold = true;
                    worksheet.Cell(row, 4).Value = item.Weight;
                    worksheet.Cell(row, 5).Value = item.Qty;
                    worksheet.Cell(row, 6).Value = item.Rate;
                    worksheet.Cell(row, 7).Value = item.Amount;
                    
                    var totalRange = worksheet.Range(row, 1, row, 7);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.PurchaseDate != DateTime.MinValue ? item.PurchaseDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                    worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                    worksheet.Cell(row, 4).Value = item.Weight > 0 ? item.Weight : (double?)null;
                    worksheet.Cell(row, 5).Value = item.Qty > 0 ? item.Qty : (long?)null;
                    worksheet.Cell(row, 6).Value = item.Rate > 0 ? item.Rate : (double?)null;
                    worksheet.Cell(row, 7).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = "";
            worksheet.Cell(row, 3).Value = "";
            worksheet.Cell(row, 4).Value = model.TotalWeight;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalQty;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = "";
            worksheet.Cell(row, 7).Value = model.TotalAmount;
            worksheet.Cell(row, 7).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 7);
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

                // Table with 7 columns
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1.8f, 2.5f, 2.5f, 1.2f, 1.2f, 1.2f, 1.8f });

                // Header row
                string[] headers = { "Date", "Product Name", "Urdu Name", "Weight", "Qty", "Rate", "Amount" };

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
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase($"Total — {NameDisplayHelper.EnglishNameCell(item.ProductName)}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.ProductUrduName), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Weight.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Qty.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Rate.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        table.AddCell(item.PurchaseDate != DateTime.MinValue ? item.PurchaseDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                        table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
                        table.AddCell(item.Weight > 0 ? item.Weight.ToString("N2") : "");
                        table.AddCell(item.Qty > 0 ? item.Qty.ToString() : "");
                        table.AddCell(item.Rate > 0 ? item.Rate.ToString("N2") : "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                // Summary row
                var summaryCell1 = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 3,
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
            worksheet.Range(1, 1, 1, 6).Merge();
            
            worksheet.Cell(2, 1).Value = $"Date: {filters.ReportDate.Value.ToString("dd-MMM-yyyy")}";
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Range(2, 1, 2, 6).Merge();
            
            // Column headers
            worksheet.Cell(3, 1).Value = "Product Name";
            worksheet.Cell(3, 2).Value = "Urdu Name";
            worksheet.Cell(3, 3).Value = "Purchase";
            worksheet.Cell(3, 4).Value = "Sales";
            worksheet.Cell(3, 5).Value = "Closing";
            worksheet.Cell(3, 6).Value = "Bags";
            
            // Style header
            var headerRange = worksheet.Range(3, 1, 3, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add data
            int row = 4;
            foreach (var item in model.StockPositionList)
            {
                worksheet.Cell(row, 1).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                worksheet.Cell(row, 2).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                worksheet.Cell(row, 3).Value = item.PurchaseQuantity > 0 ? item.PurchaseQuantity : (double?)null;
                worksheet.Cell(row, 4).Value = item.SalesQuantity > 0 ? item.SalesQuantity : (double?)null;
                worksheet.Cell(row, 5).Value = item.ClosingStock;
                worksheet.Cell(row, 6).Value = item.Bags > 0 ? item.Bags : (long?)null;
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 1).Value = "TOTAL:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.TotalPurchase;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalSales;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalClosing;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalBags;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 6);
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

                // Table with 6 columns
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2.5f, 2.5f, 2f, 2f, 2f, 1.5f });

                // Header row
                string[] headers = { "Product Name", "Urdu Name", "Purchase", "Sales", "Closing", "Bags" };

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
                    table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                    table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
                    table.AddCell(item.PurchaseQuantity > 0 ? item.PurchaseQuantity.ToString("N2") : "");
                    table.AddCell(item.SalesQuantity > 0 ? item.SalesQuantity.ToString("N2") : "");
                    table.AddCell(item.ClosingStock.ToString("N2"));
                    table.AddCell(item.Bags > 0 ? item.Bags.ToString() : "");
                }

                // Summary row
                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 2,
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
            worksheet.Cell(1, 4).Value = "Urdu Name";
            worksheet.Cell(1, 5).Value = "Expense Detail";
            worksheet.Cell(1, 6).Value = "Amount";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 6);
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
                    worksheet.Cell(row, 5).Value = "";
                    worksheet.Cell(row, 6).Value = item.Amount;
                    worksheet.Cell(row, 6).Style.Font.Bold = true;
                    
                    var totalRange = worksheet.Range(row, 1, row, 6);
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                else
                {
                    // Regular row
                    worksheet.Cell(row, 1).Value = item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "";
                    worksheet.Cell(row, 2).Value = item.ExpenseTypeName;
                    worksheet.Cell(row, 3).Value = NameDisplayHelper.EnglishNameCell(item.ProductName);
                    worksheet.Cell(row, 4).Value = NameDisplayHelper.UrduNameCell(item.ProductUrduName);
                    worksheet.Cell(row, 5).Value = item.ExpenseDetail;
                    worksheet.Cell(row, 6).Value = item.Amount;
                }
                row++;
            }

            // Add summary row
            row++;
            worksheet.Cell(row, 2).Value = "TOTAL:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.TotalAmount;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            
            var summaryRange = worksheet.Range(row, 1, row, 6);
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

                // Table with 6 columns
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1.8f, 2f, 2.2f, 2.2f, 2.8f, 1.8f });

                // Header row
                string[] headers = { "Date", "Expense Type", "Product Name", "Urdu Name", "Expense Detail", "Amount" };

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
                        var totalCell1 = new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE };
                        table.AddCell(totalCell1);
                        var totalCell2 = new PdfPCell(new Phrase($"Total - {item.ExpenseTypeName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE };
                        table.AddCell(totalCell2);
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                        table.AddCell(new PdfPCell(new Phrase(item.Amount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9))) { BackgroundColor = BaseColor.BLUE });
                    }
                    else
                    {
                        table.AddCell(item.ExpenseDate != DateTime.MinValue ? item.ExpenseDate.ToString("dd-MMM-yyyy") : "");
                        table.AddCell(item.ExpenseTypeName ?? "");
                        table.AddCell(NameDisplayHelper.EnglishNameCell(item.ProductName));
                        table.AddCell(NameDisplayHelper.UrduNameCell(item.ProductUrduName));
                        table.AddCell(item.ExpenseDetail ?? "");
                        table.AddCell(item.Amount.ToString("N2"));
                    }
                }

                var summaryCell = new PdfPCell(new Phrase("TOTAL", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    Colspan = 5,
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
            worksheet.Cell(1, 2).Value = "Customer Name";
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Debit";
            worksheet.Cell(1, 5).Value = "Credit";
            worksheet.Cell(1, 6).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 6);
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
                    worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = fillColor;
                }
                worksheet.Cell(row, 1).Value = item.Date.ToString("dd-MMM-yy");
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.CustomerName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.CustomerUrduName);
                worksheet.Cell(row, 4).Value = item.Debit;
                worksheet.Cell(row, 5).Value = item.Credit;
                worksheet.Cell(row, 6).Value = item.Balance;
                row++;
            }

            row++;
            worksheet.Cell(row, 2).Value = "Total Debit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalDebit;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Total Credit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalCredit;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Closing Balance:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.ClosingBalance;
            worksheet.Cell(row, 6).Style.Font.Bold = true;

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
            {
                document.Add(new Paragraph(NameDisplayHelper.EnglishNameCell(model.CustomerName, ""), FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph(NameDisplayHelper.UrduNameCell(model.CustomerUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
            }
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.5f, 2.5f, 2.5f, 2f, 2f, 2f });
            string[] headers = { "Date", "Customer Name", "Urdu Name", "Debit", "Credit", "Balance" };
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
                var nameCell = new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.CustomerName), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var urduCell = new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.CustomerUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var debitCell = new PdfPCell(new Phrase(item.Debit > 0 ? item.Debit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var creditCell = new PdfPCell(new Phrase(item.Credit > 0 ? item.Credit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var balanceCell = new PdfPCell(new Phrase(item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                if (rowBg != null) { dateCell.BackgroundColor = rowBg; nameCell.BackgroundColor = rowBg; urduCell.BackgroundColor = rowBg; debitCell.BackgroundColor = rowBg; creditCell.BackgroundColor = rowBg; balanceCell.BackgroundColor = rowBg; }
                table.AddCell(dateCell);
                table.AddCell(nameCell);
                table.AddCell(urduCell);
                table.AddCell(debitCell);
                table.AddCell(creditCell);
                table.AddCell(balanceCell);
            }

            var totalLabelCell = new PdfPCell(new Phrase("Total Debit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 3,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(totalLabelCell);
            table.AddCell(new PdfPCell(new Phrase(model.TotalDebit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var creditLabelCell = new PdfPCell(new Phrase("Total Credit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 3,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(creditLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCredit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var balanceLabelCell = new PdfPCell(new Phrase("Closing Balance", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
            {
                Colspan = 3,
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
            worksheet.Cell(1, 2).Value = "Vendor Name";
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Debit";
            worksheet.Cell(1, 5).Value = "Credit";
            worksheet.Cell(1, 6).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 6);
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
                    worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = fillColor;
                }
                worksheet.Cell(row, 1).Value = item.Date.ToString("dd-MMM-yy");
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.VendorName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.VendorUrduName);
                worksheet.Cell(row, 4).Value = item.Debit;
                worksheet.Cell(row, 5).Value = item.Credit;
                worksheet.Cell(row, 6).Value = item.Balance;
                row++;
            }

            row++;
            worksheet.Cell(row, 2).Value = "Total Debit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalDebit;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Total Credit:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalCredit;
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Closing Balance:";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = model.ClosingBalance;
            worksheet.Cell(row, 6).Style.Font.Bold = true;

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
            {
                document.Add(new Paragraph(NameDisplayHelper.EnglishNameCell(model.VendorName, ""), FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph(NameDisplayHelper.UrduNameCell(model.VendorUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 12)) { Alignment = Element.ALIGN_CENTER });
            }
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.5f, 2.5f, 2.5f, 2f, 2f, 2f });
            string[] headers = { "Date", "Vendor Name", "Urdu Name", "Debit", "Credit", "Balance" };
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
                var nameCell = new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.VendorName), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var urduCell = new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.VendorUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var debitCell = new PdfPCell(new Phrase(item.Debit > 0 ? item.Debit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var creditCell = new PdfPCell(new Phrase(item.Credit > 0 ? item.Credit.ToString("N2") : "-", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                var balanceCell = new PdfPCell(new Phrase(item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                if (rowBg != null) { dateCell.BackgroundColor = rowBg; nameCell.BackgroundColor = rowBg; urduCell.BackgroundColor = rowBg; debitCell.BackgroundColor = rowBg; creditCell.BackgroundColor = rowBg; balanceCell.BackgroundColor = rowBg; }
                table.AddCell(dateCell);
                table.AddCell(nameCell);
                table.AddCell(urduCell);
                table.AddCell(debitCell);
                table.AddCell(creditCell);
                table.AddCell(balanceCell);
            }

            var totalLabelCell = new PdfPCell(new Phrase("Total Debit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 3, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(totalLabelCell);
            table.AddCell(new PdfPCell(new Phrase(model.TotalDebit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var creditLabelCell = new PdfPCell(new Phrase("Total Credit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 3, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(creditLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCredit.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            var balanceLabelCell = new PdfPCell(new Phrase("Closing Balance", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Colspan = 3, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY };
            table.AddCell(balanceLabelCell);
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(model.ClosingBalance >= 0 ? model.ClosingBalance.ToString("N2") : "(" + (-model.ClosingBalance).ToString("N2") + ")", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { BackgroundColor = BaseColor.LIGHT_GRAY });

            document.Add(table);
            document.Close();

            string filename = $"VendorLedgerReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> PayableReceivableReport([Bind(Prefix = "Filters")] PayableReceivableReportFilters? filters)
        {
            PayableReceivableReportViewModel model;
            try
            {
                filters ??= new PayableReceivableReportFilters();

                if (!Request.Query.ContainsKey("Filters.AsOfDate") && !filters.AsOfDate.HasValue)
                    filters.AsOfDate = DateTimeHelper.Now.Date;

                // IDs: read from query only (GET + Kendo hidden fields) so filtering is reliable
                var qCust = Request.Query["Filters.CustomerId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(qCust) && long.TryParse(qCust.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var cid) && cid > 0)
                    filters.CustomerId = cid;
                else
                    filters.CustomerId = null;

                var qVend = Request.Query["Filters.VendorId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(qVend) && long.TryParse(qVend.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var vid) && vid > 0)
                    filters.VendorId = vid;
                else
                    filters.VendorId = null;

                model = await _reportService.GetPayableReceivableReport(filters);
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model = new PayableReceivableReportViewModel
                {
                    Filters = filters ?? new PayableReceivableReportFilters(),
                    Rows = new List<PayableReceivableReportItem>()
                };
            }

            return View(model);
        }

        public async Task<IActionResult> ExportPayableReceivableExcel(long? customerId = null, long? vendorId = null, DateTime? asOfDate = null)
        {
            var filters = new PayableReceivableReportFilters
            {
                CustomerId = customerId,
                VendorId = vendorId,
                AsOfDate = asOfDate?.Date ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetPayableReceivableReport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Payable Receivable Report");

            worksheet.Cell(1, 1).Value = "As of date";
            worksheet.Cell(1, 2).Value = "Customer";
            worksheet.Cell(1, 3).Value = "Vendor";
            worksheet.Cell(1, 4).Value = "Payable";
            worksheet.Cell(1, 5).Value = "Receivable";
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var item in model.Rows ?? new List<PayableReceivableReportItem>())
            {
                worksheet.Cell(row, 1).Value = item.AsOfDate;
                worksheet.Cell(row, 1).Style.DateFormat.Format = "dd-MM-yy";
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.CustomerName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.EnglishNameCell(item.VendorName);
                worksheet.Cell(row, 4).Value = item.Payable;
                worksheet.Cell(row, 5).Value = item.Receivable;
                row++;
            }

            worksheet.Cell(row, 1).Value = "Total";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalPayable;
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = model.TotalReceivable;
            worksheet.Cell(row, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"PayableReceivableReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        private static string FormatPrSnapshotCell(decimal amount)
        {
            if (amount == 0) return "0.00";
            return amount > 0 ? amount.ToString("N2") : "(" + (-amount).ToString("N2") + ")";
        }

        public async Task<IActionResult> ExportPayableReceivablePdf(long? customerId = null, long? vendorId = null, DateTime? asOfDate = null)
        {
            var filters = new PayableReceivableReportFilters
            {
                CustomerId = customerId,
                VendorId = vendorId,
                AsOfDate = asOfDate?.Date ?? DateTimeHelper.Now.Date
            };
            var model = await _reportService.GetPayableReceivableReport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 24f, 24f, 24f, 24f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("Payable / Receivable Report", titleFont) { Alignment = Element.ALIGN_CENTER });
            var period = $"As of: {filters.AsOfDate:dd-MMM-yyyy}. Negative customer balance → Payable column (positive). Negative vendor balance → Receivable column (positive).";
            document.Add(new Paragraph(period, FontFactory.GetFont(FontFactory.HELVETICA, 9)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.2f, 2.1f, 2.1f, 1.7f, 1.7f });
            string[] headers = { "As of date", "Customer", "Vendor", "Payable", "Receivable" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell);
            }

            foreach (var item in model.Rows ?? new List<PayableReceivableReportItem>())
            {
                table.AddCell(new PdfPCell(new Phrase(item.AsOfDate.ToString("dd-MM-yy"), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.CustomerName), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.VendorName), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(FormatPrSnapshotCell(item.Payable), FontFactory.GetFont(FontFactory.HELVETICA, 8))) { HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(FormatPrSnapshotCell(item.Receivable), FontFactory.GetFont(FontFactory.HELVETICA, 8))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var totalLabel = new PdfPCell(new Phrase("Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 3,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            };
            table.AddCell(totalLabel);
            table.AddCell(new PdfPCell(new Phrase(model.TotalPayable.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            });
            table.AddCell(new PdfPCell(new Phrase(model.TotalReceivable.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LIGHT_GRAY
            });

            document.Add(table);
            document.Close();

            string filename = $"PayableReceivableReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", filename);
        }

        public async Task<IActionResult> CashInHandReport(CashInHandReportViewModel model, int pageNumber = 1, int? pageSize = null)
        {
            try
            {
                if (model == null)
                    model = new CashInHandReportViewModel();
                if (model.Filters == null)
                    model.Filters = new CashInHandReportFilters();

                var hasFrom = Request.Query.ContainsKey("Filters.FromDate");
                var hasTo = Request.Query.ContainsKey("Filters.ToDate");
                if (!hasFrom && !model.Filters.FromDate.HasValue)
                    model.Filters.FromDate = new DateTime(DateTimeHelper.Now.Year, DateTimeHelper.Now.Month, 1);
                if (!hasTo && !model.Filters.ToDate.HasValue)
                    model.Filters.ToDate = DateTimeHelper.Now.Date;

                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }

                var filters = model.Filters;
                model = await _reportService.GetCashInHandReport(pageNumber, currentPageSize, filters);
                model.Filters = filters;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model ??= new CashInHandReportViewModel();
                model.Filters ??= new CashInHandReportFilters();
                model.Items ??= new List<CashInHandReportItem>();
            }

            return View(model);
        }

        public async Task<IActionResult> ExportCashInHandExcel(DateTime? fromDate = null, DateTime? toDate = null, long? customerId = null, long? vendorId = null, string? sourceKind = null)
        {
            var now = DateTimeHelper.Now.Date;
            var filters = new CashInHandReportFilters
            {
                FromDate = fromDate?.Date ?? new DateTime(now.Year, now.Month, 1),
                ToDate = toDate?.Date ?? now,
                CustomerId = customerId is > 0 ? customerId : null,
                VendorId = vendorId is > 0 ? vendorId : null,
                SourceKind = string.IsNullOrWhiteSpace(sourceKind) ? null : sourceKind.Trim()
            };
            var model = await _reportService.GetCashInHandReportForExport(filters);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Cash In Hand");
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Ref / Bill #";
            worksheet.Cell(1, 3).Value = "Party / detail";
            worksheet.Cell(1, 4).Value = "Source";
            worksheet.Cell(1, 5).Value = "Amount (+ in, − out)";
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = item.TransactionDate;
                worksheet.Cell(row, 1).Style.DateFormat.Format = "dd-MMM-yyyy";
                worksheet.Cell(row, 2).Value = item.BillNumber;
                worksheet.Cell(row, 3).Value = NameDisplayHelper.EnglishNameCell(item.PartyName);
                worksheet.Cell(row, 4).Value = item.SourceKind;
                worksheet.Cell(row, 5).Value = item.CashAmount;
                row++;
            }

            row++;
            worksheet.Cell(row, 1).Value = "Summary";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = "Total cash in";
            worksheet.Cell(row, 3).Value = model.TotalCashIn;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Total cash out";
            worksheet.Cell(row, 3).Value = model.TotalCashOut;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 2).Value = "Net cash";
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Value = model.NetCash;
            worksheet.Cell(row, 3).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string filename = $"CashInHandReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        public async Task<IActionResult> ExportCashInHandPdf(DateTime? fromDate = null, DateTime? toDate = null, long? customerId = null, long? vendorId = null, string? sourceKind = null)
        {
            var now = DateTimeHelper.Now.Date;
            var filters = new CashInHandReportFilters
            {
                FromDate = fromDate?.Date ?? new DateTime(now.Year, now.Month, 1),
                ToDate = toDate?.Date ?? now,
                CustomerId = customerId is > 0 ? customerId : null,
                VendorId = vendorId is > 0 ? vendorId : null,
                SourceKind = string.IsNullOrWhiteSpace(sourceKind) ? null : sourceKind.Trim()
            };
            var model = await _reportService.GetCashInHandReportForExport(filters);

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 30f, 30f, 30f, 30f);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            document.Add(new Paragraph("Cash In Hand Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
            var period = $"Period: {filters.FromDate:dd-MMM-yyyy} to {filters.ToDate:dd-MMM-yyyy} (sales, expenses, purchase cash, salaries)";
            document.Add(new Paragraph(period, FontFactory.GetFont(FontFactory.HELVETICA, 10)) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.4f, 1f, 2.2f, 1.4f, 1.4f });
            foreach (var h in new[] { "Date", "Ref #", "Party / detail", "Source", "Amount" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                });
            }

            foreach (var item in model.Items)
            {
                table.AddCell(new PdfPCell(new Phrase(item.TransactionDate.ToString("dd-MMM-yyyy"), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(item.BillNumber.ToString(), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.PartyName), FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(item.SourceKind, FontFactory.GetFont(FontFactory.HELVETICA, 8))));
                table.AddCell(new PdfPCell(new Phrase(item.CashAmount.ToString("N2"), FontFactory.GetFont(FontFactory.HELVETICA, 8))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var sumFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            var cellG = BaseColor.LIGHT_GRAY;
            table.AddCell(new PdfPCell(new Phrase("Total cash in", sumFont)) { Colspan = 4, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCashIn.ToString("N2"), sumFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });
            table.AddCell(new PdfPCell(new Phrase("Total cash out", sumFont)) { Colspan = 4, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });
            table.AddCell(new PdfPCell(new Phrase(model.TotalCashOut.ToString("N2"), sumFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });
            table.AddCell(new PdfPCell(new Phrase("Net cash", sumFont)) { Colspan = 4, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });
            table.AddCell(new PdfPCell(new Phrase(model.NetCash.ToString("N2"), sumFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = cellG });

            document.Add(table);
            document.Close();
            string filename = $"CashInHandReport_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf";
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
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rows = model.BalanceList ?? new List<CustomerBalanceReportItem>();
            int row = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(row, 1).Value = item.AsOfDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.CustomerName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.CustomerUrduName);
                worksheet.Cell(row, 4).Value = item.Balance;
                row++;
            }
            row++;
            worksheet.Cell(row, 3).Value = "Total (sum of balances):";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalBalance;
            worksheet.Cell(row, 4).Style.Font.Bold = true;

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

            var table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2f, 2.5f, 2.5f, 2f });
            foreach (var h in new[] { "As of Date", "Customer", "Urdu Name", "Balance" })
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
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.CustomerName), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.CustomerUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                var bal = item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")";
                table.AddCell(new PdfPCell(new Phrase(bal, FontFactory.GetFont(FontFactory.HELVETICA, 9))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var totalLabel = new PdfPCell(new Phrase("Total (sum of balances)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 3,
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
            worksheet.Cell(1, 3).Value = "Urdu Name";
            worksheet.Cell(1, 4).Value = "Balance";
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rows = model.BalanceList ?? new List<VendorBalanceReportItem>();
            int row = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(row, 1).Value = item.AsOfDate.ToString("dd-MMM-yyyy");
                worksheet.Cell(row, 2).Value = NameDisplayHelper.EnglishNameCell(item.VendorName);
                worksheet.Cell(row, 3).Value = NameDisplayHelper.UrduNameCell(item.VendorUrduName);
                worksheet.Cell(row, 4).Value = item.Balance;
                row++;
            }
            row++;
            worksheet.Cell(row, 3).Value = "Total (sum of balances):";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Value = model.TotalBalance;
            worksheet.Cell(row, 4).Style.Font.Bold = true;

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

            var table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 2f, 2.5f, 2.5f, 2f });
            foreach (var h in new[] { "As of Date", "Vendor", "Urdu Name", "Balance" })
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
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.EnglishNameCell(item.VendorName), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                table.AddCell(new PdfPCell(new Phrase(NameDisplayHelper.UrduNameCell(item.VendorUrduName), FontFactory.GetFont(FontFactory.HELVETICA, 9))));
                var bal = item.Balance == 0 ? "-" : item.Balance > 0 ? item.Balance.ToString("N2") : "(" + (-item.Balance).ToString("N2") + ")";
                table.AddCell(new PdfPCell(new Phrase(bal, FontFactory.GetFont(FontFactory.HELVETICA, 9))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            var totalLabel = new PdfPCell(new Phrase("Total (sum of balances)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = 3,
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

        private static string FormatBankLedgerSaleReference(PersonalPaymentTransactionViewModel t)
        {
            if (t.SaleId == 0)
            {
                if (t.TransactionType == "Credit") return "Manual Deposit";
                if (t.TransactionType == "Debit") return "Manual Withdraw";
                return "-";
            }
            if (t.BillNumber > 0) return "Bill #" + t.BillNumber;
            return string.IsNullOrEmpty(t.SaleDescription) ? "-" : t.SaleDescription;
        }

}
}
