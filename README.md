# DsacReporting.Api

API for DSAC reporting. To connect and auto-deploy to Railway:

- Create a GitHub repository and push this project.
- In Railway, connect the GitHub repository or use a GitHub Action to run the `railway` CLI.

To run locally with Railway `DATABASE_URL` and apply init SQL:

```powershell
$env:DATABASE_URL='postgresql://user:password@host:port/dbname'
$env:APPLY_INIT_SQL='true'
dotnet run --project DsacReporting.Api
```

CI: a build workflow is provided in `.github/workflows/ci.yml`.
