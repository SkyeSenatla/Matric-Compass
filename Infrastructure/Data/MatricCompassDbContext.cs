namespace Infrastructure.Data;

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
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        // Week 5 Day 3: matches GET /api/bursary-applications?studentId=...
        // exactly — equality column first (StudentId), then the sort columns
        // in sort order (Deadline, then the Id tiebreaker). Postgres can walk
        // this index already in page order and stop after LIMIT rows: no
        // sort step, no reading the student's whole history.
        modelBuilder.Entity<BursaryApplication>()
            .HasIndex(b => new { b.StudentId, b.Deadline, b.Id })
            .HasDatabaseName("IX_BursaryApplications_StudentId_Deadline_Id");

        // -- Week 5 Day 3: the database defends the rules itself ----------
        // Each of these backs up a rule the C# code ALREADY enforces. The C#
        // check stays (it gives a friendly message); the constraint is what
        // still holds when two requests race past the C# check at once.

        // Found while writing today's constraint test: Subject and
        // CareerRecommendation mint their own Id in the constructor, but EF
        // Core's convention for a Guid key is "the database generates it on
        // insert". So a NEW Subject added to an ALREADY-TRACKED Student
        // arrives with a key already set, EF concludes it must be an existing
        // row, and sends an UPDATE — which matches 0 rows and throws
        // DbUpdateConcurrencyException. ValueGeneratedNever() says "this code
        // always supplies the key", so a new child is correctly an INSERT.
        modelBuilder.Entity<Subject>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<CareerRecommendation>().Property(r => r.Id).ValueGeneratedNever();

        // Student.EnrollSubject() rejects a duplicate code — but only against
        // the subjects currently LOADED into _subjects.
        modelBuilder.Entity<Subject>()
            .HasIndex(s => new { s.StudentId, s.Code })
            .IsUnique()
            .HasDatabaseName("UX_Subjects_StudentId_Code");

        // BursaryApplicationService.CreateAsync: no second ACTIVE application
        // with the same funder. A partial (filtered) unique index — unique
        // only among rows that aren't Rejected, exactly like the C# rule.
        // 4 = BursaryApplicationStatus.Rejected (enums are stored as integers).
        modelBuilder.Entity<BursaryApplication>()
            .HasIndex(b => new { b.StudentId, b.Funder })
            .IsUnique()
            .HasFilter($"\"Status\" <> {(int)BursaryApplicationStatus.Rejected}")
            .HasDatabaseName("UX_BursaryApplications_StudentId_Funder_Active");

        // BursaryApplication's constructor and UpdateDetails() reject
        // amount <= 0.
        modelBuilder.Entity<BursaryApplication>()
            .ToTable(t => t.HasCheckConstraint("CK_BursaryApplications_Amount_Positive", "\"Amount\" > 0"));

        // Week 5 Day 3: optimistic concurrency. A uint property marked as a row
        // version is mapped by Npgsql to PostgreSQL's xmin system column — the
        // id of the transaction that last wrote the row. No new column, no
        // trigger: every UPDATE now ends "... WHERE "Id" = @id AND xmin = @v",
        // and 0 rows affected means someone else wrote first.
        modelBuilder.Entity<BursaryApplication>()
            .Property(b => b.Version)
            .IsRowVersion();

        // -- Week 6 Day 1: who can sign in, and the tokens they hold ---------
        modelBuilder.Entity<AppUser>(user =>
        {
            // One login per email address — enforced by the database, not
            // just by a lookup before insert (Week 5 Day 3).
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.Role).HasMaxLength(32);

            // A learner's login points at their student record. Deleting the
            // student deletes the login with it.
            user.HasOne<Student>()
                .WithMany()
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            // Lookups are by hash, so the hash is the index. Unique: two
            // tokens with the same hash would mean a broken random generator.
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);

            token.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Two browser tabs refreshing the same token at the same moment:
            // only one rotation may win (Week 5 Day 3's xmin, reused).
            token.Property(t => t.Version).IsRowVersion();
        });
    }
}
