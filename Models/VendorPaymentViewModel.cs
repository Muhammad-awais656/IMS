using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class VendorPaymentViewModel
    {
        public List<VendorBillViewModel> BillsList { get; set; } = new List<VendorBillViewModel>();
        public List<AdminSupplier> VendorList { get; set; } = new List<AdminSupplier>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Summary totals
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPayableAmount { get; set; }
    }

    public class VendorPaymentFormViewModel
    {
        public List<AdminSupplier> VendorList { get; set; } = new List<AdminSupplier>();
        public long SupplierId { get; set; }
        public long BillId { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        public string? PaymentMethod { get; set; }
        public long? OnlineAccountId { get; set; }
    }

    public class VendorBillViewModel
    {
        public long BillId { get; set; }
        public long BillNumber { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal TotalPayableAmount { get; set; }
        public string? Description { get; set; }
        public string? PaymentMethod { get; set; }
        public long? OnlineAccountId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class VendorPaymentFilters
    {
        public long? BillId { get; set; }
        public long? VendorId { get; set; }
        public long? BillNumber { get; set; }
        public DateTime? BillDateFrom { get; set; }
        public DateTime? BillDateTo { get; set; }
        public string? Description { get; set; }
    }
    public class VendorBillsFilters
    {
        public long? BillId { get; set; }
        public long? VendorId { get; set; }
        public long? BillNumber { get; set; }
        public DateTime? BillDateFrom { get; set; }
        public DateTime? BillDateTo { get; set; }
        public string? Description { get; set; }
    }

    public class SupplierBillNumber
    {
        public long BillNumber { get; set; }
        public long PurchaseOrderId { get; set; }
        public decimal TotalDueAmount { get; set; }
    }
}
