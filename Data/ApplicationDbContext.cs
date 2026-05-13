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
    }
}
