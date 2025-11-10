using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMS.DAL.PrimaryDBContext;

public partial class Product
{
    public long ProductId { get; set; }

    [Required(ErrorMessage = "Product Name is required")]
    [StringLength(200, ErrorMessage = "Product Name cannot exceed 200 characters")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Product Description cannot exceed 500 characters")]
    [Display(Name = "Product Description")]
    public string? ProductDescription { get; set; }

    public long SizeIdFk { get; set; }

    public long LabelIdFk { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit Price must be a positive number")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public DateTime CreatedDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public long ModifiedBy { get; set; }

    [StringLength(50, ErrorMessage = "Product Code cannot exceed 50 characters")]
    [Display(Name = "Product Code")]
    public string? ProductCode { get; set; }

    [Display(Name = "Is Enabled")]
    public byte IsEnabled { get; set; }

    public long CategoryIdFk { get; set; }

    public long? MeasuringUnitTypeIdFk { get; set; }

    public long? SupplierIdFk { get; set; }

    [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    [Display(Name = "Location")]
    public string? Location { get; set; }
}
