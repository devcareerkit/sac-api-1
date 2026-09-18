-- Adds a "Budget spent" KPI target so the frontend's submission form
-- (Job Creation + Budget Spent fields) maps onto two real KPI targets.
INSERT INTO kpi_targets (id, entity_id, cycle_id, kpi_name, target_value, unit)
VALUES
  ('88888888-8888-8888-8888-888888888888',
   '68fa5885-993e-4f89-97bb-9db41e9039a5',
   '55555555-5555-5555-5555-555555555555',
   'Budget spent', 1500000, 'ZAR')
ON CONFLICT (entity_id, cycle_id, kpi_name) DO NOTHING;
