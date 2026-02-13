using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class GenerateBillViewModel
    {
        public long? BillId { get; set; }
        public List<BillItemViewModel> BillItems { get; set; } = new List<BillItemViewModel>();
        public List<AdminSupplier> VendorList { get; set; } = new List<AdminSupplier>();
        public List<Product> ProductList { get; set; } = new List<Product>();
        public List<ProductRange> ProductSizes { get; set; } = new List<ProductRange>();

        // Bill Details
        public long? VendorId { get; set; }
        public long? CustomerId { get; set; }
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
    }

}
