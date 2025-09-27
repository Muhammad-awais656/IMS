using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class VendorBillsViewModel
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
}
