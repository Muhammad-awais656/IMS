using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class PurchaseOrder
{
    public long PurchaseOrderId { get; set; }

    public long BillNumber { get; set; }

    public DateTime PurchaseOrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalReceivedAmount { get; set; }

    public decimal TotalDueAmount { get; set; }

    public long SupplierIdFk { get; set; }

    public string? PurchaseOrderDescription { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }
}
