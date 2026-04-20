using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class User
{
    public long UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public bool IsAdmin { get; set; }

    public int BranchId { get; set; }

    /// <summary>
    /// Role assigned to the user (from UserRoles table; not a column on Users).
    /// </summary>
    public long? RoleId { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<AdminMeasuringUnit> AdminMeasuringUnits { get; set; } = new List<AdminMeasuringUnit>();
}
