using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class BillGenerationViewModel
    {
        // Vendor Information
        public List<AdminSupplier> VendorList { get; set; } = new List<AdminSupplier>();
        public long? SelectedVendorId { get; set; }
        public string? SelectedVendorName { get; set; }

        // Bill Information
        public long BillNumber { get; set; }
        public DateTime BillDate { get; set; } = DateTime.Now;
        public decimal PayNow { get; set; }
        public string? Description { get; set; }

        // Product Information
        public List<Product> ProductList { get; set; } = new List<Product>();
        public long? SelectedProductId { get; set; }
        public string? SelectedProductName { get; set; }
        public string? SelectedProductCode { get; set; }

        // Product Size/Range Information
        public List<ProductRange> ProductRanges { get; set; } = new List<ProductRange>();
        public long? SelectedProductRangeId { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? UnitPurchasePrice { get; set; }
        public decimal? RangeFrom { get; set; }
        public decimal? RangeTo { get; set; }

        // Current Item Being Added
        public decimal Quantity { get; set; } = 1;
        public decimal DiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }

        // Bill Items
        public List<BillItemViewModel> BillItems { get; set; } = new List<BillItemViewModel>();

        // Bill Summary
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal PreviousDue { get; set; }
    }

    public class BillItemViewModel
    {
        public long BillItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal BillPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public decimal PrintQuantity { get; set; }
        public long ProductRangeId { get; set; }
        public long MeasuringUnitId { get; set; }
        public string ProductSize { get; set; } = string.Empty;
        public string? MeasuringUnitAbbreviation { get; set; }
        public bool IsSmallestUnit { get; set; }
    }
}








