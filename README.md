# Bergwacht Alarm Service

This repository contains the AlarmService which integrates with the Bergwacht Companion backend and displays the dashboard while reacting to alarms.

## Secret management (no secrets in appsettings)

To avoid committing secrets, the following sensitive settings have been moved out of the appsettings files:
- BackendBWBCompanionSettings.AccessToken
- BackendBWBCompanionSettings.DashboardToken

The application now loads configuration from (in order of precedence):
1. Environment variables
2. secrets.local.json (if present, not committed)
3. appsettings.{Environment}.json
4. appsettings.json

### Local development

Option A — secrets.local.json (recommended for local runs):
1. Copy `src/AlarmService/secrets.local.sample.json` to `src/AlarmService/secrets.local.json`.
2. Fill in your real values for:
   - `BackendBWBCompanionSettings:AccessToken`
   - `BackendBWBCompanionSettings:DashboardToken` (URL-encoded string, as previously used in appsettings.Production.json)
3. Run the service as usual. The app will automatically pick up the values.

Option B — .NET User Secrets (no files on disk in repo):
1. From the `src/AlarmService` directory, initialize user-secrets if not done yet:
   - `dotnet user-secrets init`
2. Set secrets:
   - `dotnet user-secrets set "BackendBWBCompanionSettings:AccessToken" "<your-token>"`
   - `dotnet user-secrets set "BackendBWBCompanionSettings:DashboardToken" "<your-encoded-dashboard-token>"`

Option C — Environment variables:
- `BackendBWBCompanionSettings__AccessToken`
- `BackendBWBCompanionSettings__DashboardToken`

Note: Environment variables take precedence over user-secrets and secrets.local.json; secrets.local.json takes precedence over appsettings.*.json.

### Production / Deployment

Provide the secrets via environment variables or mount a `secrets.local.json` next to the application binaries. Environment variables take precedence over the file. The base URL stays in appsettings.*.json and is not considered secret.

## Notes

- Do not commit `secrets.local.json`. It is git-ignored by default.
- The configuration binding remains the same (`CompanionSettings`), so no code changes are required in consumers.
