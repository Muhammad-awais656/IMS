using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class RoleFeatureAccessRight
{
    public long RoleFeatureAccessRightId { get; set; }

    public long FeatureIdFk { get; set; }

    public long UserIdFk { get; set; }

    public bool CanView { get; set; }

    public bool CanModify { get; set; }
}
