using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminRole
{
    public long RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? RoleDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public long ModifiedBy { get; set; }

    public DateTime ModifiedDate { get; set; }
}
