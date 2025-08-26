using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class ProductViewModel
    {
        //public List<Product> ProductList { get; set; }
        public List<AdminCategory> CategoryNameList { get; set; } = new List<AdminCategory>();
        public List<AdminLabel> LabelNameList { get; set; } = new List<AdminLabel>();
        //public List<AdminSize> SizeNameList { get; set; }
        public List<AdminMeasuringUnitType> MeasuringUnitTypeNameList { get; set; } = new List<AdminMeasuringUnitType>();

        //public List<ProductViewModel> Items { get; set; } = new List<ProductViewModel>();

        //public long ProductId { get; set; }
        //public string? ProductName { get; set; }
        //public string? ProductDescription { get; set; }
        //public string? ProductCode { get; set; }
        //public decimal Price { get; set; }
        //public string? CategoryName { get; set; }
        //public string? LabelName { get; set; }
        //public string? MeasuringUnitTypeName { get; set; }
        //public byte IsEnabled { get; set; }

        public ProductFilters? productFilters { get; set; } = new ProductFilters();
        public List<ProductViewModel> Items { get; set; } = new List<ProductViewModel>();

        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
        public decimal Price { get; set; }
        public byte IsEnabled { get; set; }
        public string? CategoryName { get; set; }
        public string? LabelName { get; set; }
        public string? MeasuringUnitTypeName { get; set; }




        //public string? LabelName { get; set; }
        //public string? CategoryName { get; set; }
        //public string? SizeName { get; set; }
        //public string? MeasuringUnitTypeName { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    public class ProductFilters
    {
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public bool IsEnabled { get; set; }
        public long? SizeId { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public long? LabelId { get; set; }
        public string? LabelName { get; set; }
        public long? MeasuringUnitTypeId { get; set; }
        public string? MeasuringUnitTypeName { get; set; }
      
    }
}
