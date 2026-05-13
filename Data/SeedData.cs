using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Syphonic.Models;

namespace Syphonic.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminRoleName = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));

        var adminEmail = configuration["AdminSeed:Email"] ?? "admin@localhost";
        var adminPassword = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Configure AdminSeed:Password outside of Development.");

            adminPassword = "ChangeMe!1";
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };
            var create = await userManager.CreateAsync(admin, adminPassword);
            if (!create.Succeeded)
                throw new InvalidOperationException(string.Join("; ", create.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(admin, adminRoleName);
        }

        if (await db.Lessons.AnyAsync())
            return;

        var utc = DateTimeOffset.UtcNow;
        db.Lessons.AddRange(
            new Lesson
            {
                Title = "Earth at a glance",
                Slug = "earth-at-a-glance",
                Summary = "A short orientation before you dive into continents, countries, capitals, and maps.",
                Content = """
                          # Earth at a glance

                          Geography joins **places**, **patterns**, and **systems** across the globe.

                          As you explore Syphonic lessons, you'll build map sense, memorize key facts, and connect regions to rivers, climates, borders, capitals, and cultures.

                          **Next lesson:** continents and oceans.
                          """,
                OrderIndex = 10,
                Published = true,
                CreatedAt = utc,
                UpdatedAt = utc
            },
            new Lesson
            {
                Title = "Continents & oceans",
                Slug = "continents-oceans",
                Summary = "The major landmasses of the planet and where the world's oceans sit between them.",
                Content = """
                          # Continents & oceans

                          There are **seven continents** traditionally taught in geography: Africa, Antarctica, Asia, Australia, Europe, North America, and South America.

                          Earth's surface is dominated by **five interconnected oceans**.

                          Practical tip: memorize relative positions (east/west poles, equator crossings) alongside names so maps feel anchored.
                          """,
                OrderIndex = 20,
                Published = true,
                CreatedAt = utc,
                UpdatedAt = utc
            },
            new Lesson
            {
                Title = "Reading political maps",
                Slug = "reading-political-maps",
                Summary = "Understand borders, capitals, and scale so political maps stop feeling abstract.",
                Content = """
                          # Reading political maps

                          Political maps show **countries, borders, cities, capitals**, and disputed areas.

                          Key skills:
                          - **Legend & scale**: how distance on the ground relates to what's on paper.
                          - **Projection distortions**: high latitudes can look larger than reality.
                          - **Capitals vs largest cities**: not always the same place.

                          Try comparing the same region on Syphonic Maps with a roadmap-style basemap versus a thematic layer when you wire them later.
                          """,
                OrderIndex = 30,
                Published = true,
                CreatedAt = utc,
                UpdatedAt = utc
            });

        await db.SaveChangesAsync();
    }
}
