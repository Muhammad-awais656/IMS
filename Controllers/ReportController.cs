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
        private const int DefaultPageSize = 10; // Default page size
        private static readonly int[] AllowedPageSizes = { 10, 20, 30 };
        public ReportController(IReportService reportService , ILogger<ReportController> logger, ICustomer customerService)
        {
            _reportService = reportService;
            _logger = logger;
            _customerService = customerService;
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
            var model = await _reportService.GetAllSales(pageNumber, currentPageSize, salesReportsFilters);

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

      

public async Task<IActionResult> ExportPdf(int pageNumber = 1, int? pageSize = null, long ? custId = null, DateTime? fromDate = null, DateTime? toDate = null)
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

            var model = await _reportService.GetAllSales(pageNumber, currentPageSize, salesReportsFilters);

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

}
}
