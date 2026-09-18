-- Seeds 3 historical reporting cycles (before the existing Q3 2026) plus
-- synthetic submission history for every real entity, so trend/YoY and
-- predictive-risk queries have actual rows to aggregate instead of a single
-- data point. Clearly demo/seed data - not real DSAC reporting history.
--
-- Pattern (deterministic, based on each entity's row position so it's
-- reproducible): every 4th entity is "consistently late" (missed/missed/
-- late), every 5th is "improving" (missed/late/submitted), everyone else is
-- "consistently on time" (submitted/submitted/submitted) - giving the
-- predictive risk signal (missed >= 2 of last 3) real entities to flag, and
-- everyone else a normal trend line.

DO $$
DECLARE
  q4_2025 UUID := 'c1000000-0000-0000-0000-000000000001';
  q1_2026 UUID := 'c1000000-0000-0000-0000-000000000002';
  q2_2026 UUID := 'c1000000-0000-0000-0000-000000000003';
  entity_rec RECORD;
  row_num INT := 0;
  pattern TEXT;
BEGIN
  INSERT INTO reporting_cycles (id, label, due_date) VALUES
    (q4_2025, 'Q4 2025', '2025-12-31'),
    (q1_2026, 'Q1 2026', '2026-03-31'),
    (q2_2026, 'Q2 2026', '2026-06-30')
  ON CONFLICT (id) DO NOTHING;

  FOR entity_rec IN SELECT id FROM entities ORDER BY name LOOP
    row_num := row_num + 1;

    IF row_num % 4 = 0 THEN
      pattern := 'consistently_late';
    ELSIF row_num % 5 = 0 THEN
      pattern := 'improving';
    ELSE
      pattern := 'on_time';
    END IF;

    -- Q4 2025
    INSERT INTO submissions (id, entity_id, cycle_id, status, submitted_at)
    SELECT gen_random_uuid(), entity_rec.id, q4_2025,
      CASE pattern
        WHEN 'consistently_late' THEN 'missed'
        WHEN 'improving' THEN 'missed'
        ELSE 'submitted'
      END,
      CASE pattern WHEN 'on_time' THEN '2025-12-28'::timestamptz ELSE NULL END
    WHERE NOT EXISTS (
      SELECT 1 FROM submissions WHERE entity_id = entity_rec.id AND cycle_id = q4_2025
    );

    -- Q1 2026
    INSERT INTO submissions (id, entity_id, cycle_id, status, submitted_at)
    SELECT gen_random_uuid(), entity_rec.id, q1_2026,
      CASE pattern
        WHEN 'consistently_late' THEN 'missed'
        WHEN 'improving' THEN 'missed'
        ELSE 'submitted'
      END,
      CASE pattern WHEN 'on_time' THEN '2026-03-29'::timestamptz ELSE NULL END
    WHERE NOT EXISTS (
      SELECT 1 FROM submissions WHERE entity_id = entity_rec.id AND cycle_id = q1_2026
    );

    -- Q2 2026
    INSERT INTO submissions (id, entity_id, cycle_id, status, submitted_at)
    SELECT gen_random_uuid(), entity_rec.id, q2_2026,
      CASE pattern
        WHEN 'consistently_late' THEN 'missed'
        WHEN 'improving' THEN 'submitted'
        ELSE 'submitted'
      END,
      CASE WHEN pattern IN ('on_time', 'improving') THEN '2026-06-27'::timestamptz ELSE NULL END
    WHERE NOT EXISTS (
      SELECT 1 FROM submissions WHERE entity_id = entity_rec.id AND cycle_id = q2_2026
    );
  END LOOP;
END $$;
