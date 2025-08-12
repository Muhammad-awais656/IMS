using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Product
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public long SizeIdFk { get; set; }

    public long LabelIdFk { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    public string? ProductCode { get; set; }

    public byte IsEnabled { get; set; }

    public long CategoryIdFk { get; set; }

    public long? MeasuringUnitTypeIdFk { get; set; }

    public long? SupplierIdFk { get; set; }
}
