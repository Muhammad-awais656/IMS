using IMS.DAL.PrimaryDBContext;
using IMS.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Domain { get; set; } // Shop or Factory

        /// <summary>
        /// Kept for connection string selection (e.g. Shop/Factory). Set server-side on login.
        /// </summary>
        [ValidateNever]
        public string Domain { get; set; } = "Shop";

        [ValidateNever]
       public DAL.PrimaryDBContext.User ShopUsers { get; set; }
    }
}
