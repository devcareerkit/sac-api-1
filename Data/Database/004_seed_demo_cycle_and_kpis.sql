-- Re-links Thandi to a real seeded entity and adds a reporting cycle + KPI targets
-- so the submission flow has something real to reference during the demo.
UPDATE users
SET entity_id = '68fa5885-993e-4f89-97bb-9db41e9039a5'
WHERE email = 'thandi@example.com';

INSERT INTO reporting_cycles (id, label, due_date)
VALUES ('55555555-5555-5555-5555-555555555555', 'Q3 2026', '2026-09-30')
ON CONFLICT (id) DO NOTHING;

INSERT INTO kpi_targets (id, entity_id, cycle_id, kpi_name, target_value, unit)
VALUES
  ('66666666-6666-6666-6666-666666666666',
   '68fa5885-993e-4f89-97bb-9db41e9039a5',
   '55555555-5555-5555-5555-555555555555',
   'Job creation', 20, 'jobs'),
  ('77777777-7777-7777-7777-777777777777',
   '68fa5885-993e-4f89-97bb-9db41e9039a5',
   '55555555-5555-5555-5555-555555555555',
   'Audit finding closure', 100, 'percent')
ON CONFLICT (entity_id, cycle_id, kpi_name) DO NOTHING;
