using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class StockMaster
{
    public long StockMasterId { get; set; }

    public long ProductIdFk { get; set; }

    public decimal AvailableQuantity { get; set; }
    public long CategoryIdFk { get; set; }

    public decimal UsedQuantity { get; set; }

    public decimal TotalQuantity { get; set; }

    public DateTime? UploadedDate { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public long? ModifiedBy { get; set; }
}
