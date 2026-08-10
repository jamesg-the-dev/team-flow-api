# TeamFlow API

## User secrets

```powershell
dotnet user-secrets --project src/TeamFlow.Api set "Supabase:JwtSecret" "<secret>"
dotnet user-secrets --project src/TeamFlow.Api set "ConnectionStrings:Default" "Host=…"
```

## Build & run

```powershell
dotnet build TeamFlow.sln
dotnet run --project src/TeamFlow.Api
```
