using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Sale
{
    public long SaleId { get; set; }

    public long BillNumber { get; set; }

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalReceivedAmount { get; set; }

    public decimal TotalDueAmount { get; set; }

    public long CustomerIdFk { get; set; }

    public string? SaleDescription { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }
}
