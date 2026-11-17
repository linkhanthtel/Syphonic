using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // One address, one account: without this two learners could share an email.
        options.User.RequireUniqueEmail = true;

        // Length matters more than symbols, and one simple rule is easy to explain on the sign-up form.
        // Keep the length in step with RegisterModel.MinPasswordLength.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Five wrong passwords lock the account for five minutes, which blunts password guessing.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// App services (Scoped = one instance per HTTP request, safe to use with ApplicationDbContext).
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<ReviewService>();
// Singleton = one shared instance: the country file is read once and never changes.
builder.Services.AddSingleton<CountryData>();

builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminOnly");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

await SeedData.InitializeAsync(app.Services, app.Configuration, app.Environment);

app.Run();
