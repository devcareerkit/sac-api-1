-- Sipho's KPI-setting and APP approval workflow.
-- See docs/superpowers/specs/2026-09-18-app-kpi-workflow-design.md

CREATE TABLE IF NOT EXISTS kpi_form_schemas (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  schema_json JSONB NOT NULL,
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  is_active BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS entity_kpis (
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

CREATE TABLE IF NOT EXISTS app_submissions (
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

CREATE TABLE IF NOT EXISTS app_indicators (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  app_submission_id UUID NOT NULL REFERENCES app_submissions(id) ON DELETE CASCADE,
  entity_id UUID NOT NULL REFERENCES entities(id),
  entity_kpi_id UUID REFERENCES entity_kpis(id),
  name TEXT NOT NULL,
  annual_target NUMERIC,
  unit TEXT,
  match_confidence TEXT CHECK (match_confidence IN ('matched', 'unmatched', 'manual')),
  is_approved BOOLEAN NOT NULL DEFAULT false,
  status TEXT NOT NULL DEFAULT 'not_started'
    CHECK (status IN ('not_started', 'in_progress', 'completed')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS app_indicator_quarters (
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

CREATE INDEX IF NOT EXISTS idx_entity_kpis_entity ON entity_kpis(entity_id);
CREATE INDEX IF NOT EXISTS idx_app_submissions_entity ON app_submissions(entity_id);
CREATE INDEX IF NOT EXISTS idx_app_indicators_submission ON app_indicators(app_submission_id);
CREATE INDEX IF NOT EXISTS idx_app_indicators_entity ON app_indicators(entity_id);
CREATE INDEX IF NOT EXISTS idx_app_indicator_quarters_indicator ON app_indicator_quarters(app_indicator_id);
