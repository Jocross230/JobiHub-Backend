using CVBuilder.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Data;

public class CvBuilderDbContext : DbContext
{
    public CvBuilderDbContext(DbContextOptions<CvBuilderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<CvSkill> CvSkills => Set<CvSkill>();
    public DbSet<CvExperience> CvExperiences => Set<CvExperience>();
    public DbSet<CvProject> CvProjects => Set<CvProject>();
    public DbSet<CvEducation> CvEducations => Set<CvEducation>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<SavedCandidate> SavedCandidates { get; set; }
    public DbSet<RecruitmentRequest> RecruitmentRequests { get; set; }
    public DbSet<SupportIssue> SupportIssues { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Payment> Payments { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PasswordResetToken>()
    .HasIndex(x => x.Token)
    .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SavedJob>(entity =>
        {
            entity.ToTable("SavedJobs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SavedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.UserId, x.JobId })
                .IsUnique();
        });
        modelBuilder.Entity<Job>()
    .HasOne(x => x.Business)
    .WithMany()
    .HasForeignKey(x => x.BusinessId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Business>()
    .HasOne(x => x.User)
    .WithOne(x => x.Business)
    .HasForeignKey<Business>(x => x.UserId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
    .Property(x => x.Role)
    .HasMaxLength(30)
    .HasDefaultValue("JobSeeker")
    .IsRequired();
        modelBuilder.Entity<Cv>()
    .HasOne(x => x.User)
    .WithMany(x => x.Cvs)
    .HasForeignKey(x => x.UserId)
    .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CvSkill>()
            .HasOne(x => x.Cv)
            .WithMany(x => x.Skills)
            .HasForeignKey(x => x.CvId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CvExperience>()
    .HasOne(x => x.Cv)
    .WithMany(x => x.Experiences)
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CvProject>()
    .HasOne(x => x.Cv)
    .WithMany(x => x.Projects)
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CvEducation>()
    .HasOne(x => x.Cv)
    .WithMany(x => x.Educations)
    .HasForeignKey(x => x.CvId)
    .OnDelete(DeleteBehavior.Cascade);
    }
}