using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace IMS.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ReceiptService> _logger;
        private readonly string _templatesPath;

        public ReceiptService(IDbContextFactory dbContextFactory, ILogger<ReceiptService> logger, IWebHostEnvironment environment)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _templatesPath = Path.Combine(environment.ContentRootPath, "Templates");
        }

        public async Task<string> GenerateReceiptAsync(ReceiptGenerationRequest request)
        {
            try
            {
                var xmlData = await GetReceiptXmlDataAsync(request);
                var templatePath = GetTemplatePath(request.TemplateType);
                
                return await TransformXmlWithXslAsync(xmlData, templatePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt for request: {Request}", request);
                throw;
            }
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(ReceiptGenerationRequest request)
        {
            try
            {
                var htmlContent = await GenerateReceiptAsync(request);
                // For now, return HTML as bytes. In production, you might want to use a library like PuppeteerSharp or wkhtmltopdf
                return Encoding.UTF8.GetBytes(htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF receipt for request: {Request}", request);
                throw;
            }
        }

        public async Task<List<ReceiptTemplate>> GetAvailableTemplatesAsync()
        {
            return new List<ReceiptTemplate>
            {
                new ReceiptTemplate
                {
                    Name = "General Report",
                    FileName = "GeneralReport.xsl",
                    Description = "General purpose report template",
                    Type = ReceiptType.GeneralReport
                },
                new ReceiptTemplate
                {
                    Name = "Sales Invoice",
                    FileName = "SalesInvoice.xsl",
                    Description = "Simple sales invoice template",
                    Type = ReceiptType.SalesInvoice
                },
                new ReceiptTemplate
                {
                    Name = "Sales Invoice Detailed",
                    FileName = "SalesInvoiceDetailed.xsl",
                    Description = "Detailed sales invoice with company branding",
                    Type = ReceiptType.SalesInvoiceDetailed
                },
                new ReceiptTemplate
                {
                    Name = "Sales Invoice Compact",
                    FileName = "SalesInvoiceCompact.xsl",
                    Description = "Compact sales invoice for thermal printers",
                    Type = ReceiptType.SalesInvoiceCompact
                }
            };
        }

        public async Task<ReceiptData> GetSalesReceiptDataAsync(long saleId)
        {
            using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
            await connection.OpenAsync();

            // Get sale details
            var saleQuery = @"
                SELECT s.SaleId, s.BillNumber, s.SaleDate, s.TotalAmount, s.DiscountAmount, 
                       s.TotalReceivedAmount, s.TotalDueAmount, c.CustomerName
                FROM Sales s
                LEFT JOIN Customer c ON s.CustomerId_FK = c.CustomerId
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

            await saleReader.CloseAsync();

            var receiptData = new ReceiptData
            {
                ReportTitle = "Sales Report",
                ShowHeader = true,
                Filters = new List<ReceiptFilter>
                {
                    new ReceiptFilter { ColumnName = "Bill Number", ColumnValue = billNumber.ToString() },
                    new ReceiptFilter { ColumnName = "Sale Date", ColumnValue = saleDate.ToString("yyyy-MM-dd") },
                    new ReceiptFilter { ColumnName = "Customer", ColumnValue = customerName }
                }
            };

            // Get sale details
            var detailsQuery = @"
                SELECT p.ProductName, sd.Quantity, sd.UnitPrice, sd.SalePrice, 
                       sd.LineDiscountAmount, sd.PayableAmount
                FROM SaleDetails sd
                LEFT JOIN Product p ON sd.ProductId_FK = p.ProductId
                WHERE sd.SaleId_FK = @SaleId";

            using var detailsCommand = new SqlCommand(detailsQuery, connection);
            detailsCommand.Parameters.AddWithValue("@SaleId", saleId);

            using var detailsReader = await detailsCommand.ExecuteReaderAsync();
            while (await detailsReader.ReadAsync())
            {
                var row = new ReceiptDataRow();
                row.Columns["Product"] = detailsReader.IsDBNull("ProductName") ? "N/A" : detailsReader.GetString("ProductName");
                row.Columns["Quantity"] = detailsReader.GetDecimal("Quantity");
                row.Columns["Unit_Price"] = detailsReader.GetDecimal("UnitPrice");
                row.Columns["Sale_Price"] = detailsReader.GetDecimal("SalePrice");
                row.Columns["Discount_Amount"] = detailsReader.GetDecimal("LineDiscountAmount");
                row.Columns["Payable_Amount"] = detailsReader.GetDecimal("PayableAmount");
                receiptData.DataRows.Add(row);
            }

            // Add footer
            receiptData.FooterItems = new List<ReceiptFooter>
            {
                new ReceiptFooter { ColumnName = "Total Amount", ColumnValue = totalAmount.ToString("N2") },
                new ReceiptFooter { ColumnName = "Discount", ColumnValue = discountAmount.ToString("N2") },
                new ReceiptFooter { ColumnName = "Received Amount", ColumnValue = totalReceivedAmount.ToString("N2") },
                new ReceiptFooter { ColumnName = "Due Amount", ColumnValue = totalDueAmount.ToString("N2") }
            };

            return receiptData;
        }

        public async Task<SalesReceiptData> GetSalesReceiptDetailedDataAsync(long saleId)
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

            var salesReceiptData = new SalesReceiptData
            {
                CustomerDetails = new CustomerDetails
                {
                    CustomerName = customerName,
                    CreatedByUserName = createdByUserName
                },
                SaleDetails = new SaleDetails
                {
                    BillNo = billNumber.ToString(),
                    SaleDate = saleDate.ToString("yyyy-MM-dd"),
                    TotalAmount = totalAmount.ToString("N2"),
                    TotalLineLevelDiscount = discountAmount.ToString("N2"),
                    TotalDiscount = discountAmount.ToString("N2"),
                    TotalBillAmount = totalAmount.ToString("N2"),
                    PreviousAmount = "0.00", // You might need to calculate this from previous sales
                    TotalDue = totalDueAmount.ToString("N2"),
                    TotalReceiveAmount = totalReceivedAmount.ToString("N2")
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
            while (await detailsReader.ReadAsync())
            {
                salesReceiptData.SaleItems.Add(new SaleItem
                {
                    Product = detailsReader.IsDBNull("ProductName") ? "N/A" : detailsReader.GetString("ProductName"),
                    QTY = detailsReader.GetDecimal("Quantity"),
                    SalePrice = detailsReader.GetDecimal("SalePrice"),
                    DiscountAmount = detailsReader.GetDecimal("LineDiscountAmount"),
                    PayableAmount = detailsReader.GetDecimal("PayableAmount")
                });
            }

            return salesReceiptData;
        }

        private async Task<XDocument> GetReceiptXmlDataAsync(ReceiptGenerationRequest request)
        {
            switch (request.TemplateType)
            {
                case ReceiptType.GeneralReport:
                    var generalData = await GetSalesReceiptDataAsync(request.SaleId ?? 0);
                    return ConvertToPrintDataSetXml(generalData);
                
                case ReceiptType.SalesInvoice:
                case ReceiptType.SalesInvoiceDetailed:
                case ReceiptType.SalesInvoiceCompact:
                    var detailedData = await GetSalesReceiptDetailedDataAsync(request.SaleId ?? 0);
                    return ConvertToSalesReceiptXml(detailedData);
                
                default:
                    throw new ArgumentException($"Unsupported receipt type: {request.TemplateType}");
            }
        }

        private XDocument ConvertToPrintDataSetXml(ReceiptData data)
        {
            var doc = new XDocument(
                new XElement("PrintDataSet",
                    new XElement("ReportHeader",
                        new XElement("ColumnValue", data.ShowHeader.ToString().ToLower())
                    ),
                    new XElement("ReportHeaderDetail",
                        new XElement("ColumnValue", data.ReportTitle)
                    )
                )
            );

            var root = doc.Root!;

            // Add filters
            foreach (var filter in data.Filters)
            {
                root.Add(new XElement("Filter",
                    new XElement("ColumnName", filter.ColumnName),
                    new XElement("ColumnValue", filter.ColumnValue)
                ));
            }

            // Add data rows
            foreach (var row in data.DataRows)
            {
                var dataElement = new XElement("Data");
                foreach (var column in row.Columns)
                {
                    // Convert column names to valid XML element names
                    var elementName = ConvertToValidXmlName(column.Key);
                    dataElement.Add(new XElement(elementName, column.Value));
                }
                root.Add(dataElement);
            }

            // Add footer
            foreach (var footer in data.FooterItems)
            {
                root.Add(new XElement("Footer",
                    new XElement("ColumnName", footer.ColumnName),
                    new XElement("ColumnValue", footer.ColumnValue)
                ));
            }

            return doc;
        }

        private XDocument ConvertToSalesReceiptXml(SalesReceiptData data)
        {
            var doc = new XDocument(
                new XElement("PrintDataSet")
            );

            var root = doc.Root!;

            // Add customer details
            root.Add(new XElement("CustomerDeatils",
                new XElement("ColumnName", "CustomerName"),
                new XElement("ColumnValue", data.CustomerDetails.CustomerName)
            ));

            root.Add(new XElement("CustomerDeatils",
                new XElement("ColumnName", "CreatedByUserName"),
                new XElement("ColumnValue", data.CustomerDetails.CreatedByUserName)
            ));

            // Add sale details
            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "BillNo"),
                new XElement("ColumnValue", data.SaleDetails.BillNo)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "SaleDate"),
                new XElement("ColumnValue", data.SaleDetails.SaleDate)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalAmount"),
                new XElement("ColumnValue", data.SaleDetails.TotalAmount)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalLineLevelDiscount"),
                new XElement("ColumnValue", data.SaleDetails.TotalLineLevelDiscount)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalDiscount"),
                new XElement("ColumnValue", data.SaleDetails.TotalDiscount)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalBillAmount"),
                new XElement("ColumnValue", data.SaleDetails.TotalBillAmount)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "PreviousAmount"),
                new XElement("ColumnValue", data.SaleDetails.PreviousAmount)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalDue"),
                new XElement("ColumnValue", data.SaleDetails.TotalDue)
            ));

            root.Add(new XElement("SaleDeatils",
                new XElement("ColumnName", "TotalReceiveAmount"),
                new XElement("ColumnValue", data.SaleDetails.TotalReceiveAmount)
            ));

            // Add sale items
            foreach (var item in data.SaleItems)
            {
                root.Add(new XElement("SaleItems",
                    new XElement("Product", item.Product),
                    new XElement("QTY", item.QTY),
                    new XElement("sale_price", item.SalePrice),
                    new XElement("Discount_amount", item.DiscountAmount),
                    new XElement("Payable_amount", item.PayableAmount)
                ));
            }

            return doc;
        }

        private string ConvertToValidXmlName(string name)
        {
            // Replace invalid XML characters with underscores
            return name.Replace(" ", "_").Replace("-", "_").Replace("(", "").Replace(")", "");
        }

        private string GetTemplatePath(ReceiptType templateType)
        {
            var fileName = templateType switch
            {
                ReceiptType.GeneralReport => "GeneralReport.xsl",
                ReceiptType.SalesInvoice => "SalesInvoice.xsl",
                ReceiptType.SalesInvoiceDetailed => "SalesInvoiceDetailed.xsl",
                ReceiptType.SalesInvoiceCompact => "SalesInvoiceCompact.xsl",
                _ => throw new ArgumentException($"Unknown template type: {templateType}")
            };

            return Path.Combine(_templatesPath, fileName);
        }

        private async Task<string> TransformXmlWithXslAsync(XDocument xmlData, string xslPath)
        {
            try
            {
                var xslTransform = new XslCompiledTransform();
                xslTransform.Load(xslPath);

                using var xmlReader = xmlData.CreateReader();
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                });

                xslTransform.Transform(xmlReader, xmlWriter);
                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming XML with XSL template: {XslPath}", xslPath);
                throw;
            }
        }
    }
}
