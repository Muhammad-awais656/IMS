using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class ProductRange
{
    public long ProductRangeId { get; set; }

    public long ProductIdFk { get; set; }

    [Required(ErrorMessage = "Measuring Unit is required")]
    public long MeasuringUnitIdFk { get; set; }

    [Required(ErrorMessage = "Range From is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Range From must be a positive number")]
    public decimal RangeFrom { get; set; }

    [Required(ErrorMessage = "Range To is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Range To must be a positive number")]
    public decimal RangeTo { get; set; }

    [Required(ErrorMessage = "Unit Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit Price must be a positive number")]
    public decimal UnitPrice { get; set; }
}
