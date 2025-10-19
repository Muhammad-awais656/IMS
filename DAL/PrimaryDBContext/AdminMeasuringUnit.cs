using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminMeasuringUnit
{
    public long MeasuringUnitId { get; set; }

    [Required(ErrorMessage = "Measuring Unit Name is required")]
    [StringLength(100, ErrorMessage = "Measuring Unit Name cannot exceed 100 characters")]
    [Display(Name = "Measuring Unit Name")]
    public string MeasuringUnitName { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    [Display(Name = "Description")]
    public string? MeasuringUnitDescription { get; set; }

    [Required(ErrorMessage = "Measuring Unit Type is required")]
    [Display(Name = "Measuring Unit Type")]
    public long MeasuringUnitTypeIdFk { get; set; }

    [Display(Name = "Is Smallest Unit")]
    public bool IsSmallestUnit { get; set; }

    [Required(ErrorMessage = "Abbreviation is required")]
    [StringLength(10, ErrorMessage = "Abbreviation cannot exceed 10 characters")]
    [Display(Name = "Abbreviation")]
    public string MeasuringUnitAbbreviation { get; set; } = null!;

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    //public virtual User CreatedByNavigation { get; set; } = null!;

    //public virtual AdminMeasuringUnitType MeasuringUnitTypeIdFkNavigation { get; set; } = null!;
}
