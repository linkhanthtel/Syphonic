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
        await BaselineLegacyDatabaseAsync(db);
        // Applies any migrations in Migrations/ that this database hasn't run yet.
        // A brand-new database gets every table; an existing one only gets what's new.
        await db.Database.MigrateAsync();

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
    /// Before migrations were introduced, the app built its tables with EnsureCreated, which
    /// leaves no "__EFMigrationsHistory" record. Calling Migrate() on such a database would try
    /// to create tables that already exist and crash. If the database already matches the first
    /// migration (InitialCreate), record that migration as applied so Migrate() continues from there.
    /// This runs once per old database; afterwards the history table exists and it does nothing.
    /// </summary>
    private static async Task BaselineLegacyDatabaseAsync(ApplicationDbContext db)
    {
        if (!await db.Database.CanConnectAsync() || await TableExistsAsync(db, "__EFMigrationsHistory"))
            return;

        if (!await TableExistsAsync(db, "Lessons"))
            return; // Empty database: let Migrate() create everything.

        // InitialCreate includes the quiz tables. Databases older than that can't be baselined safely.
        if (!await TableExistsAsync(db, "QuizQuestions") || !await TableExistsAsync(db, "QuizAttempts"))
            throw new InvalidOperationException(
                "syphonic.db was created by a very old version of Syphonic and can't be upgraded automatically. " +
                "Stop the app, delete Syphonic/syphonic.db (this resets local data), and run 'dotnet run' again.");

        var initialMigration = db.Database.GetMigrations().First();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({initialMigration}, '9.0.0')");
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string table)
    {
        var count = (await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {table}")
            .ToListAsync()).Single();
        return count > 0;
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
