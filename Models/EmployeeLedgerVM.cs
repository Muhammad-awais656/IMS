namespace IMS.Models
{
    public class EmployeeLedgerVM
    {
        public DateTime Date { get; set; }
        public string VoucherType { get; set; }
        public string ReferenceNo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}
