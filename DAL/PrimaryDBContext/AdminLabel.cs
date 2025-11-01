using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminLabel
{
    public long LabelId { get; set; }

    [Required(ErrorMessage = "Label Name is required")]
    [StringLength(200, ErrorMessage = "Label Name cannot exceed 200 characters")]
    [Display(Name = "Label Name")]
    public string LabelName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? LabelDescription { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
