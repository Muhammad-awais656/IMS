using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class PersonalPayment
{
    public long PersonalPaymentId { get; set; }

    public string BankName { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountHolderName { get; set; } = null!;

    public string? BankBranch { get; set; }

    public decimal CreditAmount { get; set; } = 0;

    public decimal DebitAmount { get; set; } = 0;

    public string PaymentDescription { get; set; } = null!;

    public DateTime PaymentDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }
}
