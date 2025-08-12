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

    public virtual ICollection<AdminMeasuringUnit> AdminMeasuringUnits { get; set; } = new List<AdminMeasuringUnit>();
}
