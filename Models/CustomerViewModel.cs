using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class CustomerViewModel
    {
        public List<Customer> Customers { get; set; }
        public CustomerFilters Filters { get; set; } = new CustomerFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class CustomerFilters
    {
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
