using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Selected branch for login. User must belong to this branch.
        /// </summary>
        [Required(ErrorMessage = "Please select a branch.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }

        [ValidateNever]
        public List<DAL.PrimaryDBContext.Branch>? Branches { get; set; }
        public bool RememberMe { get; set; }
    }
}
