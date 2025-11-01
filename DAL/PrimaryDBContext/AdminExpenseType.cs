using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminExpenseType
{
    public long ExpenseTypeId { get; set; }

    [Required(ErrorMessage = "Expense Type Name is required")]
    [StringLength(200, ErrorMessage = "Expense Type Name cannot exceed 200 characters")]
    [Display(Name = "Expense Type Name")]
    public string ExpenseTypeName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? ExpenseTypeDescription { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }
}
