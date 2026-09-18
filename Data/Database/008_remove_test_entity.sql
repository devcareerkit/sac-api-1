-- Removes the placeholder "Test Public Entity" seeded before real entity
-- data was loaded. Thandi was re-pointed to the real Amazwi entity in
-- 004_seed_demo_cycle_and_kpis.sql, so nothing should still reference this
-- row - but reassign defensively (same pattern as 007) in case anything does,
-- rather than assuming and risking an orphaned foreign key.

DO $$
DECLARE
  test_entity_id UUID := '11111111-1111-1111-1111-111111111111';
BEGIN
  IF NOT EXISTS (SELECT 1 FROM entities WHERE id = test_entity_id) THEN
    RETURN;
  END IF;

  UPDATE users SET entity_id = NULL WHERE entity_id = test_entity_id;
  DELETE FROM kpi_targets WHERE entity_id = test_entity_id;
  DELETE FROM submissions WHERE entity_id = test_entity_id;
  DELETE FROM documents WHERE entity_id = test_entity_id;
  DELETE FROM risk_scores WHERE entity_id = test_entity_id;
  DELETE FROM entity_kpis WHERE entity_id = test_entity_id;
  DELETE FROM app_submissions WHERE entity_id = test_entity_id;
  DELETE FROM app_indicators WHERE entity_id = test_entity_id;

  DELETE FROM entities WHERE id = test_entity_id;
END $$;
