using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class TransactionStatus
{
    public long StockTransactionStatusId { get; set; }

    public string? TransactionStatusName { get; set; }
}
