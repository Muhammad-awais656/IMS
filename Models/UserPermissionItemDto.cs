namespace IMS.Models;

public class UserPermissionItemDto
{
    public long FeatureId { get; set; }
    public bool CanView { get; set; }
    public bool CanModify { get; set; }
}

public class UserForPermissionsDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
