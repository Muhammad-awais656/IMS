using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class ProductRange
{
    public long ProductRangeId { get; set; }

    public long ProductIdFk { get; set; }

    public long MeasuringUnitIdFk { get; set; }

    public decimal RangeFrom { get; set; }

    public decimal RangeTo { get; set; }

    public decimal UnitPrice { get; set; }
}
