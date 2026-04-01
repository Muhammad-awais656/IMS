using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class RoleViewModel
    {
        public List<AdminRole> Roles { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
