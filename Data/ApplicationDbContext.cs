using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Syphonic.Models;

namespace Syphonic.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();

    public DbSet<UserActivity> UserActivities => Set<UserActivity>();

    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Lesson>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.Property(e => e.Slug).HasMaxLength(256);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);
        });

        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Progress)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserActivity>(entity =>
        {
            entity.Property(e => e.Kind).HasMaxLength(64);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizQuestion>(entity =>
        {
            entity.Property(e => e.Prompt).HasMaxLength(1000);
            entity.Property(e => e.OptionA).HasMaxLength(300);
            entity.Property(e => e.OptionB).HasMaxLength(300);
            entity.Property(e => e.OptionC).HasMaxLength(300);
            entity.Property(e => e.OptionD).HasMaxLength(300);
            entity.Property(e => e.Explanation).HasMaxLength(2000);
            entity.HasIndex(e => new { e.LessonId, e.OrderIndex });
            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Questions)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Bookmark>(entity =>
        {
            // A learner can save each lesson only once; also speeds up "is this saved?" lookups.
            entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.LessonId });
            entity.Ignore(e => e.Percent);
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
