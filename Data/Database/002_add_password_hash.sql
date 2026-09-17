-- Adds password storage to users, needed for real login.
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash TEXT;
