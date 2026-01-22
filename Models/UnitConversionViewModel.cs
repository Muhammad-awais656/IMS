using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class UnitConversionViewModel
    {
        public List<UnitConversionDisplayModel> UnitConversionsList { get; set; } = new List<UnitConversionDisplayModel>();
        public UnitConversionFilters Filters { get; set; } = new UnitConversionFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class UnitConversionDisplayModel
    {
        public long UnitConversionId { get; set; }
        public long FromUnitId { get; set; }
        public long ToUnitId { get; set; }
        public string FromUnitName { get; set; } = string.Empty;
        public string ToUnitName { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class UnitConversionFilters
    {
        public string? FromUnitName { get; set; }
        public string? ToUnitName { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Description { get; set; }
    }
}


