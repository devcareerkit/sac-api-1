# Foundation fix + real login — design

## Problem

DsacReporting.Api was scaffolded with two things that don't agree with each other:

- `Data/Database/init.sql` — a real Postgres schema (UUID keys, roles, KPI targets,
  reporting cycles, documents, comments, tasks, risk scores, notifications, audit log).
- `Data/Entities/*.cs` — thin EF Core classes with `int` keys and only a few fields each,
  which do not map onto that schema (e.g. `Submission` has no `CycleId`, `Status`; `User`
  has no `Role`, `EntityId`, or password field at all).

Every controller is a hardcoded stub that ignores the database entirely, including
`AuthController.Login`, which accepts any body and always returns `{ "token": "fake-token" }`.

This pass fixes the model/schema mismatch and makes login real. Other controllers stay
stubbed and are explicitly out of scope for this pass.

## Scope

**In scope:**
1. Rewrite `Data/Entities/*.cs` to match `init.sql` (UUID keys, all columns, two new
   entities: `ReportingCycle`, `KpiTarget`).
2. Add a `password_hash` column to `users` via a new migration SQL file.
3. Seed 3 demo users (Thandi/entity_officer, Sipho/dsac_me, DSAC Exec/dsac_exec) with a
   shared known dev password, hashed.
4. Real `POST /api/auth/login`: verifies email + password against the database, issues a
   signed JWT with `sub`, `email`, role claim, and `entity_id` claim (when present).
5. A test project covering password hashing/verification and login success/failure.

**Out of scope (future pass):** `SubmissionsController`, `EntityController`,
`DsacController`, `DocumentsController`, `CommentsController`, `TasksController` remain
stubs. `AuditLoggingMiddleware` stays a no-op placeholder.

## Data model changes

### Entities to rewrite (UUID keys, matching `init.sql` columns 1:1)

- `Entity` — `Id (Guid)`, `Name`, `Type` (`public_entity`|`npo`), `CreatedAt`
- `User` — `Id (Guid)`, `EntityId (Guid?)`, `FullName`, `Email`, `Role`
  (`entity_officer`|`dsac_me`|`dsac_exec`), `PasswordHash`, `CreatedAt`
- `ReportingCycle` (new) — `Id (Guid)`, `Label`, `DueDate`, `CreatedAt`
- `KpiTarget` (new) — `Id (Guid)`, `EntityId`, `CycleId`, `KpiName`, `TargetValue`, `Unit`
- `Submission` — `Id (Guid)`, `EntityId`, `CycleId`, `SubmittedBy (Guid?)`, `Status`
  (`not_started`|`in_progress`|`submitted`|`missed`), `SubmittedAt (DateTimeOffset?)`
- `SubmissionValue` — `Id (Guid)`, `SubmissionId`, `KpiTargetId`, `ActualValue`, `Notes`
- `DocumentRecord` — `Id (Guid)`, `SubmissionId (Guid?)`, `EntityId`, `DocType`, `FileUrl`,
  `Version`, `KpiTagId (Guid?)`, `UploadedBy (Guid?)`, `UploadedAt`
- `Comment` — `Id (Guid)`, `DocumentId (Guid?)`, `SubmissionId (Guid?)`, `AuthorId`, `Body`,
  `CreatedAt`
- `TaskItem` — `Id (Guid)`, `SubmissionId (Guid?)`, `AssignedBy (Guid?)`, `AssignedTo (Guid?)`,
  `Description`, `Status` (`open`|`done`), `DueDate (DateOnly?)`, `CreatedAt`
- `RiskScore` — `Id (Guid)`, `EntityId`, `CycleId`, `Score`, `Reason`, `ComputedAt`
- `Notification` — `Id (Guid)`, `SubmissionId (Guid?)`, `RecipientId`, `Type`
  (`nudge`|`escalation`), `Channel`, `SentAt`
- `AuditLogEntry` — `Id (Guid)`, `ActorId (Guid?)`, `Action`, `TargetTable`, `TargetId (Guid?)`,
  `Metadata (string?, JSON)`, `CreatedAt`

`CHECK`-constrained columns (`type`, `role`, `status`, `doc_type`, `notification.type`) are
modeled as plain `string` properties on the C# side — no enum mapping in this pass, since
Postgres already enforces the valid set and adding EF enum conversions is extra ceremony
this pass doesn't need.

`AppDbContext.OnModelCreating` gets explicit table name mappings (snake_case tables/columns
via `ToTable`/`HasColumnName`, since the DB uses `snake_case` and C# uses `PascalCase`) and
default value generation left to Postgres (`gen_random_uuid()`, `now()`).

### New migration: `Data/Database/002_add_password_hash.sql`

```sql
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash TEXT;
```

Applied the same way as `init.sql` — via `APPLY_INIT_SQL=true` triggering
`DatabaseInitializer`, which will be extended to run both files in order (idempotent, so
safe to leave on across a deploy if needed, though the existing guidance to unset it after
a successful run still applies).

### Seed data (appended to init SQL / a new seed script)

One entity (`Test Public Entity`, type `public_entity`), then three users:

| Name | Email | Role | Entity |
|---|---|---|---|
| Thandi | thandi@example.com | entity_officer | Test Public Entity |
| Sipho | sipho@example.com | dsac_me | (none) |
| DSAC Exec | exec@example.com | dsac_exec | (none) |

All three share the dev password `Password123!`, hashed with `PasswordHasher<User>` at
seed-script generation time (i.e. the SQL file contains a precomputed hash, not plaintext).

## Login flow

`POST /api/auth/login`

Request: `LoginRequestDto { string Email, string Password }`

Logic:
1. Look up `User` by `Email` (case-insensitive).
2. If not found, or `PasswordHasher.VerifyHashedPassword` fails →
   `401 Unauthorized`, generic `{ "error": "Invalid email or password." }` (no user
   enumeration — same message either way).
3. On success, build claims: `ClaimTypes.NameIdentifier` (user id), `ClaimTypes.Email`,
   `ClaimTypes.Role` (the role string — this makes `IsInRole` and the existing
   `[Authorize(Policy = ...)]` role policies work unchanged), and a custom `entity_id` claim
   (only added when `EntityId` is not null) — matching what `EntityScopeMiddleware` already
   reads via `user.FindFirst("entity_id")`.
4. Sign a JWT (HMAC-SHA256, `Jwt:Secret` from config/env) with 8-hour expiry, return
   `{ "token": "...", "expiresAt": "..." }`.

`AuthController` gets a real constructor-injected `AppDbContext` and `IOptions<JwtSettings>`,
plus a small `IJwtTokenGenerator` service (in `Services/`) so token creation is unit-testable
without spinning up ASP.NET's full pipeline.

## Testing

New test project: `DsacReporting.Api.Tests` (xUnit, referenced from a new `test/` or
`tests/` folder — matching whatever the sibling `backend/test` convention looks like; if
none applies here, `tests/DsacReporting.Api.Tests` at the repo root).

- `JwtTokenGeneratorTests` — token contains expected claims, correct expiry.
- `PasswordHasherTests` — hash/verify roundtrip, wrong password rejected.
- `AuthControllerTests` (using EF Core's InMemory provider for `AppDbContext`) — valid
  login returns 200 + token; wrong password returns 401; unknown email returns 401 with
  the same message.

CI (`.github/workflows/ci.yml`) already runs `dotnet test` conditionally on a `tests`
directory existing — this will make that branch actually execute.

## Out of scope / explicit non-goals

- No refresh tokens — 8-hour expiry only, user re-logs-in after that.
- No password reset / change-password flow.
- No rate limiting on login (worth flagging for later, not building now).
- Other controllers stay stub-returns; this pass does not touch them.
