-- Interim document storage: files land in Postgres instead of SharePoint
-- until SharePoint credentials are available. Referenced by a synthetic
-- fileUrl of the form db://<id>/<url-encoded-filename>, which
-- DbDocumentStorageService parses back to find the row.

CREATE TABLE IF NOT EXISTS document_blobs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  file_name TEXT NOT NULL,
  content_type TEXT NOT NULL,
  content BYTEA NOT NULL,
  uploaded_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
