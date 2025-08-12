using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminExpenseType
{
    public long ExpenseTypeId { get; set; }

    public string ExpenseTypeName { get; set; } = null!;

    public string? ExpenseTypeDescription { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    public bool IsEnabled { get; set; }
}
