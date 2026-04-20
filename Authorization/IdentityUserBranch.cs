namespace IMS.Authorization
{
    /// <summary>
    /// Branch assignments for Identity users. One user can be assigned to many branches.
    /// </summary>
    public class IdentityUserBranch
    {
        public string UserId { get; set; } = string.Empty;
        public int BranchId { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }
}
