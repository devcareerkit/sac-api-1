-- Removes duplicate `entities` rows (same name, different id) that resulted
-- from a double import. For each duplicate name group, keeps the OLDEST row
-- (by created_at, then id as a tiebreaker) and reassigns every foreign key
-- referencing the newer duplicate(s) onto the kept row before deleting them.
--
-- Idempotent: running this again when no duplicates remain is a no-op.

DO $$
DECLARE
  dup RECORD;
  keeper UUID;
  loser UUID;
BEGIN
  FOR dup IN
    SELECT name
    FROM entities
    GROUP BY name
    HAVING COUNT(*) > 1
  LOOP
    -- Row to keep: oldest created_at, id as a stable tiebreaker.
    SELECT id INTO keeper
    FROM entities
    WHERE name = dup.name
    ORDER BY created_at ASC, id ASC
    LIMIT 1;

    FOR loser IN
      SELECT id FROM entities WHERE name = dup.name AND id <> keeper
    LOOP
      -- users.entity_id has no uniqueness constraint - plain reassignment.
      UPDATE users SET entity_id = keeper WHERE entity_id = loser;

      -- kpi_targets has UNIQUE(entity_id, cycle_id, kpi_name) - reassign only
      -- rows that wouldn't collide with a row the keeper already has; drop
      -- any that would collide (the keeper's row is treated as authoritative).
      UPDATE kpi_targets kt SET entity_id = keeper
      WHERE kt.entity_id = loser
        AND NOT EXISTS (
          SELECT 1 FROM kpi_targets k2
          WHERE k2.entity_id = keeper AND k2.cycle_id = kt.cycle_id AND k2.kpi_name = kt.kpi_name
        );
      DELETE FROM kpi_targets WHERE entity_id = loser;

      -- submissions has UNIQUE(entity_id, cycle_id) - same pattern.
      UPDATE submissions s SET entity_id = keeper
      WHERE s.entity_id = loser
        AND NOT EXISTS (
          SELECT 1 FROM submissions s2 WHERE s2.entity_id = keeper AND s2.cycle_id = s.cycle_id
        );
      DELETE FROM submissions WHERE entity_id = loser;

      -- documents, risk_scores: risk_scores has UNIQUE(entity_id, cycle_id) too.
      UPDATE documents SET entity_id = keeper WHERE entity_id = loser;

      UPDATE risk_scores r SET entity_id = keeper
      WHERE r.entity_id = loser
        AND NOT EXISTS (
          SELECT 1 FROM risk_scores r2 WHERE r2.entity_id = keeper AND r2.cycle_id = r.cycle_id
        );
      DELETE FROM risk_scores WHERE entity_id = loser;

      -- entity_kpis has UNIQUE(entity_id, kpi_name).
      UPDATE entity_kpis ek SET entity_id = keeper
      WHERE ek.entity_id = loser
        AND NOT EXISTS (
          SELECT 1 FROM entity_kpis e2 WHERE e2.entity_id = keeper AND e2.kpi_name = ek.kpi_name
        );
      DELETE FROM entity_kpis WHERE entity_id = loser;

      -- app_submissions, app_indicators: no uniqueness constraint on entity_id.
      UPDATE app_submissions SET entity_id = keeper WHERE entity_id = loser;
      UPDATE app_indicators SET entity_id = keeper WHERE entity_id = loser;

      -- Now safe to remove the duplicate entity row.
      DELETE FROM entities WHERE id = loser;
    END LOOP;
  END LOOP;
END $$;
