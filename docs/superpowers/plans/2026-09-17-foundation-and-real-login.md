# Foundation Fix + Real Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the EF Core entity model match the real Postgres schema, and replace the fake `AuthController.Login` stub with real email/password authentication that issues a signed JWT carrying role and entity-scope claims.

**Architecture:** Rewrite `Data/Entities/*.cs` to mirror `init.sql` 1:1 (UUID keys, snake_case column mapping via Fluent API). Add a `password_hash` column via a new migration SQL file, applied through the existing `DatabaseInitializer`. Add a small `IJwtTokenGenerator` service so token creation is unit-testable in isolation. Rewrite `AuthController` to use `AppDbContext` + `PasswordHasher<User>` + `IJwtTokenGenerator`. Add a new xUnit test project (`test/DsacReporting.Api.Tests`) using EF Core's InMemory provider for `AppDbContext`-backed tests.

**Tech Stack:** .NET 8, EF Core 8 (Npgsql provider + InMemory provider for tests), ASP.NET Core Identity's `PasswordHasher<T>`, `System.IdentityModel.Tokens.Jwt` (already available transitively via `Microsoft.AspNetCore.Authentication.JwtBearer`), xUnit.

---

## File Structure

**Modify:**
- `Data/Entities/Entity.cs` — add `Type`, `CreatedAt`; switch `Id` to `Guid`
- `Data/Entities/User.cs` — add `EntityId`, `FullName`, `Role`, `PasswordHash`, `CreatedAt`; switch `Id` to `Guid`; remove `Username`
- `Data/Entities/Submission.cs` — add `CycleId`, `SubmittedBy`, `Status`; switch `Id`/`EntityId` to `Guid`
- `Data/Entities/SubmissionValue.cs` — replace `Key`/`Value` with `KpiTargetId`/`ActualValue`/`Notes`; switch to `Guid`
- `Data/Entities/DocumentRecord.cs` — add `EntityId`, `DocType`, `Version`, `KpiTagId`, `UploadedBy`, `UploadedAt`; rename `Url`→`FileUrl`, drop `FileName` (not in schema); switch to `Guid`
- `Data/Entities/Comment.cs` — add `DocumentId`, rename `UserId`→`AuthorId`, `Text`→`Body`; switch to `Guid`
- `Data/Entities/TaskItem.cs` — add `SubmissionId`, `AssignedBy`, `AssignedTo`, `Status`, `DueDate`, `CreatedAt`; rename `Completed`→derived from `Status`; switch to `Guid`
- `Data/Entities/RiskScore.cs` — add `CycleId`, `Reason`; rename `CalculatedAt`→`ComputedAt`; switch to `Guid`
- `Data/Entities/Notification.cs` — add `SubmissionId`, `Type`, `Channel`, `SentAt`; remove `Message`/`Read` (not in schema); switch to `Guid`
- `Data/Entities/AuditLogEntry.cs` — add `TargetTable`, `TargetId`, `Metadata`; rename `PerformedBy`→`ActorId` (Guid FK, not string), `PerformedAt`→`CreatedAt`; switch to `Guid`
- `Data/AppDbContext.cs` — add `ReportingCycles`, `KpiTargets` DbSets; add Fluent API column/table mappings
- `Data/Database/DatabaseInitializer.cs` — no change needed (already generic over any SQL file path)
- `Program.cs` — apply the new migration file alongside `init.sql`; register `IJwtTokenGenerator`
- `Controllers/AuthController.cs` — real login logic
- `.github/workflows/ci.yml` — update test path if needed (already runs `dotnet test` if a `tests` dir exists; we'll use `test/` matching sibling convention, so this needs a small fix)

**Create:**
- `Data/Entities/ReportingCycle.cs`
- `Data/Entities/KpiTarget.cs`
- `Data/Database/002_add_password_hash.sql`
- `Data/Database/003_seed_demo_users.sql`
- `DTOs/LoginRequestDto.cs`
- `DTOs/LoginResponseDto.cs`
- `Services/IJwtTokenGenerator.cs`
- `Services/JwtTokenGenerator.cs`
- `test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj`
- `test/DsacReporting.Api.Tests/JwtTokenGeneratorTests.cs`
- `test/DsacReporting.Api.Tests/PasswordHasherTests.cs`
- `test/DsacReporting.Api.Tests/AuthControllerTests.cs`

---

### Task 1: Rewrite entity models to match `init.sql`

**Files:**
- Modify: `Data/Entities/Entity.cs`
- Modify: `Data/Entities/User.cs`
- Create: `Data/Entities/ReportingCycle.cs`
- Create: `Data/Entities/KpiTarget.cs`
- Modify: `Data/Entities/Submission.cs`
- Modify: `Data/Entities/SubmissionValue.cs`
- Modify: `Data/Entities/DocumentRecord.cs`
- Modify: `Data/Entities/Comment.cs`
- Modify: `Data/Entities/TaskItem.cs`
- Modify: `Data/Entities/RiskScore.cs`
- Modify: `Data/Entities/Notification.cs`
- Modify: `Data/Entities/AuditLogEntry.cs`

There's no test for this task — it's a pure data-shape change verified by successful compilation and by Task 6's `AuthControllerTests` exercising `User`/`Entity` through EF Core.

- [ ] **Step 1: Rewrite `Data/Entities/Entity.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "public_entity" or "npo"
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 2: Rewrite `Data/Entities/User.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? EntityId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "entity_officer", "dsac_me", "dsac_exec"
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 3: Create `Data/Entities/ReportingCycle.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class ReportingCycle
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 4: Create `Data/Entities/KpiTarget.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class KpiTarget
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public decimal? TargetValue { get; set; }
    public string? Unit { get; set; }
}
```

- [ ] **Step 5: Rewrite `Data/Entities/Submission.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public Guid? SubmittedBy { get; set; }
    public string Status { get; set; } = "not_started"; // not_started, in_progress, submitted, missed
    public DateTimeOffset? SubmittedAt { get; set; }
}
```

- [ ] **Step 6: Rewrite `Data/Entities/SubmissionValue.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class SubmissionValue
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid KpiTargetId { get; set; }
    public decimal? ActualValue { get; set; }
    public string? Notes { get; set; }
}
```

- [ ] **Step 7: Rewrite `Data/Entities/DocumentRecord.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class DocumentRecord
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid EntityId { get; set; }
    public string DocType { get; set; } = string.Empty;
    // strategic_plan, app, operational_plan, annual_report, quarterly_report, financials
    public string FileUrl { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid? KpiTagId { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
```

- [ ] **Step 8: Rewrite `Data/Entities/Comment.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 9: Rewrite `Data/Entities/TaskItem.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? AssignedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open"; // open, done
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 10: Rewrite `Data/Entities/RiskScore.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class RiskScore
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public decimal Score { get; set; } // 0-100
    public string? Reason { get; set; }
    public DateTimeOffset ComputedAt { get; set; }
}
```

- [ ] **Step 11: Rewrite `Data/Entities/Notification.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty; // nudge, escalation
    public string Channel { get; set; } = "email";
    public DateTimeOffset SentAt { get; set; }
}
```

- [ ] **Step 12: Rewrite `Data/Entities/AuditLogEntry.cs`**

```csharp
namespace DsacReporting.Api.Data.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; } // JSON stored as text
    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 13: Build to catch compile errors from the type changes**

Run: `dotnet build DsacReporting.Api.csproj --configuration Release`
Expected: FAIL — `AppDbContext` and any code referencing old property names (e.g.
`Submission.EntityId` as `int`, `SubmissionService` constructing `Submission`) will not
compile yet. This is expected; Task 2 fixes `AppDbContext`, Task 3 fixes `SubmissionService`.

- [ ] **Step 14: Commit**

```bash
git add Data/Entities/
git commit -m "refactor: rewrite entity models to match init.sql schema"
```

---

### Task 2: Update `AppDbContext` with new DbSets and column mappings

**Files:**
- Modify: `Data/AppDbContext.cs`

- [ ] **Step 1: Rewrite `Data/AppDbContext.cs`**

```csharp
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
```

- [ ] **Step 2: Build to confirm `AppDbContext` compiles (other errors from `SubmissionService` still expected)**

Run: `dotnet build DsacReporting.Api.csproj --configuration Release`
Expected: FAIL only on `Services/SubmissionService.cs` (still references old `Submission`
shape) and possibly `DTOs/SubmissionDto.cs`. If `AppDbContext.cs` itself has an error, fix
it before moving on.

- [ ] **Step 3: Commit**

```bash
git add Data/AppDbContext.cs
git commit -m "refactor: map AppDbContext to snake_case schema with new entities"
```

---

### Task 3: Fix `SubmissionService` and `SubmissionDto` for the new `Submission` shape

**Files:**
- Modify: `Services/SubmissionService.cs`
- Modify: `DTOs/SubmissionDto.cs`

- [ ] **Step 1: Rewrite `DTOs/SubmissionDto.cs`**

```csharp
namespace DsacReporting.Api.DTOs;

public class SubmissionDto
{
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
}
```

- [ ] **Step 2: Rewrite `Services/SubmissionService.cs`**

```csharp
using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public class SubmissionService : ISubmissionService
{
    private readonly AppDbContext _db;
    public SubmissionService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateSubmissionAsync(SubmissionDto dto)
    {
        var s = new Data.Entities.Submission
        {
            Id = Guid.NewGuid(),
            EntityId = dto.EntityId,
            CycleId = dto.CycleId,
            Status = "in_progress"
        };
        _db.Submissions.Add(s);
        await _db.SaveChangesAsync();
        return s.Id;
    }
}
```

- [ ] **Step 3: Update `Services/ISubmissionService.cs` return type to match**

```csharp
using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface ISubmissionService
{
    Task<Guid> CreateSubmissionAsync(SubmissionDto dto);
}
```

- [ ] **Step 4: Build to confirm the whole project compiles now**

Run: `dotnet build DsacReporting.Api.csproj --configuration Release`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Services/SubmissionService.cs Services/ISubmissionService.cs DTOs/SubmissionDto.cs
git commit -m "fix: update SubmissionService and SubmissionDto for Guid-keyed schema"
```

---

### Task 4: Add password_hash migration and seed demo users

**Files:**
- Create: `Data/Database/002_add_password_hash.sql`
- Create: `Data/Database/003_seed_demo_users.sql`
- Modify: `Program.cs`

The seed script needs a precomputed PBKDF2 hash for the shared dev password
`Password123!`, produced by ASP.NET Core Identity's `PasswordHasher<T>` (the same hasher
`AuthController` will use to verify it in Task 6 — hashing and verification must use the
same algorithm or login will never succeed). We generate this hash via a throwaway console
snippet, not by hand.

- [ ] **Step 1: Generate the password hash to embed in the seed SQL**

Run this from the project root (uses the `Microsoft.Extensions.Identity.Core` hasher that
Task 5 will add as a package reference — add it now so this snippet compiles):

```bash
dotnet add package Microsoft.Extensions.Identity.Core --version 8.0.0
```

Then create a scratch file `/tmp/hash.csx`-equivalent — since `dotnet script` isn't
guaranteed installed, instead add a temporary throwaway top-level statement:

Create `HashGen/HashGen.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.0" />
  </ItemGroup>
</Project>
```

Create `HashGen/Program.cs`:
```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), "Password123!");
Console.WriteLine(hash);
```

Run: `dotnet run --project HashGen`
Expected: prints a base64 hash string starting with `AQAAAAI...` — copy this exact output
for Step 3.

Delete the scratch project afterward: `rm -rf HashGen` (it must not be committed).

- [ ] **Step 2: Create `Data/Database/002_add_password_hash.sql`**

```sql
-- Adds password storage to users, needed for real login.
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash TEXT;
```

- [ ] **Step 3: Create `Data/Database/003_seed_demo_users.sql`**

Replace `<HASH_FROM_STEP_1>` with the exact output from Step 1 (do not alter it — PBKDF2
hashes are sensitive to any character change):

```sql
-- Seeds one demo entity and the three demo users referenced in init.sql's comments.
-- All three share the dev password "Password123!".
INSERT INTO entities (id, name, type)
VALUES ('11111111-1111-1111-1111-111111111111', 'Test Public Entity', 'public_entity')
ON CONFLICT (id) DO NOTHING;

INSERT INTO users (id, entity_id, full_name, email, role, password_hash)
VALUES
  ('22222222-2222-2222-2222-222222222222',
   '11111111-1111-1111-1111-111111111111',
   'Thandi', 'thandi@example.com', 'entity_officer',
   '<HASH_FROM_STEP_1>'),
  ('33333333-3333-3333-3333-333333333333',
   NULL,
   'Sipho', 'sipho@example.com', 'dsac_me',
   '<HASH_FROM_STEP_1>'),
  ('44444444-4444-4444-4444-444444444444',
   NULL,
   'DSAC Exec', 'exec@example.com', 'dsac_exec',
   '<HASH_FROM_STEP_1>')
ON CONFLICT (email) DO NOTHING;
```

- [ ] **Step 4: Update `Program.cs` to apply both new SQL files when `APPLY_INIT_SQL=true`**

Find this block near the end of `Program.cs`:

```csharp
if (Environment.GetEnvironmentVariable("APPLY_INIT_SQL") == "true")
{
    var sqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Database", "init.sql");
    if (!File.Exists(sqlPath))
    {
        sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", "init.sql");
    }

    if (File.Exists(sqlPath))
    {
        Console.WriteLine($"Applying init SQL from {sqlPath}");
        await DsacReporting.Api.Data.Database.DatabaseInitializer.ApplyInitSqlAsync(app.Services, sqlPath);
        Console.WriteLine("Init SQL applied.");
    }
    else
    {
        Console.WriteLine("init.sql not found; skipping apply.");
    }
}
```

Replace it with:

```csharp
if (Environment.GetEnvironmentVariable("APPLY_INIT_SQL") == "true")
{
    var sqlFiles = new[] { "init.sql", "002_add_password_hash.sql", "003_seed_demo_users.sql" };
    foreach (var fileName in sqlFiles)
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Database", fileName);
        if (!File.Exists(sqlPath))
        {
            sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", fileName);
        }

        if (File.Exists(sqlPath))
        {
            Console.WriteLine($"Applying SQL from {sqlPath}");
            await DsacReporting.Api.Data.Database.DatabaseInitializer.ApplyInitSqlAsync(app.Services, sqlPath);
            Console.WriteLine($"{fileName} applied.");
        }
        else
        {
            Console.WriteLine($"{fileName} not found; skipping.");
        }
    }
}
```

- [ ] **Step 5: Ensure the new SQL files are copied to the build output (same as init.sql already is)**

Check whether `DsacReporting.Api.csproj` has an explicit `<None Update="Data/Database/init.sql">`
entry. Run:

Run: `grep -n "init.sql" DsacReporting.Api.csproj`

If there's no match, no action is needed — the SDK-style project already includes all
files under the project directory in output by default for `.sql` via content copy only
if configured; verify by building and checking output:

Run: `dotnet build DsacReporting.Api.csproj --configuration Release && ls bin/Release/net8.0/Data/Database/`
Expected output lists `init.sql`, `002_add_password_hash.sql`, `003_seed_demo_users.sql`,
`DatabaseInitializer.cs` is not listed (it's compiled, not copied). If the `.sql` files are
missing from that directory listing, add this to `DsacReporting.Api.csproj` inside a new
`<ItemGroup>`:

```xml
<ItemGroup>
  <None Include="Data/Database/*.sql" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Then re-run the build command above and confirm the `.sql` files now appear in
`bin/Release/net8.0/Data/Database/`.

- [ ] **Step 6: Commit**

```bash
git add Data/Database/002_add_password_hash.sql Data/Database/003_seed_demo_users.sql Program.cs DsacReporting.Api.csproj
git commit -m "feat: add password_hash migration and seed 3 demo users"
```

---

### Task 5: Build `IJwtTokenGenerator` service

**Files:**
- Create: `Services/IJwtTokenGenerator.cs`
- Create: `Services/JwtTokenGenerator.cs`
- Modify: `DsacReporting.Api.csproj`
- Modify: `Program.cs`

- [ ] **Step 1: Add the JWT-writing package reference**

`Microsoft.AspNetCore.Authentication.JwtBearer` (already referenced) pulls in
`Microsoft.IdentityModel.Tokens` for validation, but writing tokens needs
`System.IdentityModel.Tokens.Jwt`. Add it explicitly:

```bash
dotnet add DsacReporting.Api.csproj package System.IdentityModel.Tokens.Jwt --version 7.5.1
```

Also add the password hasher package (needed by this task's tests and Task 6):

```bash
dotnet add DsacReporting.Api.csproj package Microsoft.Extensions.Identity.Core --version 8.0.0
```

- [ ] **Step 2: Create `Services/IJwtTokenGenerator.cs`**

```csharp
using DsacReporting.Api.Data.Entities;

namespace DsacReporting.Api.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user);
}
```

- [ ] **Step 3: Create `Services/JwtTokenGenerator.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DsacReporting.Api.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        if (user.EntityId.HasValue)
        {
            claims.Add(new Claim("entity_id", user.EntityId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }
}
```

- [ ] **Step 4: Register the service in `Program.cs`**

Find this line in `Program.cs`:

```csharp
builder.Services.AddScoped<IDashboardService, DashboardService>();
```

Add immediately after it:

```csharp
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
```

Add the using statement at the top of `Program.cs` if not already covered by the existing
`using DsacReporting.Api.Services;` line (it already is — no change needed there).

- [ ] **Step 5: Build**

Run: `dotnet build DsacReporting.Api.csproj --configuration Release`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add Services/IJwtTokenGenerator.cs Services/JwtTokenGenerator.cs Program.cs DsacReporting.Api.csproj
git commit -m "feat: add JwtTokenGenerator service"
```

---

### Task 6: Create the test project and write JwtTokenGenerator + PasswordHasher tests

**Files:**
- Create: `test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj`
- Create: `test/DsacReporting.Api.Tests/JwtTokenGeneratorTests.cs`
- Create: `test/DsacReporting.Api.Tests/PasswordHasherTests.cs`
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Create the test project**

```bash
mkdir -p test/DsacReporting.Api.Tests
cd test/DsacReporting.Api.Tests
dotnet new xunit -n DsacReporting.Api.Tests -o .
cd ../..
```

- [ ] **Step 2: Add project reference and packages to the test csproj**

```bash
dotnet add test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj reference DsacReporting.Api.csproj
dotnet add test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
dotnet add test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj package Microsoft.Extensions.Identity.Core --version 8.0.0
```

- [ ] **Step 3: Write the failing test — `test/DsacReporting.Api.Tests/JwtTokenGeneratorTests.cs`**

Delete the default `UnitTest1.cs` xUnit scaffolds first:

```bash
rm -f test/DsacReporting.Api.Tests/UnitTest1.cs
```

```csharp
using System.IdentityModel.Tokens.Jwt;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace DsacReporting.Api.Tests;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(string secret = "test-secret-at-least-32-chars-long!!") =>
        new(Options.Create(new JwtSettings { Secret = secret }));

    [Fact]
    public void GenerateToken_IncludesRoleAndEntityIdClaims()
    {
        var generator = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            Role = "entity_officer",
            EntityId = Guid.NewGuid(),
            FullName = "Thandi",
            PasswordHash = "irrelevant-for-this-test"
        };

        var (token, expiresAt) = generator.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == "email").Value);
        Assert.Equal(user.Role, jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
        Assert.Equal(user.EntityId.ToString(), jwt.Claims.First(c => c.Type == "entity_id").Value);
        Assert.True(expiresAt > DateTimeOffset.UtcNow.AddHours(7));
        Assert.True(expiresAt <= DateTimeOffset.UtcNow.AddHours(8).AddMinutes(1));
    }

    [Fact]
    public void GenerateToken_OmitsEntityIdClaim_WhenUserHasNoEntity()
    {
        var generator = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sipho@example.com",
            Role = "dsac_me",
            EntityId = null,
            FullName = "Sipho",
            PasswordHash = "irrelevant-for-this-test"
        };

        var (token, _) = generator.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "entity_id");
    }
}
```

- [ ] **Step 4: Run the test to verify it passes (implementation already exists from Task 5)**

Run: `dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj --filter JwtTokenGeneratorTests`
Expected: `Passed! - Failed: 0, Passed: 2`

- [ ] **Step 5: Write `test/DsacReporting.Api.Tests/PasswordHasherTests.cs`**

```csharp
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace DsacReporting.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void VerifyHashedPassword_Succeeds_ForCorrectPassword()
    {
        var hasher = new PasswordHasher<object>();
        var target = new object();
        var hash = hasher.HashPassword(target, "Password123!");

        var result = hasher.VerifyHashedPassword(target, hash, "Password123!");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_Fails_ForWrongPassword()
    {
        var hasher = new PasswordHasher<object>();
        var target = new object();
        var hash = hasher.HashPassword(target, "Password123!");

        var result = hasher.VerifyHashedPassword(target, hash, "WrongPassword!");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
```

- [ ] **Step 6: Run all tests so far**

Run: `dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 7: Fix CI to find the `test/` directory**

Read `.github/workflows/ci.yml`. Find:

```yaml
    - name: Run tests
      run: |
        if [ -d "tests" ]; then dotnet test --no-build; else echo "No tests"; fi
```

Replace with:

```yaml
    - name: Run tests
      run: |
        if [ -d "test" ]; then dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj; else echo "No tests"; fi
```

Note this runs `dotnet test` without `--no-build` since the test project wasn't built by
the earlier `dotnet build DsacReporting.Api.csproj` step (that only builds the API project,
not the test project) — `dotnet test` will restore and build it itself.

- [ ] **Step 8: Commit**

```bash
git add test/ .github/workflows/ci.yml
git commit -m "test: add JwtTokenGenerator and PasswordHasher unit tests"
```

---

### Task 7: Implement real `AuthController.Login`

**Files:**
- Create: `DTOs/LoginRequestDto.cs`
- Create: `DTOs/LoginResponseDto.cs`
- Modify: `Controllers/AuthController.cs`
- Create: `test/DsacReporting.Api.Tests/AuthControllerTests.cs`

- [ ] **Step 1: Create `DTOs/LoginRequestDto.cs`**

```csharp
namespace DsacReporting.Api.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create `DTOs/LoginResponseDto.cs`**

```csharp
namespace DsacReporting.Api.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
```

- [ ] **Step 3: Write the failing test — `test/DsacReporting.Api.Tests/AuthControllerTests.cs`**

```csharp
using DsacReporting.Api.Auth;
using DsacReporting.Api.Controllers;
using DsacReporting.Api.Data;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace DsacReporting.Api.Tests;

public class AuthControllerTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateController(AppDbContext db)
    {
        var jwtGenerator = new JwtTokenGenerator(
            Options.Create(new JwtSettings { Secret = "test-secret-at-least-32-chars-long!!" }));
        return new AuthController(db, jwtGenerator);
    }

    [Fact]
    public async Task Login_ReturnsToken_ForCorrectCredentials()
    {
        using var db = CreateDb(nameof(Login_ReturnsToken_ForCorrectCredentials));
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            FullName = "Thandi",
            Role = "entity_officer",
            EntityId = Guid.NewGuid()
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Login(new LoginRequestDto { Email = "thandi@example.com", Password = "Password123!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.Token));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        using var db = CreateDb(nameof(Login_ReturnsUnauthorized_ForWrongPassword));
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            FullName = "Thandi",
            Role = "entity_officer"
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Login(new LoginRequestDto { Email = "thandi@example.com", Password = "WrongPassword!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForUnknownEmail()
    {
        using var db = CreateDb(nameof(Login_ReturnsUnauthorized_ForUnknownEmail));
        var controller = CreateController(db);

        var result = await controller.Login(new LoginRequestDto { Email = "nobody@example.com", Password = "Password123!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
```

- [ ] **Step 4: Run the test to verify it fails to compile (AuthController constructor doesn't match yet)**

Run: `dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj --filter AuthControllerTests`
Expected: FAIL — build error, `AuthController` has no constructor taking `(AppDbContext, IJwtTokenGenerator)`.

- [ ] **Step 5: Rewrite `Controllers/AuthController.cs`**

```csharp
using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private static readonly PasswordHasher<User> Hasher = new();

    public AuthController(AppDbContext db, IJwtTokenGenerator jwtGenerator)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var verifyResult = Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var (token, expiresAt) = _jwtGenerator.GenerateToken(user);
        return Ok(new LoginResponseDto { Token = token, ExpiresAt = expiresAt });
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj --filter AuthControllerTests`
Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 7: Run the full test suite**

Run: `dotnet test test/DsacReporting.Api.Tests/DsacReporting.Api.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 7`

- [ ] **Step 8: Build the main project to confirm nothing else broke**

Run: `dotnet build DsacReporting.Api.csproj --configuration Release`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 9: Commit**

```bash
git add DTOs/LoginRequestDto.cs DTOs/LoginResponseDto.cs Controllers/AuthController.cs test/DsacReporting.Api.Tests/AuthControllerTests.cs
git commit -m "feat: implement real email/password login with JWT issuance"
```

---

### Task 8: Deploy and manually verify against Railway

**Files:** none (deployment + manual verification only)

- [ ] **Step 1: Push to origin/main**

```bash
git push origin master:main
```

- [ ] **Step 2: In the Railway dashboard, set `APPLY_INIT_SQL=true` on the `sac-api-1` service variables**

This triggers a redeploy. Watch the Deployments tab logs for:
```
Applying SQL from .../init.sql
init.sql applied.
Applying SQL from .../002_add_password_hash.sql
002_add_password_hash.sql applied.
Applying SQL from .../003_seed_demo_users.sql
003_seed_demo_users.sql applied.
```

- [ ] **Step 3: Remove `APPLY_INIT_SQL` from Railway variables once confirmed, triggering another redeploy**

- [ ] **Step 4: Verify login via Swagger (`ENABLE_SWAGGER=true` should already be set from prior work)**

Visit `https://<your-domain>/swagger`, expand `POST /api/auth/login`, try it with:

```json
{ "email": "thandi@example.com", "password": "Password123!" }
```

Expected: `200 OK` with a JSON body containing a `token` field (a long JWT string, not
`"fake-token"`) and an `expiresAt` timestamp roughly 8 hours out.

Then try:

```json
{ "email": "thandi@example.com", "password": "WrongPassword!" }
```

Expected: `401 Unauthorized` with `{"error": "Invalid email or password."}`.

- [ ] **Step 5: Decode the returned token to sanity-check claims**

Paste the token string from Step 4 into any local JWT decoder, or run:

```bash
python3 -c "
import base64, json, sys
token = sys.argv[1]
payload = token.split('.')[1]
payload += '=' * (-len(payload) % 4)
print(json.dumps(json.loads(base64.urlsafe_b64decode(payload)), indent=2))
" "<paste token here>"
```

Expected: JSON showing `role: entity_officer`, `entity_id: 11111111-...`, an `email`
claim, and an `exp` timestamp ~8 hours in the future.

---

## Notes on out-of-scope items (do not implement in this plan)

- `SubmissionsController`, `EntityController`, `DsacController`, `DocumentsController`,
  `CommentsController`, `TasksController` remain stub returns.
- `AuditLoggingMiddleware` remains a no-op.
- No refresh tokens, no password reset, no login rate limiting.
