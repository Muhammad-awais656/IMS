using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class VendorBillGenerationViewModel
    {
        public long? BillId { get; set; }
        public List<VendorBillDetailViewModel> BillDetails { get; set; } = new List<VendorBillDetailViewModel>();
        public List<AdminSupplier> VendorList { get; set; } = new List<AdminSupplier>();
        public List<Product> ProductList { get; set; } = new List<Product>();

        // Bill Details
        public long? VendorId { get; set; }
        public long? CreatedBy { get; set; }
        public long BillNumber { get; set; }
        public DateTime BillDate { get; set; } = DateTime.Today;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal PreviousDue { get; set; }
        public decimal PayNow { get; set; }
        public string? Description { get; set; }
        public string? ActionType { get; set; }
        public string? PaymentMethod { get; set; } // Cash or Online
        public long? OnlineAccountId { get; set; } // Personal Payment ID when Online is selected
        public bool IsEditMode { get; set; } = false; // Indicates if the form is in edit mode
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }
    }

    public class VendorBillDetailViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductSize { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public long ProductRangeId { get; set; }
        public long? MeasuringUnitId { get; set; }
        public string? MeasuringUnitAbbreviation { get; set; }
    }

    public class VendorBillWithVendorViewModel
    {
        public long BillId { get; set; }
        public long BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public long VendorIdFk { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? BillDescription { get; set; }
        public string? PaymentMethod { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }
    }

    public class VendorBillFilters
    {
        public bool? IsDeleted { get; set; }
        public long? VendorId { get; set; }
        public long? BillNumber { get; set; }
        public DateTime? BillFrom { get; set; }
        public DateTime? BillDateTo { get; set; }
        public string? Description { get; set; }
    }

    public class VendorBillPrintViewModel
    {
        public long BillId { get; set; }
        public long BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public long VendorIdFk { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? BillDescription { get; set; }
        public List<VendorBillDetailPrintViewModel> BillDetails { get; set; } = new List<VendorBillDetailPrintViewModel>();
    }

    public class VendorBillDetailPrintViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public long Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public long ProductRangeId { get; set; }
    }
}

