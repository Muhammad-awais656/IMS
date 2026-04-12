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

    [Range(0, double.MaxValue, ErrorMessage = "Range From must be zero or positive")]
    public decimal RangeFrom { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Range To must be zero or positive")]
    public decimal RangeTo { get; set; }

    public string? ProductRangeName { get; set; }

    public string? UrduName { get; set; }

    [Required(ErrorMessage = "Unit Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit Price must be a positive number")]
    public decimal UnitPrice { get; set; }

    // Additional properties for display purposes
    public string? MeasuringUnitName { get; set; }
    public string? MeasuringUnitAbbreviation { get; set; }
    
    // Soft delete property
    public bool IsDeleted { get; set; } = false;
}
