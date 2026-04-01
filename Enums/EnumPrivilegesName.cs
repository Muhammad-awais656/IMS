namespace IMS.Enums;

/// <summary>
/// Common privilege/feature names for role-based access.
/// Use these constants when checking permissions (e.g. with AdminFeature.FeatureName or menu visibility).
/// </summary>
public static class EnumPrivilegesName
{
    // Optional: wire app configuration when available
    // public static IConfiguration Configuration = Configurations.GetConfiguration();

    /// <summary>
    /// Domain Management
    /// </summary>

    // ========== Admin / Menu & Features ==========

    public const string ADMIN_PORTAL = "Admin Portal";
    //1-Add Dashboard Menus and Submenus
    public const string DASHBOARD = "Dashboard";

    //2-Add User Management Menus and Sub menus
    public const string USER_MANAGEMENT = "User Management";
             //Sub menus
    public const string VIEW_USERS = "View Users";
    public const string USER_PERMISSIONS = "User Permissions";
    public const string VIEW_ROLES = "View Roles";
    public const string VIEW_BRANCHES = "View Branches";

   

    //3-Add Customer Management Menus and Sub menus
    public const string CUSTOMER_MANAGEMENT = "Customer Management";
    // Sub menus
    public const string VIEW_CUSTOMERS = "View Customers";
    public const string VIEW_SALES = "View Sales";
    public const string ADD_SALE = "Add sale";
    public const string VIEW_CUSTOMER_PAYMENTS = "View Customer Payments";


    //4-Add Vendor Management Menus and Sub menus
    public const string VENDOR_MANAGEMENT = "Vendor Management";
    // Sub menus
    public const string VIEW_VENDORS = "View Vendors";
    public const string VIEW_PURCHASE_ORDERS = "View Purchase Orders";
    public const string GENERATE_PO_BILLS = "Generate PO Bills";
    public const string VIEW_VENDOR_PAYMENTS = "View Vendor Payments";

    //5-Add Employee Management Menus and Sub menus
    public const string EMPLOYEE_MANAGEMENT = "Employee Management";
    // Sub menus
    public const string VIEW_EMPLOYEES = "View Employees";
    public const string VIEW_EMPLOYEES_LEDGER = "View Employees Ledger";

    //6-Add Product Management Menus and Sub menus
    public const string PRODUCT_MANAGEMENT = "Product Management";
    // Sub menus
    public const string VIEW_PRODUCTS = "View Products";


    //7-Add Stock Management Menus and Sub menus
    public const string STOCK_MANAGEMENT = "Stock Management";
    // Sub menus
    public const string VIEW_STOCKS = "View Stocks";



    //8-Add Expense Management Menus and Sub menus
    public const string EXPENSE_MANAGEMENT = "Expense Management";
    // Sub menus
    public const string VIEW_EXPENSES = "View Expenses";
 
    //9-Add Bank Management Menus and Sub menus
    public const string ONLINE_BANK_MANAGEMENT = "Online/Bank Management";
    // Sub menus
    public const string VIEW_BANK_PAYMENTS = "View Bank Payments";


    //10-Add Reports Management Menus and Sub menus
    public const string REPORTS_MANAGEMENT = "Reports Management";
    // Sub menus
    public const string DAILY_STOCK_POSITION_REPORT = "Daily Stock Position Report";
    public const string PRODUCT_WISE_SALES_REPORT = "Product Wise Sales Report";
    public const string PRODUCT_WISE_PURCHASE_REPORT = "Product Wise Purchase Report";
    public const string CUSTOMER_LEDGER_REPORT = "Customer Ledger Report";
    public const string VENDOR_LEDGER_REPORT = "Vendor Ledger Report";
    public const string GENERAL_EXPENSE_REPORT = "General Expense Report";
    public const string DIRECT_PRODUCT_EXPENSE_REPORT = "Direct Product Expenses Report";


    //11-Add Settings Menus and Sub menus
    public const string SETTINGS = "Settings";
    // Sub menus
    public const string VIEW_EXPENSE_TYPES = "View Expense Types";
    public const string VIEW_ADMIN_LABEL = "View Company/Brand Labels";
    public const string VIEW_CATEGORIES = "View Categories";
    public const string VIEW_MEASURING_UNIT_TYPE = "View Measuring Unit Types";
    public const string VIEW_MEASURING_UNITS = "View Measuring Units";
    public const string VIEW_UNIT_CONVERSIONS = "View Unit Conversions";






    
}
