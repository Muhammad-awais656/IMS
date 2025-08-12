using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminFeature
{
    public long FeatureId { get; set; }

    public long ParentFeatureIdFk { get; set; }

    public string FeatureName { get; set; } = null!;

    public string FeatureUielementId { get; set; } = null!;

    public string FeatureLabel { get; set; } = null!;

    public long FeatureOrder { get; set; }

    public string? FeatureDescription { get; set; }

    public bool AddLineDividerAfterFeature { get; set; }

    public bool IsDeleted { get; set; }
}
