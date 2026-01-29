using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Payment
{
    public long PaymentId { get; set; }

    public decimal PaymentAmount { get; set; }

    public long SaleId { get; set; }

    public long CustomerId { get; set; }

    public DateTime PaymentDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? Description { get; set; }
    public string? SupplierName { get; set; }
    public string? paymentMethod { get; set; }
    public long? onlineAccountId { get; set; }

    public long? ModifiedBy { get; set; }
    public long? SupplierId { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
