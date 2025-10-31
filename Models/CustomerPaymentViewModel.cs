using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class CustomerPaymentViewModel
    {
        public List<PaymentWithCustomerViewModel> PaymentsList { get; set; } = new List<PaymentWithCustomerViewModel>();
        public List<Customer> CustomerList { get; set; } = new List<Customer>();
        public List<Sale> SalesList { get; set; } = new List<Sale>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Properties for form binding
        public long PaymentId { get; set; }
        public decimal PaymentAmount { get; set; }
        public long SaleId { get; set; }
        public long CustomerId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        public string? PaymentMethod { get; set; }
        public long? OnlineAccountId { get; set; }
    }

    public class PaymentWithCustomerViewModel
    {
        public long PaymentId { get; set; }
        public decimal PaymentAmount { get; set; }
        public long SaleId { get; set; }
        public long BillNumber { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
        public string? PaymentMethod { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class CustomerPaymentFilters
    {
        public long? CustomerId { get; set; }
        public long? SaleId { get; set; }
        public DateTime? PaymentDateFrom { get; set; }
        public DateTime? PaymentDateTo { get; set; }
    }
}
