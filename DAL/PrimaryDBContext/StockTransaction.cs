using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class StockTransaction
{
    public long StockTransactionId { get; set; }

    public long StockMasterIdFk { get; set; }

    public decimal StockQuantity { get; set; }

    public DateTime TransactionDate { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public long TransactionStatusId { get; set; }

    public string? Comment { get; set; }

    public long? SaleId { get; set; }
}
