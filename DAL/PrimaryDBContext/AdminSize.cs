using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminSize
{
    public long SizeId { get; set; }

    public string SizeName { get; set; } = null!;

    public string? SizeDescription { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }
}
