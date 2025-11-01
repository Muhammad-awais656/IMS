using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class ModernReceiptViewModel
    {
        public ReceiptHeader Header { get; set; } = new ReceiptHeader();
        public ReceiptCompanyInfo CompanyInfo { get; set; } = new ReceiptCompanyInfo();
        public ReceiptCustomerInfo CustomerInfo { get; set; } = new ReceiptCustomerInfo();
        public ReceiptSaleInfo SaleInfo { get; set; } = new ReceiptSaleInfo();
        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
        public ReceiptTotals Totals { get; set; } = new ReceiptTotals();
        public ReceiptFooter Footer { get; set; } = new ReceiptFooter();
    }

    public class ReceiptHeader
    {
        public string Title { get; set; } = "Sales Invoice";
        public string Subtitle { get; set; } = "We believe in Authenticity";
        public bool ShowHeader { get; set; } = true;
    }

    public class ReceiptCompanyInfo
    {
        public string CompanyName { get; set; } = "IM TRADERS";
        public string Address { get; set; } = "179-Seetla Mander, Circular Road Lahore";
        public string Phone { get; set; } = "Office: 042 37213511 Mob: +92 000 0000000";
        public string Email { get; set; } = "imtraders999@gmail.com";
        public string LogoPath { get; set; } = "/Images/imtraders_logo.jpeg";
    }

    public class ReceiptCustomerInfo
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
    }

    public class ReceiptSaleInfo
    {
        public string BillNo { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string SaleDateFormatted => SaleDate.ToString("yyyy-MM-dd");
    }

    public class ReceiptItem
    {
        public int SerialNumber { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }

    public class ReceiptTotals
    {
        public decimal GrossTotal { get; set; }
        public decimal InvoiceDiscount { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal CashReceived { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class ReceiptFooter
    {
        public string GeneratedBy { get; set; } = string.Empty;
        public string PoweredBy { get; set; } = "IM TRADERS";
        public string Website { get; set; } = "https://peersolutions.000webhostapp.com/";
        public string ColumnName { get; internal set; }
        public string ColumnValue { get; internal set; }
    }

    public enum ModernReceiptType
    {
        Simple,
        Detailed,
        Compact,
        GeneralReport
    }
}
