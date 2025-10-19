using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminMeasuringUnitType
{
    public long MeasuringUnitTypeId { get; set; }

    [Required(ErrorMessage = "Measuring Unit Type Name is required")]
    [StringLength(100, ErrorMessage = "Measuring Unit Type Name cannot exceed 100 characters")]
    [Display(Name = "Measuring Unit Type Name")]
    public string MeasuringUnitTypeName { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    [Display(Name = "Description")]
    public string? MeasuringUnitTypeDescription { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    public virtual ICollection<AdminMeasuringUnit> AdminMeasuringUnits { get; set; } = new List<AdminMeasuringUnit>();
}
