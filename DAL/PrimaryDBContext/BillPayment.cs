using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class BillPayment
{
    public long PaymentId { get; set; }

    public decimal PaymentAmount { get; set; }

    public long BillId { get; set; }

    public long SupplierIdFk { get; set; }

    public DateTime PaymentDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? Description { get; set; }
    public string? PaymentMethod { get; set; }
    public long? onlineAccountId { get; set; }
}
