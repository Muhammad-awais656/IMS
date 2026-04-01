namespace IMS.DAL.PrimaryDBContext;

/// <summary>
/// Many-to-many: User can be assigned to multiple branches.
/// Table: UserBranches (UserId, BranchId).
/// </summary>
public class UserBranch
{
    public long UserId { get; set; }
    public int BranchId { get; set; }

    public virtual User? User { get; set; }
    public virtual Branch? Branch { get; set; }
}
