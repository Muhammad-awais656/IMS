using Microsoft.AspNetCore.Identity;

namespace IMS.Authorization
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; }
        public long? LegacyUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        public virtual ICollection<IdentityUserBranch> Branches { get; set; } = new List<IdentityUserBranch>();
    }
}
