namespace IMS.Models
{
    public class EmployeeLedgerResponse
    {
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }
}

