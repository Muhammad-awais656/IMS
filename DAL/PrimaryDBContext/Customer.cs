using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerContactNumber { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public bool IsEnabled { get; set; }

    public TimeOnly? StartWorkingTime { get; set; }

    public TimeOnly? EndWorkingTime { get; set; }

    public long? InvoiceCreditPeriod { get; set; }

    public string? CustomerEmailCc { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
