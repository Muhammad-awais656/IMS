using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminCategory
{
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Category Name is required")]
    [StringLength(200, ErrorMessage = "Category Name cannot exceed 200 characters")]
    [Display(Name = "Category Name")]
    public string? CategoryName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? CategoryDescription { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    [Display(Name = "Other Adjustments")]
    public byte? OtherAdjustments { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
