using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class PurchaseOrderItem
{
    public long PurchaseOrderItemId { get; set; }

    public long PurchaseOrderIdFk { get; set; }

    public long PrductIdFk { get; set; }

    public decimal UnitPrice { get; set; }

    public long Quantity { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal LineDiscountAmount { get; set; }

    public decimal PayableAmount { get; set; }

    public long? ProductRangeIdFk { get; set; }
}
