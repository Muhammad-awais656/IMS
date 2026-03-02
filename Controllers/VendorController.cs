using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NuGet.Protocol.Core.Types;
using System.Threading.Tasks;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using PageSize = iTextSharp.text.PageSize;

namespace IMS.Controllers
{
    public class VendorController : Controller
    {
        private readonly ILogger<VendorController> _logger;
        private readonly IVendor _vndorservice;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 };
        public VendorController(IVendor repository, ILogger<VendorController> logger)
        {
            _vndorservice = repository;
            _logger = logger;
        }
        // GET: CustomerController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, string sidx = "Id", string sord = "asc", bool _search = false, string? Name = null, string? phoneNumber = null,string? NTN = null)
        {
            var viewModel = new VendorViewModel();
            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                if (Name == null)
                {
                    Name = HttpContext.Request.Query["searchUsername"].ToString();
                }
                if (phoneNumber==null)
                {
                    phoneNumber = HttpContext.Request.Query["searchContactNo"].ToString();
                }
                if (NTN==null)
                {
                    NTN = HttpContext.Request.Query["searchEmail"].ToString();
                }
                viewModel = await _vndorservice.GetAllVendors(pageNumber, currentPageSize, Name, phoneNumber, NTN);


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
            
        }
        

        // GET: CustomerController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CustomerController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CustomerController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminSupplier adminSupplier)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminSupplier.CreatedBy = userId;
                    adminSupplier.CreatedDate = DateTimeHelper.Now;
                    adminSupplier.ModifiedBy = userId;
                    adminSupplier.ModifiedDate = DateTimeHelper.Now;

                    var result = await _vndorservice.CreateVendorAsync(adminSupplier);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(adminSupplier);
            }
            return View(adminSupplier);
        }

        // GET: CustomerController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var unit = await _vndorservice.GetVendorByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return View(unit);
        }

        // POST: CustomerController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, AdminSupplier adminSupplier)
        {
            if (id != adminSupplier.SupplierId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    adminSupplier.ModifiedDate = DateTimeHelper.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    adminSupplier.ModifiedBy = userId;
                    var response = await _vndorservice.UpdateVendorAsync(adminSupplier);
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(adminSupplier);
                    }

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;

                }

            }
            return View(adminSupplier);
        }

        // GET: CustomerController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var vendor = new AdminSupplier();
            try
            {
                vendor = await _vndorservice.GetVendorByIdAsync(id);
                if (vendor == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(vendor);
        }

        // POST: CustomerController/Delete/5
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long id)
        {
            try
            {
                var res = await _vndorservice.DeleteVendorAsync(id);
                if (res != 0)
                {
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint to get vendors for Kendo combobox
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                var vendors = await _vndorservice.GetAllEnabledVendors();
                var result = vendors.Select(v => new
                {
                    value = v.SupplierId.ToString(),
                    text = v.SupplierName
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors for Kendo combobox");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? name = null, string? phoneNumber = null, string? ntn = null)
        {
            try
            {
                const int exportPageSize = 100000;
                var model = await _vndorservice.GetAllVendors(1, exportPageSize, name, phoneNumber, ntn);
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Vendors");
                worksheet.Cell(1, 1).Value = "Vendor Id"; worksheet.Cell(1, 2).Value = "Vendor Name"; worksheet.Cell(1, 3).Value = "Phone"; worksheet.Cell(1, 4).Value = "Email"; worksheet.Cell(1, 5).Value = "Address";
                var headerRange = worksheet.Range(1, 1, 1, 5); headerRange.Style.Font.Bold = true; headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                int row = 2;
                foreach (var item in model.VendorList ?? new List<AdminSupplier>())
                {
                    worksheet.Cell(row, 1).Value = item.SupplierId; worksheet.Cell(row, 2).Value = item.SupplierName ?? ""; worksheet.Cell(row, 3).Value = item.SupplierPhoneNumber ?? ""; worksheet.Cell(row, 4).Value = item.SupplierEmail ?? ""; worksheet.Cell(row, 5).Value = item.SupplierAddress ?? "";
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vendors_{DateTimeHelper.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting vendors to Excel"); TempData["ErrorMessage"] = "An error occurred while exporting to Excel."; return RedirectToAction(nameof(Index)); }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string? name = null, string? phoneNumber = null, string? ntn = null)
        {
            try
            {
                const int exportPageSize = 100000;
                var model = await _vndorservice.GetAllVendors(1, exportPageSize, name, phoneNumber, ntn);
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 15f, 15f, 15f, 15f);
                PdfWriter.GetInstance(document, stream); document.Open();
                document.Add(new Paragraph("Supplier/Vendor Management Report", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));
                var table = new PdfPTable(5); table.WidthPercentage = 100; table.SetWidths(new float[] { 1f, 2.5f, 1.5f, 2f, 2.5f });
                foreach (var h in new[] { "Vendor Id", "Vendor Name", "Phone", "Email", "Address" })
                { var cell = new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8))) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = BaseColor.LIGHT_GRAY }; table.AddCell(cell); }
                foreach (var c in model.VendorList ?? new List<AdminSupplier>())
                { table.AddCell(c.SupplierId.ToString()); table.AddCell(c.SupplierName ?? ""); table.AddCell(c.SupplierPhoneNumber ?? ""); table.AddCell(c.SupplierEmail ?? ""); table.AddCell(c.SupplierAddress ?? ""); }
                document.Add(table); document.Close();
                return File(stream.ToArray(), "application/pdf", $"Vendors_{DateTimeHelper.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error exporting vendors to PDF"); TempData["ErrorMessage"] = "An error occurred while exporting to PDF."; return RedirectToAction(nameof(Index)); }
        }
    }
}
