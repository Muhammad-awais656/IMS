using System;

namespace IMS.Models
{
    public class VendorPaymentPurchaseTransactionViewModel
    {
        public long PersonalPaymentPurchaseDetailId { get; set; }
        public long PersonalPaymentId { get; set; }
        public long PurchaseId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string? TransactionDescription { get; set; }
        public DateTime TransactionDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }
        
        // Additional fields for display
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string PurchaseOrderDescription { get; set; } = string.Empty;
        public long BillNumber { get; set; }
    }
    
    public class VendorPaymentPurchaseAccountSummary
    {
        public long PersonalPaymentId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public int TransactionCount { get; set; }
        public DateTime LastTransactionDate { get; set; }
    }
}

