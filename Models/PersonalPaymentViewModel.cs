using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class PersonalPaymentViewModel
    {
        public List<PersonalPaymentModel> PersonalPaymentList { get; set; } = new List<PersonalPaymentModel>();
        public PersonalPaymentFilters PaymentFilters { get; set; } = new PersonalPaymentFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class PersonalPaymentModel
    {
        public long PersonalPaymentId { get; set; }
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal DebitAmount { get; set; }
        public string PaymentDescription { get; set; } = null!;
        public DateTime PaymentDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = null!;
        
        // Computed properties for display
        public decimal NetAmount => CreditAmount - DebitAmount;
        public string TransactionType => CreditAmount > 0 ? "Credit" : DebitAmount > 0 ? "Debit" : "N/A";
    }

    public class PersonalPaymentFilters
    {
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? PaymentDescription { get; set; }
        public decimal? CreditAmountFrom { get; set; }
        public decimal? CreditAmountTo { get; set; }
        public decimal? DebitAmountFrom { get; set; }
        public decimal? DebitAmountTo { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool? IsActive { get; set; }
        public string? TransactionType { get; set; } // Credit, Debit, or All
    }
}
