namespace API.Data;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

public class MatricCompassDbContext : DbContext
{
    public MatricCompassDbContext(DbContextOptions<MatricCompassDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<TertiaryApplication> TertiaryApplications => Set<TertiaryApplication>();
    public DbSet<BursaryApplication> BursaryApplications => Set<BursaryApplication>();
    public DbSet<AptitudeTest> AptitudeTests => Set<AptitudeTest>();
    public DbSet<CareerRecommendation> CareerRecommendations => Set<CareerRecommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(student =>
        {
            student.HasIndex(s => s.LearnerReferenceNumber).IsUnique();

            // .WithOne() with no argument: Subject has no navigation back to
            // Student — deliberately one-directional. HasForeignKey points
            // EF Core at the StudentId column that already exists rather
            // than letting it invent a shadow one.
            student.HasMany(s => s.Subjects)
                .WithOne()
                .HasForeignKey(subject => subject.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Subjects is a read-only wrapper over a private field, same shape
            // as SubjectCodes was — but this time it's a real navigation to a
            // real entity, not a primitive collection, so EF Core can use the
            // backing field directly instead of needing a mappable column.
            student.Navigation(s => s.Subjects).UsePropertyAccessMode(PropertyAccessMode.Field);

            student.HasMany(s => s.BursaryApplications)
                .WithOne()
                .HasForeignKey(b => b.StudentId);
            student.Navigation(s => s.BursaryApplications).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<AptitudeTest>(test =>
        {
            test.HasMany(t => t.Recommendations)
                .WithOne()
                .HasForeignKey(r => r.AptitudeTestId)
                .OnDelete(DeleteBehavior.Cascade);
            test.Navigation(t => t.Recommendations).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // "One Guardian, one linked Student, to start" — at most one
        // Guardian row per StudentId.
        modelBuilder.Entity<Guardian>()
            .HasIndex(g => g.StudentId)
            .IsUnique();

        // Still a computed, read-only wrapper over a private List<string> —
        // not stable, settable storage EF Core can track or materialize
        // into — so it stays ignored for now.
        modelBuilder.Entity<BursaryApplication>().Ignore(b => b.RequiredDocuments);
    }
}
