using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminMeasuringUnitType
{
    public long MeasuringUnitTypeId { get; set; }

    public string MeasuringUnitTypeName { get; set; } = null!;

    public string? MeasuringUnitTypeDescription { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    public virtual ICollection<AdminMeasuringUnit> AdminMeasuringUnits { get; set; } = new List<AdminMeasuringUnit>();
}
