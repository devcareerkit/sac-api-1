# Sipho's KPI-setting & APP approval workflow — design

## Problem

Right now the API has a cycle-scoped, self-reported KPI flow (`kpi_targets`,
`submissions`, `submission_values` — entities report actuals against
DSAC-defined per-cycle targets). This spec adds a *separate*, higher-level
workflow sitting on top of it:

1. Sipho (DSAC M&E) sets **5-year KPIs** for every one of the 32 entities
   (26 Public Entities + 6 NPOs), using a form builder (requirements change
   over time, so the field set must be data-driven, not hardcoded).
2. Once sent, the entity (Thandi) uploads their **Annual Performance Plan
   (APP)** as a PDF. This just stores the file — no AI runs yet.
3. Sipho opens the submission and explicitly triggers AI extraction (an
   "Analyze" action, not automatic on upload). AI reads the PDF, extracts
   the Output Indicators it contains (matching the real APP structure
   examined from `NAC-2025-26-Annual-Performance-Plan.pdf` — see below), and
   tries to match each one to one of Sipho's KPIs by name.
4. Sipho reviews the AI's summary + extracted indicators (draft rows
   created at analysis time) and approves or rejects the APP.
5. On approval, the (kept) indicators become trackable **tasks** on that
   entity's dashboard, each broken into 4 quarterly **mini-tasks**. Thandi
   uploads proof of completion per quarter; that rolls up to task and
   portfolio-wide completion, shown as three status cards for Thandi and a
   double bar chart (% complete vs. % remaining) across all 32 entities for
   Sipho.

## What the real APP document looks like

`NAC-2025-26-Annual-Performance-Plan.pdf` (National Arts Council, 48 pages)
has no flat task list. Its actual structure:

- **Part C — Measuring Our Performance**: per-programme tables of
  *Outcome → Output → Output Indicator*, each with audited-performance
  history, then annual targets for the 5-year MTEF window
  (2025/26–2027/28+), then a second table splitting each indicator's annual
  target across 4 quarters (e.g. "Number of capacity development
  programmes/workshops implemented by NAC" — annual target 8, split 2/2/2/2).
- **Part D — Technical Indicator Descriptions**: one block per indicator
  with Definition, Source of Data, Method of Calculation, Means of
  Verification, Calculation Type, Reporting Cycle, and Responsible Unit.

This confirms: **a "task" is an Output Indicator**, not a separate
free-text action item. There is no clean extraction target better than the
indicator table + its Part D description.

## Scope

**In scope:**
1. Form-builder-driven 5-year KPI definitions, set per entity by Sipho.
2. APP PDF upload (reuses the existing SharePoint document storage from the
   earlier document-upload work).
3. AI extraction (Claude, PDF document input + structured JSON output) —
   summary + a list of indicator-shaped items, each with a best-effort match
   to one of the entity's KPIs.
4. Sipho approve/reject workflow on the APP submission: analysis creates
   draft indicator rows (matched + unmatched), Sipho can discard unwanted
   ones before approving, then approve finalizes the kept rows and creates
   their 4 quarterly mini-tasks each. All of this happens on the one
   submission-review screen, not a separate triage screen, to keep this
   pass bounded.
6. Thandi's dashboard: 3 status cards (not started / in progress /
   completed), counting **main tasks** (not quarterly mini-tasks).
7. Thandi's paginated task table: main tasks as rows, each expandable to
   its 4 quarterly mini-tasks; clicking a mini-task opens a detail view to
   upload proof (file + notes).
8. Proof upload marks that mini-task `completed` immediately (no separate
   Sipho re-approval step for proofs, per your call) — the parent task
   auto-rolls-up to `in_progress` (≥1 quarter done) or `completed` (all 4
   done).
9. Sipho's portfolio view: a double bar chart, one pair of bars per entity
   (32 total), red = % of tasks not yet completed, green = % completed.

**Out of scope (explicitly, for this pass):**
- Editing/versioning a KPI after it's been sent (a new form submission
  creates new KPI rows; changing a sent KPI is a future pass).
- A dedicated "unmatched indicators" triage screen — handled inline in the
  approval action for now.
- Any UI for building the form schema itself beyond a minimal schema
  editor (see Form builder section) — reusing the *shape* proven in
  `pfa-ui`'s `FieldEditor.tsx`/`schema.ts`, not its MUI implementation
  (that app is shadcn/Tailwind; SAC.Powerverse uses shadcn/Tailwind too,
  so the concept ports, the component doesn't).
- Re-running AI extraction automatically on re-upload after a rejection —
  each new upload creates a new `app_submissions` row and re-runs
  extraction; no special "diff against previous attempt" logic.

## Data model

### `kpi_form_schemas` — Sipho's form builder definitions

```sql
CREATE TABLE kpi_form_schemas (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  schema_json JSONB NOT NULL,   -- FormSchema shape, see below
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  is_active BOOLEAN NOT NULL DEFAULT true
);
```

`schema_json` follows the field-definition shape already proven in
`pfa-ui/lib/types/schema.ts` (adapted, domain-specific bits removed):

```ts
type FieldType = 'text' | 'number' | 'select' | 'textarea'
interface FieldOption { value: string; label: string }
interface FieldDef {
  id: string; type: FieldType; label: string
  helperText?: string; options?: FieldOption[]
  validation: { required?: boolean; min?: number; max?: number }
}
interface KpiFormSchema {
  name: string
  fields: FieldDef[]   // no multi-step needed for a single KPI entry
}
```

A submitted KPI form's answers are stored as-is in `entity_kpis.form_values`
(JSONB) so schema changes over time never break historical records — only
`kpi_name`, `unit`, and `five_year_target` are pulled out as first-class
columns because the rest of the system (matching, task creation) needs to
query them directly.

### `entity_kpis` — Sipho's 5-year KPIs, one row per entity per indicator

```sql
CREATE TABLE entity_kpis (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id),
  form_schema_id UUID REFERENCES kpi_form_schemas(id),
  kpi_name TEXT NOT NULL,
  unit TEXT,
  five_year_target NUMERIC,
  form_values JSONB NOT NULL DEFAULT '{}',
  status TEXT NOT NULL DEFAULT 'draft'
    CHECK (status IN ('draft', 'sent', 'received')),
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  sent_at TIMESTAMPTZ,
  received_at TIMESTAMPTZ,
  UNIQUE (entity_id, kpi_name)
);
```

`status` lifecycle: `draft` (Sipho is still editing) → `sent` (entity can
see it) → `received` (an approved APP has matched a task to it — set
automatically on APP approval, not manually).

### `app_submissions` — the PDF upload + AI processing + approval lifecycle

```sql
CREATE TABLE app_submissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id),
  file_url TEXT NOT NULL,
  uploaded_by UUID REFERENCES users(id),
  uploaded_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  status TEXT NOT NULL DEFAULT 'pending_review'
    CHECK (status IN ('pending_review', 'ai_failed', 'ai_processed', 'approved', 'rejected')),
  ai_summary TEXT,
  ai_processed_at TIMESTAMPTZ,
  ai_processed_by UUID REFERENCES users(id),
  reviewed_by UUID REFERENCES users(id),
  reviewed_at TIMESTAMPTZ,
  rejection_reason TEXT
);
```

One entity can have many `app_submissions` over time (rejections cause
re-uploads — each is a new row, not an edit). Only one row per entity may
be in a non-terminal state (`pending_review`/`ai_failed`/`ai_processed`) at
a time — enforced in the service layer, not a DB constraint, to keep this
pass simple.

Status lifecycle: `pending_review` (uploaded, Sipho hasn't analyzed it yet)
→ `ai_processed` (Sipho clicked "Analyze", extraction succeeded) or
`ai_failed` (Sipho clicked "Analyze", extraction errored — Sipho can retry,
which re-runs analysis on the same row rather than requiring a re-upload)
→ `approved` / `rejected` (Sipho's decision, only reachable from
`ai_processed`).

### `app_indicators` — extracted Output Indicators (the "tasks")

Named `app_indicators`, not `app_tasks`, to avoid colliding with the
existing `tasks` table (free-text task assignment between users — a
different, already-shipped concept).

```sql
CREATE TABLE app_indicators (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  app_submission_id UUID NOT NULL REFERENCES app_submissions(id) ON DELETE CASCADE,
  entity_id UUID NOT NULL REFERENCES entities(id),
  entity_kpi_id UUID REFERENCES entity_kpis(id),  -- null until matched/confirmed
  name TEXT NOT NULL,
  annual_target NUMERIC,
  unit TEXT,
  match_confidence TEXT CHECK (match_confidence IN ('matched', 'unmatched', 'manual')),
  is_approved BOOLEAN NOT NULL DEFAULT false,
  status TEXT NOT NULL DEFAULT 'not_started'
    CHECK (status IN ('not_started', 'in_progress', 'completed')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Rows are created by `analyze` (one per extracted indicator, `is_approved =
false`) so Sipho's review screen has something concrete to show and
edit/discard before committing. `approve` sets `is_approved = true` on the
rows Sipho keeps (deleting any Sipho discarded) and creates their 4
`app_indicator_quarters` each. A row only counts toward dashboards/task
tables once `is_approved = true` — `GET` endpoints for tasks/dashboards
always filter on it.

`match_confidence`: `matched` (AI found a confident name match to an
`entity_kpis` row), `unmatched` (AI extracted it but found no KPI match —
Sipho decides at approval time whether to keep it, still creating the row
but leaving `entity_kpi_id` null), `manual` (Sipho manually linked/renamed
during approval review).

### `app_indicator_quarters` — the 4 mini-tasks per indicator

```sql
CREATE TABLE app_indicator_quarters (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  app_indicator_id UUID NOT NULL REFERENCES app_indicators(id) ON DELETE CASCADE,
  quarter SMALLINT NOT NULL CHECK (quarter BETWEEN 1 AND 4),
  quarter_target NUMERIC,
  status TEXT NOT NULL DEFAULT 'not_started'
    CHECK (status IN ('not_started', 'completed')),
  proof_file_url TEXT,
  proof_notes TEXT,
  completed_by UUID REFERENCES users(id),
  completed_at TIMESTAMPTZ,
  UNIQUE (app_indicator_id, quarter)
);
```

No `in_progress` state at the mini-task level — a quarter's proof is
either uploaded (`completed`) or not (`not_started`); "in progress" only
exists as a *derived* state at the parent indicator level (some but not
all of its 4 quarters completed).

### Status roll-up rule (service-layer logic, not a trigger)

On every mini-task completion:
- `app_indicators.status` = `completed` if all 4 `app_indicator_quarters`
  rows are `completed`; else `in_progress` if ≥1 is `completed`; else
  `not_started`.
- `entity_kpis.status` flips to `received` the moment its matched
  `app_indicators` row is created (i.e. right after the APP that matched it
  is approved) — independent of quarterly completion.

## AI extraction

**Model:** `claude-opus-5`, non-beta `client.Messages.Create`, PDF passed as
`DocumentBlockParam` with `Base64PdfSource` (APP PDFs run ~20MB+ in
practice — comfortably under the 32MB base64 request limit, but the backend
must reject anything over that before calling the API, with a clear error,
rather than letting the call fail opaquely). Structured output
(`OutputConfig.Format` → `JsonOutputFormat`) constrains the response to:

```json
{
  "summary": "string — 2-4 sentence plain-language summary of the APP",
  "indicators": [
    {
      "name": "string — the Output Indicator's exact or near-exact wording",
      "annual_target": "number | null",
      "unit": "string | null",
      "quarterly_targets": [1, 2, 3, 4] // number|null, length 4, Q1..Q4
    }
  ]
}
```

**Matching**: after extraction, the service does a case-insensitive
containment/similarity match between each extracted `indicators[].name` and
the entity's `entity_kpis.kpi_name` rows (simple string similarity — e.g.
normalized Levenshtein ratio above a threshold — no embedding call needed
for this volume). A confident match sets `entity_kpi_id` +
`match_confidence='matched'`; no match sets `match_confidence='unmatched'`.

**Trigger**: extraction is NOT run on upload. It runs only when Sipho calls
`POST /api/app-submissions/{id}/analyze` (see API surface below) — an
explicit "Analyze" action while reviewing a submission. This means the
10-30s Claude round-trip happens on Sipho's own request, not Thandi's
upload, so there's no need for background job infrastructure in this pass:
a synchronous request/response for `analyze` is fine.

**Failure handling**: if the PDF exceeds the size limit, if Claude returns
a non-`end_turn` stop reason, or the call throws, `app_submissions.status`
is set to `ai_failed` with the summary field holding a short diagnostic —
Sipho sees this state explicitly (distinct from `ai_processed`) and can
retry the same `analyze` call again rather than the row getting stuck.

**Config**: `Anthropic__ApiKey` env var, same "not configured yet" pattern
as SharePoint — the `analyze` endpoint (not `upload`, since upload never
touches Claude) returns `503` with a clear message until it's set, rather
than failing confusingly deep in the pipeline.

## API surface (new endpoints)

All under existing `[Authorize]` + role-scoping conventions
(`entity_officer` scoped to own entity via `User.GetEntityId()`,
`dsac_me`/`dsac_exec` via `IsDsacStaff()`).

- `GET /api/kpi-forms` — list form schemas (DSAC staff only)
- `POST /api/kpi-forms` — create a form schema (DSAC staff only)
- `GET /api/entities/{id}/kpis` — list an entity's KPIs (own entity or DSAC staff)
- `POST /api/entities/{id}/kpis` — Sipho submits the KPI form for an entity → creates `entity_kpis` rows, `status='draft'`
- `POST /api/entities/{id}/kpis/send` — flips all `draft` KPIs for that entity to `sent` (DSAC staff only)
- `POST /api/app-submissions/upload` — Thandi uploads the APP PDF (multipart, reuses `IDocumentStorageService`); creates `app_submissions` row with `status='pending_review'`. No AI runs here.
- `GET /api/app-submissions/{id}` — submission detail: status, AI summary (once analyzed), extracted indicators with match state (own entity or DSAC staff)
- `POST /api/app-submissions/{id}/analyze` — DSAC staff only; runs AI extraction against the stored PDF, sets `status='ai_processed'` (or `'ai_failed'`) + `ai_summary` + creates the extracted-indicator preview (held in-memory in the response and persisted as draft `app_indicators` rows so `approve` doesn't have to re-run extraction — see note below)
- `POST /api/app-submissions/{id}/approve` — DSAC staff only; requires `status='ai_processed'`; finalizes the draft `app_indicators` rows created by `analyze` (creates their `app_indicator_quarters`), flips `entity_kpis.status` to `received` for matched ones
- `POST /api/app-submissions/{id}/reject` — DSAC staff only; takes a `reason`, sets `status='rejected'`
- `GET /api/entities/{id}/indicators?page=&pageSize=` — paginated main-task table (own entity or DSAC staff)
- `GET /api/indicators/{id}` — one indicator + its 4 quarters (detail view)
- `POST /api/indicators/{id}/quarters/{quarter}/proof` — upload proof (multipart file + notes); marks that quarter `completed`, rolls up parent status
- `GET /api/dsac/indicator-summary` — the double-bar-chart data: one row per entity with `{ entityId, entityName, percentComplete, percentRemaining }` (DSAC staff only)

## Frontend

New route segments under the existing `(dsac)` and `(entity)` groups:
- `(dsac)/kpis/[entitySlug]` — Sipho's KPI form (reuses the shadcn form
  patterns already in `KpiSubmissionForm.tsx`, driven by `schema_json`
  instead of a hardcoded field list — this is the "form builder" in
  practice: a schema-driven renderer, not a drag-and-drop UI builder,
  matching your "changing requirements" need without over-building an
  admin authoring UI in this pass. `docs/superpowers/plans` for this spec
  should call out `POST /api/kpi-forms` as JSON-authored by Sipho's team
  for now — not a WYSIWYG form editor, deliberately, to keep scope bounded).
- `(dsac)/app-submissions` — review queue: list + detail (AI summary,
  extracted indicators with match state, approve/reject actions).
- `(dsac)/portfolio` gets a new double bar chart section, fed by
  `GET /api/dsac/indicator-summary`.
- `(entity)/kpis` — Thandi's read-only view of received KPIs.
- `(entity)/app-submission` — upload APP PDF, see status/rejection reason,
  re-upload on rejection.
- `(entity)/indicators` — paginated main-task table with expandable
  quarterly rows; clicking a quarter row opens `(entity)/indicators/[id]/quarters/[q]`
  for proof upload.
- `(entity)/dashboard` gains the 3 status cards (not started / in progress
  / completed, counting main `app_indicators`).

## Testing

- Backend: unit tests for the status roll-up logic (quarter completion →
  indicator status transitions), the name-matching function (exact match,
  fuzzy match above/below threshold, no match), and the approval endpoint's
  row-creation logic (matched vs. unmatched indicators). AI extraction
  itself is not unit-tested against the live API (non-deterministic,
  costs money) — instead the extraction *parsing* (raw Claude JSON response
  → `app_indicators` rows) is tested with a fixed canned response.
- Manual: one full pass with the real NAC PDF once `Anthropic__ApiKey` is
  set, to sanity-check real extraction quality before relying on it for
  all 32 entities.

## Non-goals / explicit deferrals

- No retry/backoff logic around the Claude call beyond what the SDK does
  by default.
- No notification system (email/in-app) for "KPI sent" / "APP rejected" —
  status is visible on next page load, not pushed.
- No versioning of `kpi_form_schemas` beyond `is_active` — editing a
  schema in place is acceptable for this pass since historical answers are
  preserved in `entity_kpis.form_values` regardless.
