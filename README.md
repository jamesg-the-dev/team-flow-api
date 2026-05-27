# TeamFlow API

ASP.NET Core 10 backend for the TeamFlow SaaS (workspaces, projects, kanban tasks,
channels, attachments, notifications, activity feed). Built on Clean Architecture +
DDD + CQRS + MediatR + EF Core (Npgsql) and authenticated via Supabase JWT (HS256).

## Solution layout

```
TeamFlow.sln
├── src/
│   ├── TeamFlow.Domain         ← pure C#, no framework refs; aggregates, VOs, events
│   ├── TeamFlow.Application    ← CQRS handlers, DTOs, validators, pipeline behaviors
│   ├── TeamFlow.Infrastructure ← EF Core DbContext, configs, repos, interceptors
│   └── TeamFlow.Api            ← controllers, Supabase auth, Swagger, middleware
└── schema.sql                  ← Postgres source-of-truth schema
```

Dependency direction is strictly inward:

```
Api → Application → Domain
Api → Infrastructure → Application → Domain
```

## Architectural decisions

| Concern | Decision |
|---|---|
| Architecture style | Clean Architecture + DDD aggregates |
| Cross-cutting | MediatR pipeline behaviors (Logging → Validation → UnitOfWork) |
| Result handling | `Result` / `Result<T>` + `Error` (no exceptions for control flow) |
| IDs | `Guid` v7 (`Guid.CreateVersion7()`) — sortable, index-friendly |
| Domain events | Collected on aggregates; dispatched **after** `SaveChanges` via `SaveChangesInterceptor` → MediatR `IPublisher` |
| Auditing | `AuditingInterceptor` stamps `IAuditable` on add/modify using `ICurrentUser` + `IDateTimeProvider` |
| Validation | FluentValidation; `ValidationBehavior` returns `Result.Failure` when `TResponse : Result`, otherwise throws |
| Enums in PG | `HasPostgresEnum<T>` for all 9 domain enums (matches `schema.sql`) |
| Naming | `UseSnakeCaseNamingConvention()` so EF maps `WorkspaceId → workspace_id` |
| Full-text search | `tsvector` generated columns (tasks, messages) + GIN indexes |
| Kanban ordering | Fractional `NUMERIC(20,10)` `position`; midpoint insertion via `GetNeighbourPositionAsync` (no global reorder) |
| Authentication | Supabase HS256 JWT (`Supabase:JwtSecret`); `NameClaimType = "sub"` → `Guid` user id |
| Authorization | Policies: `authenticated`, `workspace-member` (extend per-tenant) |
| Errors → HTTP | `ExceptionHandlingMiddleware` maps `DomainException → 409`, `ValidationException → 400`, `UnauthorizedAccessException → 401` |
| Repository per aggregate | `IWorkspaceRepository`, `IProjectRepository`, `ITaskRepository`, … |
| Unit of work | `IUnitOfWork` (thin wrapper over `DbContext.SaveChangesAsync`) committed by `UnitOfWorkBehavior` on `ICommand[<T>]` |
| Query side | Bypasses repos for read-only projections via `IListProjectsQueryService`, `IGetProjectBoardQueryService` (CQRS read model) |
| Value objects | `Money(AmountCents, Currency)` mapped with `OwnsOne` |
| Soft delete | `ISoftDeletable` + `deleted_at` (extend with global query filters as needed) |

## Configuration

`appsettings.json` (use User Secrets in dev for the JWT secret + DB password):

```jsonc
{
  "ConnectionStrings": {
    "Default": "Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<password>;SslMode=Require;Trust Server Certificate=true"
  },
  "Supabase": {
    "Url": "https://<project-ref>.supabase.co",
    "JwtSecret": "<copy from Supabase dashboard → Settings → API → JWT Secret>"
  },
  "Cors": { "AllowedOrigins": [ "http://localhost:5173" ] }
}
```

```powershell
dotnet user-secrets --project src/TeamFlow.Api set "Supabase:JwtSecret" "<secret>"
dotnet user-secrets --project src/TeamFlow.Api set "ConnectionStrings:Default" "Host=…"
```

## Build & run

```powershell
dotnet build TeamFlow.sln
dotnet run --project src/TeamFlow.Api
```

Swagger: <https://localhost:7211/swagger>. Health: `/health`.

## EF Core migrations

```powershell
dotnet ef migrations add InitialCreate `
    --project src/TeamFlow.Infrastructure `
    --startup-project src/TeamFlow.Api `
    --output-dir Persistence/Migrations
dotnet ef database update --project src/TeamFlow.Infrastructure --startup-project src/TeamFlow.Api
```

> **Note:** On corporate Windows machines, AppLocker / WDAC policies sometimes
> block the `dotnet-ef` tool from loading DLLs that live under `%USERPROFILE%\Documents`,
> producing `FileLoadException: Access is denied` on `TeamFlow.Infrastructure.dll`.
> The fix is to move the repository to a non-controlled path (e.g. `C:\dev\team-flow-api`)
> before running `dotnet ef migrations add`. The schema itself can also be applied
> directly from `schema.sql` against the Supabase database — EF will then reverse-
> engineer cleanly from that baseline.

## What's implemented

Domain (all 8 aggregates from `schema.sql`):

- **Workspaces** — members, invites, tags, ownership transfer
- **Projects** — members, status, budget (`Money` VO), task-number counter
- **Tasks** — kanban column + fractional position, assignment, comments, watchers, dependencies, full-text
- **Channels / Messages** — threaded discussions, reactions, mentions, full-text
- **Attachments** — polymorphic owner kind + id
- **Notifications** — typed notification + delivery preferences
- **Activity feed** — append-only event log with JSONB metadata

Application (vertical slices):

- **Projects**: `CreateProject`, `UpdateProject`, `ChangeProjectStatus`, `AddProjectMember`, `GetProjectById`, `ListProjects`
- **Tasks**: `CreateTask`, `MoveTask`, `AssignTask`, `AddTaskComment`, `GetTaskById`, `GetProjectBoard`

Infrastructure: `TeamFlowDbContext`, all entity configurations, 8 repositories, 2 query
services, `AuditingInterceptor`, `DomainEventDispatchInterceptor`, `UnitOfWork`,
`SystemDateTimeProvider`, `MediatrDomainEventDispatcher`.

API: Supabase JWT auth, `ProjectsController`, `TasksController`, exception → ProblemDetails
middleware, Swagger with Bearer, CORS, Npgsql health check.

## Extending — adding a new feature slice

1. Add aggregate behavior + events to the relevant **Domain** namespace.
2. Add `Features/<Aggregate>/Commands|Queries/<Name>/<Name>Command.cs` (+ validator + handler) in **Application**.
3. If a query needs a projection, add an interface under `Application/Features/.../Queries/.../I<Name>QueryService.cs` and implement in **Infrastructure/Persistence/QueryServices**.
4. Add a controller endpoint in **Api/Controllers** that calls `_mediator.Send(...)` and `result.ToActionResult()`.

The same pattern can be followed to flesh out Workspaces, Channels/Messages, Notifications,
Attachments and Activity — domain + repositories already exist.
