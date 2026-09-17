-- Initialization SQL for DsacReporting Api
-- Run this on Railway Postgres (ensure pgcrypto extension exists)

-- Extension needed for gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Entities: the 26 Public Entities + 6 NPOs
CREATE TABLE IF NOT EXISTS entities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  type TEXT NOT NULL CHECK (type IN ('public_entity','npo')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Users: Thandi (entity_officer), Sipho (dsac_me), DSAC Exec (dsac_exec)
CREATE TABLE IF NOT EXISTS users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID REFERENCES entities(id),   -- null for DSAC staff
  full_name TEXT NOT NULL,
  email TEXT UNIQUE NOT NULL,
  role TEXT NOT NULL CHECK (role IN ('entity_officer','dsac_me','dsac_exec')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Reporting cycles, e.g. "Q3 2026"
CREATE TABLE IF NOT EXISTS reporting_cycles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  label TEXT NOT NULL,
  due_date DATE NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Fixed KPI schema per entity per cycle -- this is the structured-metadata fix
CREATE TABLE IF NOT EXISTS kpi_targets (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id),
  cycle_id UUID NOT NULL REFERENCES reporting_cycles(id),
  kpi_name TEXT NOT NULL,          -- e.g. "Job creation", "Audit finding closure"
  target_value NUMERIC,
  unit TEXT,
  UNIQUE (entity_id, cycle_id, kpi_name)
);

-- One submission per entity per cycle
CREATE TABLE IF NOT EXISTS submissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id),
  cycle_id UUID NOT NULL REFERENCES reporting_cycles(id),
  submitted_by UUID REFERENCES users(id),
  status TEXT NOT NULL DEFAULT 'not_started'
    CHECK (status IN ('not_started','in_progress','submitted','missed')),
  submitted_at TIMESTAMPTZ,
  UNIQUE (entity_id, cycle_id)
);

-- The actual reported figures against each KPI -- what feeds the dashboard
CREATE TABLE IF NOT EXISTS submission_values (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  submission_id UUID NOT NULL REFERENCES submissions(id) ON DELETE CASCADE,
  kpi_target_id UUID NOT NULL REFERENCES kpi_targets(id),
  actual_value NUMERIC,
  notes TEXT
);

-- Document repository, auto-tagged against the KPI it satisfies
CREATE TABLE IF NOT EXISTS documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  submission_id UUID REFERENCES submissions(id) ON DELETE CASCADE,
  entity_id UUID NOT NULL REFERENCES entities(id),
  doc_type TEXT NOT NULL CHECK (doc_type IN
    ('strategic_plan','app','operational_plan','annual_report','quarterly_report','financials')),
  file_url TEXT NOT NULL,
  version INT NOT NULL DEFAULT 1,
  kpi_tag_id UUID REFERENCES kpi_targets(id),
  uploaded_by UUID REFERENCES users(id),
  uploaded_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Real-time comments, visible on a document or a whole submission
CREATE TABLE IF NOT EXISTS comments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID REFERENCES documents(id) ON DELETE CASCADE,
  submission_id UUID REFERENCES submissions(id) ON DELETE CASCADE,
  author_id UUID NOT NULL REFERENCES users(id),
  body TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Task assignment up/down the chain
CREATE TABLE IF NOT EXISTS tasks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  submission_id UUID REFERENCES submissions(id) ON DELETE CASCADE,
  assigned_by UUID REFERENCES users(id),
  assigned_to UUID REFERENCES users(id),
  description TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'open' CHECK (status IN ('open','done')),
  due_date DATE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Risk score per entity per cycle -- drives the early-warning badges
CREATE TABLE IF NOT EXISTS risk_scores (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id),
  cycle_id UUID NOT NULL REFERENCES reporting_cycles(id),
  score NUMERIC NOT NULL,     -- 0-100
  reason TEXT,
  computed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (entity_id, cycle_id)
);

-- Nudges to the entity, escalations to Sipho
CREATE TABLE IF NOT EXISTS notifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  submission_id UUID REFERENCES submissions(id) ON DELETE CASCADE,
  recipient_id UUID NOT NULL REFERENCES users(id),
  type TEXT NOT NULL CHECK (type IN ('nudge','escalation')),
  channel TEXT DEFAULT 'email',
  sent_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Who, when, what -- the security slide made real
CREATE TABLE IF NOT EXISTS audit_log (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  actor_id UUID REFERENCES users(id),
  action TEXT NOT NULL,
  target_table TEXT NOT NULL,
  target_id UUID,
  metadata JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Indexes for the queries the dashboard actually runs
CREATE INDEX IF NOT EXISTS idx_submissions_entity_cycle ON submissions(entity_id, cycle_id);
CREATE INDEX IF NOT EXISTS idx_submission_values_submission ON submission_values(submission_id);
CREATE INDEX IF NOT EXISTS idx_documents_entity ON documents(entity_id);
CREATE INDEX IF NOT EXISTS idx_risk_scores_cycle ON risk_scores(cycle_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_target ON audit_log(target_table, target_id);
