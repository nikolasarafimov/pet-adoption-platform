using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Infrastructure.External;
using PetAdoption.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IAdopterService, AdopterService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();

builder.Services.AddHttpClient<IDogApiService, DogApiService>(client =>
{
    var baseUrl = builder.Configuration["DogApi:BaseUrl"] ?? "https://api.thedogapi.com/";
    if (!baseUrl.EndsWith("/")) baseUrl += "/";
    client.BaseAddress = new Uri(baseUrl);

    var key = builder.Configuration["DogApi:ApiKey"];
    if (!string.IsNullOrWhiteSpace(key))
        client.DefaultRequestHeaders.Add("x-api-key", key);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
        await db.Database.MigrateAsync();

    await DbInitializer.SeedAsync(db, users, roles);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "home",
    pattern: "Home/{action=Welcome}/{id?}",
    defaults: new { controller = "Home" });

app.MapControllerRoute(
    name: "pets",
    pattern: "Pets/{action=Index}/{id?}",
    defaults: new { controller = "Pets" });

app.MapControllerRoute(
    name: "breeds",
    pattern: "Breeds/{action=Index}/{id?}",
    defaults: new { controller = "Breeds" });

app.MapControllerRoute(
    name: "adoptionrequests",
    pattern: "AdoptionRequests/{action=Index}/{id?}",
    defaults: new { controller = "AdoptionRequests" });

app.MapControllerRoute(
    name: "shelters",
    pattern: "Shelters/{action=Index}/{id?}",
    defaults: new { controller = "Shelters" });

app.MapControllerRoute(
    name: "adoptersadmin",
    pattern: "Admin/Adopters/{action=Index}/{id?}",
    defaults: new { controller = "AdoptersAdmin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

app.Run();