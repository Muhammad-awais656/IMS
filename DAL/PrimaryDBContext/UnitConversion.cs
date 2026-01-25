using System;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class UnitConversion
{
    public long UnitConversionId { get; set; }

    [Required(ErrorMessage = "From Unit is required")]
    [Display(Name = "From Unit")]
    public long FromUnitId { get; set; }

    [Required(ErrorMessage = "To Unit is required")]
    [Display(Name = "To Unit")]
    public long ToUnitId { get; set; }

    [Required(ErrorMessage = "Conversion Factor is required")]
    [Display(Name = "Conversion Factor")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Conversion factor must be greater than 0")]
    public decimal ConversionFactor { get; set; }

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    // Navigation properties (commented out as they may not be needed if using stored procedures)
    //public virtual AdminMeasuringUnit FromUnit { get; set; } = null!;
    //public virtual AdminMeasuringUnit ToUnit { get; set; } = null!;
}










