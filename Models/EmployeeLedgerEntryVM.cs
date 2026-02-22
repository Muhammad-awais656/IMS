using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    //public class EmployeeLedgerEntryVM
    //{
    //    public long EmployeeId { get; set; }
    //    public int VoucherTypeId { get; set; }
    //    public DateTime VoucherDate { get; set; }
    //    public string ReferenceNo { get; set; }
    //    public decimal Amount { get; set; }
    //    public string Remarks { get; set; }
    //}

    public class EmployeeLedgerEntryVM
    {
        [Required]
        public long EmployeeId { get; set; }

        [Required]
        public int VoucherTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime VoucherDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }
    }

}
