namespace IMS.Models;

/// <summary>
/// Represents a feature in the permissions hierarchy (Module Access tree).
/// </summary>
public class FeatureNodeDto
{
    public long FeatureId { get; set; }
    public string FeatureLabel { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public long ParentFeatureIdFk { get; set; }
    public long FeatureOrder { get; set; }
    public int Level { get; set; }
    public List<FeatureNodeDto> Children { get; set; } = new();
}
