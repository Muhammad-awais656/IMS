using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class EmployeeViewModel
    {
        public List<Employee> EmployeesList { get; set; }
        public string GenderType { get; set; }
        public string MaritalStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
    }   
    public class EmployeesFilters
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsDeleted { get; set; }
        public string? PhoneNo { get; set; }
        public string? CNIC { get; set; }
        public string? Guardian { get; set; }
        public string? email { get; set; }
        public string? gender { get; set; }
        public string? maritalStatus { get; set; }
        public string? HusbandFatherName { get; set; }
        public DateTime startDate { get; set; }
        public DateTime EndDateTime { get; set; }
    }
}
