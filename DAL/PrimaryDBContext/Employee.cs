using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class Employee
{
    public long EmployeeId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? LastName { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string? Cnic { get; set; }

    public string Address { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public int Age { get; set; }

    public DateTime JoiningDate { get; set; }

    public long? Salary { get; set; }

    public string? HusbandFatherName { get; set; }

    public string? EmailAddress { get; set; }

    public string? MaritalStatus { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedByUserIdFk { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public long? ModifiedByUserIdFk { get; set; }
}
