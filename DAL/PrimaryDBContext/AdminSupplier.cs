using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.DAL.PrimaryDBContext;

public partial class AdminSupplier
{
    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    [Column("UrduName")]
    public string? VendorUrduName { get; set; }

    public string? SupplierDescription { get; set; }

    public string? SupplierPhoneNumber { get; set; }

    public string? SupplierNtn { get; set; }

    public string? SupplierEmail { get; set; }

    public string? SupplierAddress { get; set; }

    public bool IsDeleted { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public long ModifiedBy { get; set; }

    public DateTime ModifiedDate { get; set; }
}
