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
   'AQAAAAIAAYagAAAAEPjP5pL7JPlrDV7yfFsJGz7ZZsQCx9Cy1q3z7oSc8cNrB/lf/TwkQiaM2NgEKvDYtg=='),
  ('33333333-3333-3333-3333-333333333333',
   NULL,
   'Sipho', 'sipho@example.com', 'dsac_me',
   'AQAAAAIAAYagAAAAEPjP5pL7JPlrDV7yfFsJGz7ZZsQCx9Cy1q3z7oSc8cNrB/lf/TwkQiaM2NgEKvDYtg=='),
  ('44444444-4444-4444-4444-444444444444',
   NULL,
   'DSAC Exec', 'exec@example.com', 'dsac_exec',
   'AQAAAAIAAYagAAAAEPjP5pL7JPlrDV7yfFsJGz7ZZsQCx9Cy1q3z7oSc8cNrB/lf/TwkQiaM2NgEKvDYtg==')
ON CONFLICT (email) DO NOTHING;
