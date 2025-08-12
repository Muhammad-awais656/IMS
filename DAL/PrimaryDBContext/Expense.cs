using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Expense
{
    public long ExpenseId { get; set; }

    public long ExpenseTypeIdFk { get; set; }

    public string ExpenseDetail { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }
}
