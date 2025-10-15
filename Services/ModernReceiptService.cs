using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public interface IModernReceiptService
    {
        Task<string> GenerateReceiptHtmlAsync(long saleId, ModernReceiptType receiptType);
        Task<byte[]> GenerateReceiptPdfAsync(long saleId, ModernReceiptType receiptType);
        Task<ModernReceiptViewModel> GetReceiptDataAsync(long saleId);
    }

    public class ModernReceiptService : IModernReceiptService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ModernReceiptService> _logger;
        private readonly IViewRenderService _viewRenderService;

        public ModernReceiptService(IDbContextFactory dbContextFactory, ILogger<ModernReceiptService> logger, IViewRenderService viewRenderService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _viewRenderService = viewRenderService;
        }

        public async Task<string> GenerateReceiptHtmlAsync(long saleId, ModernReceiptType receiptType)
        {
            try
            {
                var receiptData = await GetReceiptDataAsync(saleId);
                var viewName = GetViewName(receiptType);
                
                return await _viewRenderService.RenderToStringAsync(viewName, receiptData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt HTML for sale {SaleId}", saleId);
                throw;
            }
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(long saleId, ModernReceiptType receiptType)
        {
            try
            {
                var htmlContent = await GenerateReceiptHtmlAsync(saleId, receiptType);
                
                // For now, return HTML as bytes. In production, you can use PuppeteerSharp or similar
                return System.Text.Encoding.UTF8.GetBytes(htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF receipt for sale {SaleId}", saleId);
                throw;
            }
        }

        public async Task<ModernReceiptViewModel> GetReceiptDataAsync(long saleId)
        {
            using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
            await connection.OpenAsync();

            // Get sale details with user information
            var saleQuery = @"
                SELECT s.SaleId, s.BillNumber, s.SaleDate, s.TotalAmount, s.DiscountAmount, 
                       s.TotalReceivedAmount, s.TotalDueAmount, c.CustomerName, u.UserName
                FROM Sales s
                LEFT JOIN Customer c ON s.CustomerId_FK = c.CustomerId
                LEFT JOIN [User] u ON s.CreatedBy = u.UserId
                WHERE s.SaleId = @SaleId AND s.IsDeleted = 0";

            using var saleCommand = new SqlCommand(saleQuery, connection);
            saleCommand.Parameters.AddWithValue("@SaleId", saleId);

            using var saleReader = await saleCommand.ExecuteReaderAsync();
            if (!await saleReader.ReadAsync())
                throw new ArgumentException($"Sale with ID {saleId} not found");

            var billNumber = saleReader.GetInt64("BillNumber");
            var saleDate = saleReader.GetDateTime("SaleDate");
            var totalAmount = saleReader.GetDecimal("TotalAmount");
            var discountAmount = saleReader.GetDecimal("DiscountAmount");
            var totalReceivedAmount = saleReader.GetDecimal("TotalReceivedAmount");
            var totalDueAmount = saleReader.GetDecimal("TotalDueAmount");
            var customerName = saleReader.IsDBNull("CustomerName") ? "N/A" : saleReader.GetString("CustomerName");
            var createdByUserName = saleReader.IsDBNull("UserName") ? "System" : saleReader.GetString("UserName");

            await saleReader.CloseAsync();

            var receiptData = new ModernReceiptViewModel
            {
                Header = new ReceiptHeader
                {
                    Title = "Sales Invoice",
                    Subtitle = "We believe in Authenticity",
                    ShowHeader = true
                },
                CompanyInfo = new ReceiptCompanyInfo
                {
                    CompanyName = "IM TRADERS",
                    Address = "179-Seetla Mander, Circular Road Lahore",
                    Phone = "Office: 042 37213511 Mob: +92 000 0000000",
                    Email = "imtraders999@gmail.com",
                    LogoPath = "/Images/imtraders_logo.jpeg"
                },
                CustomerInfo = new ReceiptCustomerInfo
                {
                    CustomerName = customerName,
                    CreatedByUserName = createdByUserName
                },
                SaleInfo = new ReceiptSaleInfo
                {
                    BillNo = billNumber.ToString(),
                    SaleDate = saleDate
                },
                Totals = new ReceiptTotals
                {
                    TotalAmount = totalAmount,
                    InvoiceDiscount = discountAmount,
                    PreviousAmount = 0.00m, // You might need to calculate this from previous sales
                    CashReceived = totalReceivedAmount,
                    TotalBalance = totalDueAmount,
                    TotalDue = totalDueAmount,
                    GrossTotal = totalAmount
                },
                Footer = new ReceiptFooter
                {
                    GeneratedBy = createdByUserName,
                    PoweredBy = "PeerSolutions",
                    Website = "https://peersolutions.000webhostapp.com/"
                }
            };

            // Get sale items
            var detailsQuery = @"
                SELECT p.ProductName, sd.Quantity, sd.UnitPrice, sd.SalePrice, 
                       sd.LineDiscountAmount, sd.PayableAmount
                FROM SaleDetails sd
                LEFT JOIN Product p ON sd.ProductId_FK = p.ProductId
                WHERE sd.SaleId_FK = @SaleId";

            using var detailsCommand = new SqlCommand(detailsQuery, connection);
            detailsCommand.Parameters.AddWithValue("@SaleId", saleId);

            using var detailsReader = await detailsCommand.ExecuteReaderAsync();
            int serialNumber = 1;
            while (await detailsReader.ReadAsync())
            {
                receiptData.Items.Add(new ReceiptItem
                {
                    SerialNumber = serialNumber++,
                    ProductName = detailsReader.IsDBNull("ProductName") ? "N/A" : detailsReader.GetString("ProductName"),
                    Quantity = detailsReader.GetDecimal("Quantity"),
                    UnitPrice = detailsReader.GetDecimal("UnitPrice"),
                    SalePrice = detailsReader.GetDecimal("SalePrice"),
                    DiscountAmount = detailsReader.GetDecimal("LineDiscountAmount"),
                    PayableAmount = detailsReader.GetDecimal("PayableAmount")
                });
            }

            return receiptData;
        }

        private string GetViewName(ModernReceiptType receiptType)
        {
            return receiptType switch
            {
                ModernReceiptType.Simple => "SimpleInvoice",
                ModernReceiptType.Detailed => "DetailedInvoice",
                ModernReceiptType.Compact => "CompactReceipt",
                ModernReceiptType.GeneralReport => "GeneralReport",
                _ => "SimpleInvoice"
            };
        }
    }
}
