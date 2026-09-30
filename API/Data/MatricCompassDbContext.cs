namespace API.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

public class MatricCompassDbContext : DbContext
{
    public MatricCompassDbContext(DbContextOptions<MatricCompassDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<TertiaryApplication> TertiaryApplications => Set<TertiaryApplication>();
    public DbSet<BursaryApplication> BursaryApplications => Set<BursaryApplication>();
    public DbSet<AptitudeTest> AptitudeTests => Set<AptitudeTest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All three of these are computed, read-only wrappers over a private
        // field — not stable, settable storage EF Core can track or
        // materialize into. Ignoring them today is deliberate, not a
        // workaround to forget about: Week 5 Day 2's actual job is replacing
        // Student.SubjectCodes with a real Subject entity and a proper
        // one-to-many relationship — the right fix for this shape, not a
        // trick to make EF Core accept it as-is.
        modelBuilder.Entity<Student>().Ignore(s => s.SubjectCodes);
        modelBuilder.Entity<BursaryApplication>().Ignore(b => b.RequiredDocuments);
        modelBuilder.Entity<AptitudeTest>().Ignore(t => t.RecommendedCareers);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.LearnerReferenceNumber)
            .IsUnique();
    }
}
