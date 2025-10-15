using System.Xml.Linq;

namespace IMS.Models
{
    public class ReceiptData
    {
        public string ReportTitle { get; set; } = string.Empty;
        public bool ShowHeader { get; set; } = true;
        public List<ReceiptFilter> Filters { get; set; } = new List<ReceiptFilter>();
        public List<ReceiptDataRow> DataRows { get; set; } = new List<ReceiptDataRow>();
        public List<ReceiptFooter> FooterItems { get; set; } = new List<ReceiptFooter>();
    }

    public class ReceiptFilter
    {
        public string ColumnName { get; set; } = string.Empty;
        public string ColumnValue { get; set; } = string.Empty;
    }

    public class ReceiptDataRow
    {
        public Dictionary<string, object> Columns { get; set; } = new Dictionary<string, object>();
    }


    public class SalesReceiptData
    {
        public CustomerDetails CustomerDetails { get; set; } = new CustomerDetails();
        public SaleDetails SaleDetails { get; set; } = new SaleDetails();
        public List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }

    public class CustomerDetails
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
    }

    public class SaleDetails
    {
        public string BillNo { get; set; } = string.Empty;
        public string SaleDate { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public string TotalLineLevelDiscount { get; set; } = string.Empty;
        public string TotalDiscount { get; set; } = string.Empty;
        public string TotalBillAmount { get; set; } = string.Empty;
        public string PreviousAmount { get; set; } = string.Empty;
        public string TotalDue { get; set; } = string.Empty;
        public string TotalReceiveAmount { get; set; } = string.Empty;
    }

    public class SaleItem
    {
        public string Product { get; set; } = string.Empty;
        public decimal QTY { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public class ReceiptTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReceiptType Type { get; set; }
    }

    public enum ReceiptType
    {
        GeneralReport,
        SalesInvoice,
        SalesInvoiceDetailed,
        SalesInvoiceCompact
    }

    public class ReceiptGenerationRequest
    {
        public ReceiptType TemplateType { get; set; }
        public long? SaleId { get; set; }
        public long? PaymentId { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }
}
