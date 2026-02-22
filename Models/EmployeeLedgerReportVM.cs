namespace IMS.Models
{
    public class EmployeeLedgerReportVM
    {
        public DateTime VoucherDate { get; set; }
        public string VoucherTypeName { get; set; }
        public string ReferenceNo { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal RunningBalance { get; set; }
        public string Remarks { get; set; }
        public string EmployeeName { get; set; }
        public long EmployeeId_FK { get; set; }
    }

}
