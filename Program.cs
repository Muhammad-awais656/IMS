using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Middlewares;
using IMS.Authorization;
using IMS.Common_Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Localization;
using System.Globalization;
using Microsoft.Extensions.Localization;
using IMS.Services;
using Serilog;
using StringEncrptandDecryptorApp;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

//                 Without localization
builder.Services.AddControllersWithViews(options =>
{
var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add<IMS.Filters.PrivilegeMenuFilter>();
});
// Read configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // reads Serilog section
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog(); // Replace default logging


// Self by awais

// For Primary DB Shop
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var raw = builder.Configuration.GetConnectionString("ShopConnectionString");
    if (string.IsNullOrWhiteSpace(raw))
        throw new InvalidOperationException("ShopConnectionString is missing.");
    var enc = new EncryptionHelper();
    var b = new SqlConnectionStringBuilder(raw)
    {
        UserID = enc.Decrypt(new SqlConnectionStringBuilder(raw).UserID),
        Password = enc.Decrypt(new SqlConnectionStringBuilder(raw).Password)
    };
    options.UseSqlServer(b.ConnectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
});

//// For Secondary DB Factory
//builder.Services.AddDbContext<FactoryDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("FactoryConnectionString"), sqlOptions =>
//    sqlOptions.EnableRetryOnFailure()));

//Custom Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExpenseType, ExpenseTypesService>();
builder.Services.AddScoped<IAdminLablesService, AdminLablesService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAdminMeasuringUnitTypesService, AdminMeasuringUnitTypesService>();
builder.Services.AddScoped<IAdminMeasuringUnitService, AdminMeasuringUnitService>();
builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICustomer, CustomerService>();
builder.Services.AddScoped<IVendor, VendorService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICustomerPaymentService, CustomerPaymentService>();
builder.Services.AddScoped<IVendorPaymentService, VendorPaymentService>();
builder.Services.AddScoped<IVendorBillsService, VendorBillsService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPersonalPaymentService, PersonalPaymentService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IModernReceiptService, ModernReceiptService>();
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ILoginBranchesService, LoginBranchesService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPrivilegeAuthorizationService, PrivilegeAuthorizationService>();
builder.Services.AddScoped<IUserPermissionsService, UserPermissionsService>();
builder.Services.AddScoped<IIdentityUserSyncService, IdentityUserSyncService>();
builder.Services.AddScoped<IdentityHelper>();
builder.Services.AddLogging(logging => logging.AddConsole());

// Session + auth cookie share the same idle window: after this many minutes with no HTTP requests, session data is cleared and the user must sign in again (sliding cookie).
var idleTimeoutMinutes = Math.Max(1, builder.Configuration.GetValue("Application:IdleTimeoutMinutes", 30));
var idleTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);

builder.Services.AddDistributedMemoryCache();

// Register services by Awais
builder.Services.AddSession(options =>
{
    options.IdleTimeout = idleTimeout;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure cookie authentication
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.AccessDeniedPath = "/Home/AccessDenied";
//    });

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.Name = "IMS_Auth";
    options.ExpireTimeSpan = idleTimeout;
    options.SlidingExpiration = true;
});
builder.Services.AddScoped<IDbContextFactory, AppDbContextFactory>();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<DbContextResolver>();


var app = builder.Build();

// Add missing AspNetUsers columns (ApplicationUser) + AspNetUserBranches if needed — fixes login SqlException until EF migration is applied.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var schemaLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySchemaBootstrapper");
    await IdentitySchemaBootstrapper.EnsureAsync(db, schemaLogger);
}
catch (Exception ex)
{
    var log = app.Services.GetService<ILoggerFactory>()?.CreateLogger("IdentitySchemaBootstrapper");
    log?.LogError(ex, "Identity schema bootstrap failed.");
}

// First-time Identity admin (only when AspNetUsers is empty). Set IdentitySeed:Enabled = false after use.
try
{
    var seedLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");
    await IdentityDataSeeder.SeedAsync(app.Services, app.Configuration, seedLogger);
}
catch (Exception ex)
{
    var log = app.Services.GetService<ILoggerFactory>()?.CreateLogger("IdentityDataSeeder");
    log?.LogError(ex, "Identity seed failed.");
}

//var supportedCultures = new[]
//{
//    new CultureInfo("en-US"), // English (United States)
//    new CultureInfo("es-ES"), // Spanish (Spain)
//    new CultureInfo("ur-PK"), // Urdu (Pakistan)
//};
//app.UseRequestLocalization(new RequestLocalizationOptions
//{
//    DefaultRequestCulture = new RequestCulture("en-US"),
//    SupportedCultures = supportedCultures,
//    SupportedUICultures = supportedCultures
//});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "-1";
    await next();
});
//app.UseMiddleware<DatabaseSelectionMiddleware>();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
// Middleware to store the current user's username in session (LoadAsync required before session access in middleware)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        !string.IsNullOrEmpty(context.User.Identity.Name))
    {
        await context.Session.LoadAsync();
        var userName = context.User.Identity.Name;
        var role = context.User.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value;
        var usrId = context.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

        context.Session.SetString("UserName", userName);
        if (role != null)
            context.Session.SetString("IsAdmin", role);
        if (usrId != null)
            context.Session.SetString("UserId", usrId);
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
