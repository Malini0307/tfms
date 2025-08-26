using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;
using TradeSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF license mode
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure EF Core
builder.Services.AddDbContext<TFMSDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("sqlcon")));

builder.Services.AddScoped<ILetterOfCreditService, LetterOfCreditService>();
builder.Services.AddScoped<IBankGuaranteeService, BankGuaranteeService>();
builder.Services.AddScoped<ITradeDocumentService, TradeDocumentService>();
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();

builder.Services.AddAuthentication(/* your auth config */);
builder.Services.AddAuthorization();



// Configure cookie authentication
// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<TFMSDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.Cookie.Name = "TFMS.Auth";
});



var app = builder.Build();

// Migrate & Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TFMSDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


//app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
