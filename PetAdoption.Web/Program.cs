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

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute()
    );
});

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(GenericRepository<>)
);

builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IAdopterService, AdopterService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();

builder.Services.AddHttpClient<IDogApiService, DogApiService>(client =>
{
    var configuredBaseUrl =
        builder.Configuration["DogApi:BaseUrl"]
        ?? "https://api.thedogapi.com/";

    var baseUrl = configuredBaseUrl.Trim().TrimEnd('/') + "/";

    if (!Uri.TryCreate(
            baseUrl,
            UriKind.Absolute,
            out var baseAddress
        ) ||
        (baseAddress.Scheme != Uri.UriSchemeHttps &&
         baseAddress.Scheme != Uri.UriSchemeHttp))
    {
        throw new InvalidOperationException(
            "DogApi:BaseUrl must be a valid absolute HTTP or HTTPS URL."
        );
    }

    client.BaseAddress = baseAddress;
    client.Timeout = TimeSpan.FromSeconds(15);

    var apiKey = builder.Configuration["DogApi:ApiKey"];

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add(
            "x-api-key",
            apiKey
        );
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    var seedDataEnabled =
        builder.Configuration.GetValue<bool?>("SeedData:Enabled")
        ?? app.Environment.IsDevelopment();

    if (seedDataEnabled)
    {
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var credentials = CreateDemoSeedCredentials(
            builder.Configuration,
            app.Environment
        );

        await DbInitializer.SeedAsync(
            db,
            userManager,
            roleManager,
            credentials
        );
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" }
);

app.MapControllerRoute(
    name: "home",
    pattern: "Home/{action=Welcome}/{id?}",
    defaults: new { controller = "Home" }
);

app.MapControllerRoute(
    name: "pets",
    pattern: "Pets/{action=Index}/{id?}",
    defaults: new { controller = "Pets" }
);

app.MapControllerRoute(
    name: "breeds",
    pattern: "Breeds/{action=Index}/{id?}",
    defaults: new { controller = "Breeds" }
);

app.MapControllerRoute(
    name: "adoptionrequests",
    pattern: "AdoptionRequests/{action=Index}/{id?}",
    defaults: new { controller = "AdoptionRequests" }
);

app.MapControllerRoute(
    name: "shelters",
    pattern: "Shelters/{action=Index}/{id?}",
    defaults: new { controller = "Shelters" }
);

app.MapControllerRoute(
    name: "adoptersadmin",
    pattern: "Admin/Adopters/{action=Index}/{id?}",
    defaults: new { controller = "AdoptersAdmin" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}"
);

app.Run();

static DemoSeedCredentials CreateDemoSeedCredentials(
    IConfiguration configuration,
    IHostEnvironment environment
)
{
    if (environment.IsDevelopment())
    {
        return new DemoSeedCredentials(
            AdminEmail:
                configuration["SeedData:AdminEmail"]
                ?? "admin@pets.com",
            AdminPassword:
                configuration["SeedData:AdminPassword"]
                ?? "Admin#12345",
            ShelterEmail:
                configuration["SeedData:ShelterEmail"]
                ?? "shelter@pets.com",
            ShelterPassword:
                configuration["SeedData:ShelterPassword"]
                ?? "Shelter#123",
            AdopterEmail:
                configuration["SeedData:AdopterEmail"]
                ?? "adopter@pets.com",
            AdopterPassword:
                configuration["SeedData:AdopterPassword"]
                ?? "Adopter#123"
        );
    }

    return new DemoSeedCredentials(
        AdminEmail: GetRequiredConfigurationValue(
            configuration,
            "SeedData:AdminEmail"
        ),
        AdminPassword: GetRequiredConfigurationValue(
            configuration,
            "SeedData:AdminPassword"
        ),
        ShelterEmail: GetRequiredConfigurationValue(
            configuration,
            "SeedData:ShelterEmail"
        ),
        ShelterPassword: GetRequiredConfigurationValue(
            configuration,
            "SeedData:ShelterPassword"
        ),
        AdopterEmail: GetRequiredConfigurationValue(
            configuration,
            "SeedData:AdopterEmail"
        ),
        AdopterPassword: GetRequiredConfigurationValue(
            configuration,
            "SeedData:AdopterPassword"
        )
    );
}

static string GetRequiredConfigurationValue(
    IConfiguration configuration,
    string key
)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Configuration value '{key}' is required when " +
            "demo seed data is enabled outside Development."
        );
    }

    return value;
}