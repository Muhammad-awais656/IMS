using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class Employee
{
    public long EmployeeId { get; set; }

    [Required(ErrorMessage = "First Name is required")]
    [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters")]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Phone Number is required")]
    [StringLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = null!;

    [StringLength(20, ErrorMessage = "CNIC cannot exceed 20 characters")]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    [Display(Name = "Address")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = null!;

    [Required(ErrorMessage = "Age is required")]
    [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
    [Display(Name = "Age")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Joining Date is required")]
    [Display(Name = "Joining Date")]
    public DateTime JoiningDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    [Display(Name = "Salary")]
    public long? Salary { get; set; }

    [StringLength(100, ErrorMessage = "Father/Husband Name cannot exceed 100 characters")]
    [Display(Name = "Father/Husband Name")]
    public string? HusbandFatherName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100, ErrorMessage = "Email Address cannot exceed 100 characters")]
    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    [StringLength(20, ErrorMessage = "Marital Status cannot exceed 20 characters")]
    [Display(Name = "Marital Status")]
    public string? MaritalStatus { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedByUserIdFk { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public long? ModifiedByUserIdFk { get; set; }
}
