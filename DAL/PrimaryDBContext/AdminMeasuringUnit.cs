using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminMeasuringUnit
{
    public long MeasuringUnitId { get; set; }

    public string MeasuringUnitName { get; set; } = null!;

    public string? MeasuringUnitDescription { get; set; }

    public long MeasuringUnitTypeIdFk { get; set; }

    public bool IsSmallestUnit { get; set; }

    public string MeasuringUnitAbbreviation { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual AdminMeasuringUnitType MeasuringUnitTypeIdFkNavigation { get; set; } = null!;
}
