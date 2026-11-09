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
        var createdNow = await db.Database.EnsureCreatedAsync();
        if (!createdNow)
            await EnsureSchemaIsCurrentAsync(db);

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

        if (!await db.Lessons.AnyAsync())
            await SeedLessonsAsync(db);

        if (!await db.QuizQuestions.AnyAsync())
            await SeedQuizQuestionsAsync(db);
    }

    /// <summary>
    /// EnsureCreated only builds tables for a brand-new database. If syphonic.db was
    /// created by an older version of the app, newer tables are missing, so fail fast
    /// with instructions instead of crashing later with "no such table".
    /// </summary>
    private static async Task EnsureSchemaIsCurrentAsync(ApplicationDbContext db)
    {
        string[] requiredTables = ["QuizQuestions", "QuizAttempts"];

        foreach (var table in requiredTables)
        {
            var count = (await db.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {table}")
                .ToListAsync()).Single();

            if (count == 0)
                throw new InvalidOperationException(
                    $"The database is missing the '{table}' table because it was created by an older version of Syphonic. " +
                    "Stop the app, delete Syphonic/syphonic.db (this resets local data), and run 'dotnet run' again.");
        }
    }

    private static async Task SeedLessonsAsync(ApplicationDbContext db)
    {
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

    private static async Task SeedQuizQuestionsAsync(ApplicationDbContext db)
    {
        var lessonIds = await db.Lessons
            .Where(l => l.Slug == "earth-at-a-glance" || l.Slug == "continents-oceans" || l.Slug == "reading-political-maps")
            .ToDictionaryAsync(l => l.Slug, l => l.Id);

        void Add(string slug, int order, string prompt, string a, string b, string? c, string? d, int correct, string explanation)
        {
            if (!lessonIds.TryGetValue(slug, out var lessonId))
                return;

            db.QuizQuestions.Add(new QuizQuestion
            {
                LessonId = lessonId,
                OrderIndex = order,
                Prompt = prompt,
                OptionA = a,
                OptionB = b,
                OptionC = c,
                OptionD = d,
                CorrectOption = correct,
                Explanation = explanation
            });
        }

        Add("earth-at-a-glance", 10,
            "Which three ideas does geography join together?",
            "Places, patterns, and systems", "Numbers, letters, and symbols", "Plants, animals, and fungi", null,
            0, "Geography connects places, the patterns between them, and the systems that shape them.");
        Add("earth-at-a-glance", 20,
            "Roughly how much of Earth's surface is covered by ocean?",
            "About 30%", "About 50%", "About 70%", "About 90%",
            2, "Oceans cover roughly 71% of Earth's surface.");

        Add("continents-oceans", 10,
            "How many continents are traditionally taught?",
            "Five", "Six", "Seven", "Eight",
            2, "Africa, Antarctica, Asia, Australia, Europe, North America, and South America.");
        Add("continents-oceans", 20,
            "Which is the largest ocean?",
            "Atlantic", "Pacific", "Indian", "Arctic",
            1, "The Pacific is the largest and deepest ocean, bigger than all land combined.");
        Add("continents-oceans", 30,
            "Which continent has no permanent population?",
            "Antarctica", "Australia", "Europe", null,
            0, "Antarctica only hosts temporary research staff.");

        Add("reading-political-maps", 10,
            "What does a political map mainly show?",
            "Elevation and terrain", "Countries, borders, and capitals", "Rainfall and climate zones", null,
            1, "Political maps focus on human-made boundaries and places such as capitals.");
        Add("reading-political-maps", 20,
            "Why can Greenland look huge on many world maps?",
            "It is the biggest country", "Map projections distort size at high latitudes", "It has the most islands", null,
            1, "Projections like Mercator stretch areas near the poles.");
        Add("reading-political-maps", 30,
            "A country's capital is always its largest city.",
            "True", "False", null, null,
            1, "Not always: Canberra (Australia) and Ottawa (Canada) are capitals but not the largest cities.");

        await db.SaveChangesAsync();
    }
}
