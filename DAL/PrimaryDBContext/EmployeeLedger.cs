namespace IMS.DAL.PrimaryDBContext
{
    public class EmployeeLedger
    {
        public int LedgerId { get; set; }
        public long EmployeeId { get; set; }
        public DateTime VoucherDate { get; set; }
        public string VoucherType { get; set; }
        public string ReferenceNo { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Remarks { get; set; }

        public Employee Employee { get; set; }
        public int VoucherTypeId { get; internal set; }
        public long? CreatedBy { get; internal set; }
    }
}
