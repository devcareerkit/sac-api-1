using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entities.Entity> Entities => Set<Entities.Entity>();
    public DbSet<Entities.User> Users => Set<Entities.User>();
    public DbSet<Entities.ReportingCycle> ReportingCycles => Set<Entities.ReportingCycle>();
    public DbSet<Entities.KpiTarget> KpiTargets => Set<Entities.KpiTarget>();
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

        modelBuilder.Entity<Entities.Entity>(e =>
        {
            e.ToTable("entities");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Entities.User>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Entities.ReportingCycle>(e =>
        {
            e.ToTable("reporting_cycles");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Label).HasColumnName("label");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Entities.KpiTarget>(e =>
        {
            e.ToTable("kpi_targets");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.CycleId).HasColumnName("cycle_id");
            e.Property(x => x.KpiName).HasColumnName("kpi_name");
            e.Property(x => x.TargetValue).HasColumnName("target_value");
            e.Property(x => x.Unit).HasColumnName("unit");
        });

        modelBuilder.Entity<Entities.Submission>(e =>
        {
            e.ToTable("submissions");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.CycleId).HasColumnName("cycle_id");
            e.Property(x => x.SubmittedBy).HasColumnName("submitted_by");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
        });

        modelBuilder.Entity<Entities.SubmissionValue>(e =>
        {
            e.ToTable("submission_values");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubmissionId).HasColumnName("submission_id");
            e.Property(x => x.KpiTargetId).HasColumnName("kpi_target_id");
            e.Property(x => x.ActualValue).HasColumnName("actual_value");
            e.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<Entities.DocumentRecord>(e =>
        {
            e.ToTable("documents");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubmissionId).HasColumnName("submission_id");
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.DocType).HasColumnName("doc_type");
            e.Property(x => x.FileUrl).HasColumnName("file_url");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.KpiTagId).HasColumnName("kpi_tag_id");
            e.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
            e.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        });

        modelBuilder.Entity<Entities.Comment>(e =>
        {
            e.ToTable("comments");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DocumentId).HasColumnName("document_id");
            e.Property(x => x.SubmissionId).HasColumnName("submission_id");
            e.Property(x => x.AuthorId).HasColumnName("author_id");
            e.Property(x => x.Body).HasColumnName("body");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Entities.TaskItem>(e =>
        {
            e.ToTable("tasks");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubmissionId).HasColumnName("submission_id");
            e.Property(x => x.AssignedBy).HasColumnName("assigned_by");
            e.Property(x => x.AssignedTo).HasColumnName("assigned_to");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Entities.RiskScore>(e =>
        {
            e.ToTable("risk_scores");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.CycleId).HasColumnName("cycle_id");
            e.Property(x => x.Score).HasColumnName("score");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.ComputedAt).HasColumnName("computed_at");
        });

        modelBuilder.Entity<Entities.Notification>(e =>
        {
            e.ToTable("notifications");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubmissionId).HasColumnName("submission_id");
            e.Property(x => x.RecipientId).HasColumnName("recipient_id");
            e.Property(x => x.Type).HasColumnName("type");
            e.Property(x => x.Channel).HasColumnName("channel");
            e.Property(x => x.SentAt).HasColumnName("sent_at");
        });

        modelBuilder.Entity<Entities.AuditLogEntry>(e =>
        {
            e.ToTable("audit_log");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ActorId).HasColumnName("actor_id");
            e.Property(x => x.Action).HasColumnName("action");
            e.Property(x => x.TargetTable).HasColumnName("target_table");
            e.Property(x => x.TargetId).HasColumnName("target_id");
            e.Property(x => x.Metadata).HasColumnName("metadata");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
