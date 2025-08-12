using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class SaleDetail
{
    public long SaleDetailId { get; set; }

    public long SaleIdFk { get; set; }

    public long PrductIdFk { get; set; }

    public decimal UnitPrice { get; set; }

    public long Quantity { get; set; }

    public decimal SalePrice { get; set; }

    public decimal LineDiscountAmount { get; set; }

    public decimal PayableAmount { get; set; }

    public long? ProductRangeIdFk { get; set; }
}
