using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminLabel
{
    public long LabelId { get; set; }

    public string LabelName { get; set; } = null!;

    public string? LabelDescription { get; set; }

    public bool IsEnabled { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
