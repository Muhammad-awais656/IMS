using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class MeasuringUnitViewModel
    {
        public List<AdminMeasuringUnit> AdminMeasuringUnits { get; set; }
        public List<AdminMeasuringUnitType> AdminMeasuringUnitTypes { get; set; }

        public List<MeasuringUnitListItemViewModel> Items { get; set; }
        
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    public class MeasuringUnitListItemViewModel
    {
        public long MeasuringUnitId { get; set; }
        public string? MeasuringUnitName { get; set; }
        public string? MeasuringUnitDescription { get; set; }
        public string? MeasuringUnitTypeName { get; set; } // from AdminMeasuringUnitTypes
        public bool IsEnabled { get; set; }
    }

}
