namespace IMS.Models;

/// <summary>Aggregated KPIs for the analytics dashboard (date range + optional branch).</summary>
public class AnalyticsSummaryDto
{
    public decimal SalesTotal { get; set; }
    public int SalesCount { get; set; }

    public decimal PurchaseTotal { get; set; }
    public int PurchaseCount { get; set; }

    public decimal ExpensesTotal { get; set; }
    public int ExpensesCount { get; set; }

    /// <summary>Customers created in the selected period.</summary>
    public int NewCustomers { get; set; }

    /// <summary>Products created in the selected period.</summary>
    public int NewProducts { get; set; }

    /// <summary>Employees created in the selected period.</summary>
    public int NewEmployees { get; set; }

    /// <summary>Snapshot: sum of available stock (all enabled products), branch-scoped when applicable.</summary>
    public decimal StockAvailableTotal { get; set; }

    /// <summary>Snapshot: count of enabled products (branch-scoped when applicable).</summary>
    public int ActiveProductsTotal { get; set; }

    /// <summary>Net cash-style hint: sales - purchases - expenses (informational).</summary>
    public decimal NetFlowHint { get; set; }
}

public class SalesTrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
}

public class AnalyticsDashboardPageModel
{
    public AnalyticsSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<SalesTrendPointDto> SalesTrend { get; set; } = Array.Empty<SalesTrendPointDto>();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Preset { get; set; } = "today";
    public bool IsAdmin { get; set; }
    public string? BranchName { get; set; }

    /// <summary>Products for stock donut chart dropdown (branch-scoped).</summary>
    public List<StockProductOption> StockProducts { get; set; } = new();
}

public class StockProductOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
