using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Middlewares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Localization;
using System.Globalization;
using Microsoft.Extensions.Localization;
using IMS.Services;

var builder = WebApplication.CreateBuilder(args);

//                 Without localization
builder.Services.AddControllersWithViews(options =>
{
var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});


// Self by awais

// For Primary DB Shop
//builder.Services.AddDbContext<IMS.DAL.PrimaryDBContext.AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("ShopConnectionString"), sqlOptions =>
//    sqlOptions.EnableRetryOnFailure()));

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
builder.Services.AddLogging(logging => logging.AddConsole());

// Register services by Awais
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;    // true after test
});

// Configure cookie authentication
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.AccessDeniedPath = "/Home/AccessDenied";
//    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CookieAuth";
})
.AddCookie("CookieAuth", options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.Name = "IMS_Auth";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});
builder.Services.AddScoped<IDbContextFactory, AppDbContextFactory>();
builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<DbContextResolver>();

builder.Services.AddAuthorization();


var app = builder.Build();

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
// Middleware to store the current user's username in session
app.Use(async (context, next) =>
{
    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        var userName = context.User.Identity.Name;
        var domain = context.User.Claims.FirstOrDefault(c => c.Type == "Domain")?.Value;
        var role = context.User.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value;
        var UsrId = context.User.Claims.FirstOrDefault(c=>c.Type=="UserId")?.Value;

        if (!string.IsNullOrEmpty(userName))
        {
            context.Session.SetString("UserName", userName);
            context.Session.SetString("Domain", domain ?? string.Empty);
            context.Session.SetString("IsAdmin", role?? string.Empty);
            context.Session.SetString("UserId", UsrId ?? string.Empty);
        }
    }

    await next.Invoke();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
