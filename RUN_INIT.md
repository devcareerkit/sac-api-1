Run the initialization SQL against your Railway Postgres database.

PowerShell (Windows):

```powershell
$env:DATABASE_URL = 'postgres://user:password@host:5432/dbname'
psql $env:DATABASE_URL -f Data/Database/init.sql
```

Bash (Linux / macOS):

```bash
export DATABASE_URL='postgres://user:password@host:5432/dbname'
psql "$DATABASE_URL" -f Data/Database/init.sql
```

Or run via the app (uses the same connection string the app uses). This will open the app,
apply the script, then continue running. Set the environment variable `APPLY_INIT_SQL=true` before starting.

PowerShell:
```powershell
$env:DATABASE_URL = 'postgres://user:password@host:5432/dbname'
$env:APPLY_INIT_SQL = 'true'
dotnet run --project DsacReporting.Api
```

Note: Railway sets `DATABASE_URL` for you. Ensure the URL includes username and password.
