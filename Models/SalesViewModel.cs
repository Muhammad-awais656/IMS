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
        public string? SaleDescription { get; set; }
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
}


