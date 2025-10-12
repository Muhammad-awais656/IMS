using System;

namespace IMS.DAL.PrimaryDBContext;

public partial class PersonalPaymentSaleDetail
{
    public long PersonalPaymentSaleDetailId { get; set; }
    
    public long PersonalPaymentId { get; set; }
    
    public long SaleId { get; set; }
    
    public string TransactionType { get; set; } = null!; // 'Credit' or 'Debit'
    
    public decimal Amount { get; set; }
    
    public decimal Balance { get; set; }
    
    public string? TransactionDescription { get; set; }
    
    public DateTime TransactionDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedDate { get; set; }
    
    public long CreatedBy { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    public long ModifiedBy { get; set; }
    
    // Navigation properties
    public virtual PersonalPayment PersonalPayment { get; set; } = null!;
    public virtual Sale Sale { get; set; } = null!;
}

