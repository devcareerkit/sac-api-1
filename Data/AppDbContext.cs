using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entities.Entity> Entities => Set<Entities.Entity>();
    public DbSet<Entities.User> Users => Set<Entities.User>();
    public DbSet<Entities.Submission> Submissions => Set<Entities.Submission>();
    public DbSet<Entities.SubmissionValue> SubmissionValues => Set<Entities.SubmissionValue>();
    public DbSet<Entities.DocumentRecord> DocumentRecords => Set<Entities.DocumentRecord>();
    public DbSet<Entities.Comment> Comments => Set<Entities.Comment>();
    public DbSet<Entities.TaskItem> TaskItems => Set<Entities.TaskItem>();
    public DbSet<Entities.RiskScore> RiskScores => Set<Entities.RiskScore>();
    public DbSet<Entities.Notification> Notifications => Set<Entities.Notification>();
    public DbSet<Entities.AuditLogEntry> AuditLogEntries => Set<Entities.AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Add configurations here or via separate Configuration classes
    }
}
