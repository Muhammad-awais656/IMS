using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class SalesViewModel
    {
        public List<SaleWithCustomerViewModel> SalesList { get; set; } = new List<SaleWithCustomerViewModel>();
        public List<Customer> CustomerList { get; set; } = new List<Customer>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
  


    public class SaleDetailViewModel
    {
        public long ProductId { get; set; }
        public string ProductSize { get; set; }
        public string ProductName { get; set; }
        
        public string MeasuringUnitAbbreviation { get; set; }
        public decimal UnitPrice { get; set; }
        public long Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public long ProductRangeId { get; set; }

        public string? PaymentMethod { get; set; }

    }

    public class AddSaleViewModel
    {
        public long? SaleId { get; set; } // Add this for editing existing sales
        public List<SaleDetailViewModel> SaleDetails { get; set; } = new List<SaleDetailViewModel>();
        public decimal TotalAmount { get; set; }
        public decimal TotalReceivedAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public long? CustomerId { get; set; }
        public long? VendorId { get; set; }
        public string BillNo { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal PayNow { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal PreviousDue { get; set; }
        public string Description { get; set; }
        public string ActionType { get; set; }
        public string PaymentMethod { get; set; } // Cash or Online
        public long? OnlineAccountId { get; set; } // Personal Payment ID when Online is selected
        public bool IsEditMode { get; set; } = false; // Indicates if the form is in edit mode
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }
    }

    public class ProductSizeViewModel
    {
        public long ProductRangeId { get; set; }
        public long ProductId_FK { get; set; }
        public long MeasuringUnitId_FK { get; set; }
   
        public decimal RangeFrom { get; set; }
        public decimal RangeTo { get; set; }
        public decimal UnitPrice { get; set; }
        public string MeasuringUnitName { get; set; }
        public string MeasuringUnitAbbreviation { get; set; }
    }
    public class SaleWithCustomerViewModel
    {
        public long SaleId { get; set; }
        public long BillNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalReceivedAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public long CustomerIdFk { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? SaleDescription { get; set; }
        public string? PaymentMethod { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }
    }

    public class SalesFilters
    {
        public bool? IsDeleted { get; set; }
        public long? CustomerId { get; set; }
        public long? BillNumber { get; set; }
        public DateTime? SaleFrom { get; set; }
        public DateTime? SaleDateTo { get; set; }
        public string? Description { get; set; }
    }

    public class SalePrintViewModel
    {
        public long SaleId { get; set; }
        public long BillNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalReceivedAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public long CustomerIdFk { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? SaleDescription { get; set; }
        public List<SaleDetailPrintViewModel> SaleDetails { get; set; } = new List<SaleDetailPrintViewModel>();
    }

    public class SaleDetailPrintViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public long Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public long ProductRangeId { get; set; }
        public string? MeasuringUnitAbbreviation { get; set; }
    }
}


