# Task Manager

A small task management app: a .NET 9 Web API (EF Core Code First, SQL Server) and a React + Vite UI.

> Status: **step 9 of 11**: the API is complete. The UI has its scaffold, API layer, and a task list with status
> and priority filters. Detail, create/edit, delete and summary screens come in the next steps.

## Running the UI against the API
Use two terminals:
```powershell
# 1. API (repo root)
dotnet run --project src/TaskManager.Api --launch-profile http      # http://localhost:5157

# 2. UI
cd src/task-manager-ui
npm ci                                                              # first time / after pulling
npm run dev                                                         # http://localhost:5173
```
- The UI calls the API directly at `VITE_API_BASE_URL` (`src/task-manager-ui/.env.development`, default
  `http://localhost:5157`). There is no Vite proxy. The API's CORS policy allows `http://localhost:5173`, and the
  dev server is pinned to that port (`strictPort`), so it fails rather than silently moving to a port CORS would reject.
- To point the UI at a different API, create `src/task-manager-ui/.env.development.local` (gitignored) containing
  `VITE_API_BASE_URL=http://host:port`. If the UI must run on another origin, add it to the API's `Cors:AllowedOrigins`.
- UI commands: `npm test` (Vitest, single run), `npm run test:watch`, `npm run build` (output in `dist/`), `npm run preview`.

### UI structure
```
src/task-manager-ui/src/
  api/          httpClient.js (the only fetch; parses ProblemDetails into ApiError), tasksApi.js, config.js, ApiError.js
  hooks/        useTasks.js (loads the list, re-fetches on filter change, aborts stale requests)
  components/   TaskFilters, TaskList, TaskRow, PriorityBadge, StatusBadge, EmptyState, ErrorBanner (one per file, own CSS)
  constants/    taskOptions.js (status/priority values, labels, icons)
```
- **Errors:** every failure reaches components as an `ApiError` with `title`, `detail`, camelCase `fieldErrors`
  (`$.priority` is normalised to `priority`), `traceId`, and a `kind` of `http`, `network` (API not running or
  blocked by CORS) or `config`.
- **Priority** is shown with colour plus an icon and a text label (▲▲ Critical, ▲ High, ● Medium, ▼ Low), so it
  never relies on colour alone.
- **Versions** are pinned exactly (`.npmrc` has `save-exact=true`, and `package-lock.json` is committed), using
  releases that were at least two weeks old when added.

## Prerequisites
- .NET SDK 9.0.x (`global.json` pins to the 9.0 line and rolls forward to the latest installed 9.0 feature band)
- SQL Server Express at `localhost\SQLEXPRESS` (from step 2 onwards)
- Node.js 22.12+ for the UI (developed on Node 26; `package.json` declares `"engines": { "node": ">=22.12" }`)

## Layout
```
src/TaskManager.Api/      Web API (Controllers, Domain, Infrastructure, ...)
tests/TaskManager.Tests/  xUnit tests (Unit/ and Integration/)
src/task-manager-ui/      React + Vite UI (JavaScript, plain CSS)
```
NuGet versions are managed centrally in `Directory.Packages.props`. All Microsoft.* framework packages are pinned to 9.0.x.

## Build, test, run
```powershell
dotnet build
dotnet test                                                      # needs SQL Server; see "Tests" below
dotnet ef database update --project src/TaskManager.Api          # create/upgrade TaskManagerDb
dotnet run --project src/TaskManager.Api --launch-profile http   # http://localhost:5157
```
The `dotnet ef` tool must be 9.0.x (`dotnet tool install --global dotnet-ef --version 9.0.10`).
Migrations live in `src/TaskManager.Api/Data/Migrations`. The app never migrates on startup.

## Data model
Table `dbo.Tasks` (entity `TaskItem`):

| Column | Type | Notes |
|--------|------|-------|
| Id | int identity | PK |
| Title | nvarchar(200) | required |
| Description | nvarchar(2000) | optional |
| Status | nvarchar(20) | `Todo`, `InProgress`, `Done`; enforced by `CK_Tasks_Status` |
| Priority | nvarchar(20) | `Low`, `Medium`, `High`, `Critical`; enforced by `CK_Tasks_Priority` |
| AssignedTo | nvarchar(100) | optional |
| CreatedDate / ModifiedDate | datetimeoffset | UTC |
| IsDeleted | bit | soft delete; EF global query filter hides deleted rows |
| RowVersion | rowversion | optimistic concurrency (a conflicting save returns 409) |

Index `IX_Tasks_IsDeleted_Status_Priority` supports filtering and the summary query.
Enums are stored as strings, so sorting by priority uses an explicit rank, never the column value.

## Seed data
The `SeedTasks` migration inserts 15 sample tasks (`src/TaskManager.Api/Data/SeedData.cs`, via `HasData`):
- Ids 1–12 are active and cover every status × priority combination exactly once.
- Ids 13–15 are soft-deleted, so they're hidden by the API and excluded from the summary.
- AssignedTo varies across Alice, Bob, Chen, Dana and Eve, and some tasks are unassigned.

Ids and dates are fixed literals, so the seed is identical everywhere. Because it is part of a migration,
**it is applied to every database the migrations run against**. To change the seed, edit `SeedData.cs`
and add a new migration. Never edit an applied migration.

## Tests
Integration tests use a separate database, **`TaskManagerDb_Tests`** on `localhost\SQLEXPRESS` (Windows auth).
The test run creates and migrates it automatically, and Respawn empties every table (except
`__EFMigrationsHistory`) before each database test. As a safeguard, tests refuse to run against any database
whose name doesn't end in `_Tests`. To use a different server, set:
```powershell
$env:TASKMANAGER_TEST_CONNECTION = "Server=MYPC\SQLEXPRESS;Database=TaskManagerDb_Tests;Trusted_Connection=True;TrustServerCertificate=True"
```
`src/TaskManager.Api/TaskManager.Api.http` has ready-made requests (VS / VS Code REST Client / Rider).

## Endpoints (so far)
| Method | Path | Success | Errors |
|--------|------|---------|--------|
| GET | `/api/tasks?status=&priority=` | 200 + array of tasks (`[]` when nothing matches) | 400 invalid filter value |
| GET | `/api/tasks/summary` | 200 + `[{ status, priority, count }]` (`[]` when no active tasks) | |
| POST | `/api/tasks` | 201 + `Location: /api/tasks/{id}` + task | 400 validation / bad JSON, 415 non-JSON body |
| GET | `/api/tasks/{id}` | 200 + task | 404 unknown, soft-deleted, or invalid id (0, negative, non-numeric) |
| PUT | `/api/tasks/{id}` | 200 + updated task (new `rowVersion`) | 400 validation / bad rowVersion, 404, **409 stale rowVersion** |
| PATCH | `/api/tasks/{id}/status` | 200 + task (new `rowVersion`) | 400 invalid status, 404 |
| DELETE | `/api/tasks/{id}` | 204, no body | 404 unknown, already deleted, or invalid id |
| GET | `/health` | 200 "Healthy" | |
| GET | `/openapi/v1.json` | OpenAPI document (Development only) | |

**List**: `GET /api/tasks?status=InProgress&priority=High`
- Both filters are optional and case-insensitive. An empty value (`?status=`) means no filter.
- Invalid values return 400 with the allowed values, e.g. `errors.status: ["Status must be one of: Todo, InProgress, Done."]`.
- Sort order: priority **Critical, High, Medium, Low**, then newest `createdDate` first, then id.
  Enums are stored as strings, so the sort uses an explicit priority rank (a SQL `CASE`), never the column value
  (which would sort alphabetically). See `Services/TaskQueries.cs`.
- Soft-deleted tasks are never returned. There's no paging, since the dataset is small.

**Create request**:
```json
{ "title": "Write release notes", "description": "optional", "status": "Todo", "priority": "High", "assignedTo": "Alice" }
```
| Field | Rules |
|-------|-------|
| `title` | required, not blank, max 200 characters; trimmed |
| `description` | optional, max 2000 characters; blank becomes null |
| `status` | optional, `Todo` / `InProgress` / `Done` (case-insensitive); defaults to `Todo` |
| `priority` | **required**, `Low` / `Medium` / `High` / `Critical` (case-insensitive); numbers rejected |
| `assignedTo` | optional, max 100 characters; blank becomes null |

Any other fields (`id`, `isDeleted`, `createdDate`, …) are ignored.

**Task response** (POST and GET return the same shape):
```json
{
  "id": 16, "title": "Write release notes", "description": "optional", "status": "Todo", "priority": "High",
  "assignedTo": "Alice", "createdDate": "2026-11-20T10:00:00+00:00", "modifiedDate": "2026-11-20T10:00:00+00:00",
  "rowVersion": "AAAAAAAAB9E="
}
```
A soft-deleted task is indistinguishable from one that never existed: both return 404.

**Update (PUT `/api/tasks/{id}`)**: a full edit, using optimistic concurrency.
```json
{ "title": "…", "description": "…", "status": "InProgress", "priority": "High", "assignedTo": "Bob", "rowVersion": "AAAAAAAAB9E=" }
```
- `title`, `status`, `priority` and `rowVersion` are required. It's a full replacement, so an omitted `description`
  or `assignedTo` is **cleared**.
- `rowVersion` must be the value from the last response for this task. If the task changed since, the response is
  **409 Conflict** and nothing is saved. Reload and retry.
- A missing `rowVersion` returns 400 `errors.rowVersion` ("RowVersion is required."). A malformed or wrong-length one
  returns 400 ("RowVersion must be the base64 value returned by the API.").
- `id`, `createdDate`, `modifiedDate` and `isDeleted` can't be changed, and are ignored if sent.

**Change status (PATCH `/api/tasks/{id}/status`)**: body `{ "status": "Done" }`.
- Last write wins, so no `rowVersion` is needed. Setting the status the task already has changes nothing.
- The response carries the task's **new** `rowVersion`. Clients must keep it, because a later PUT with the old one gets 409.

**Summary (GET `/api/tasks/summary`)**: counts of active tasks grouped by status and priority.
```json
[ { "status": "Todo", "priority": "High", "count": 2 }, { "status": "Done", "priority": "Critical", "count": 3 } ]
```
- Only combinations that have tasks are returned. Soft-deleted tasks are not counted.
- Order: status Todo, InProgress, Done, then priority Critical, High, Medium, Low.
- Implemented with **raw SQL via plain ADO.NET** (`Services/TaskSummaryService.cs`), not EF Core. It uses the same
  `ConnectionStrings:DefaultConnection`. The SQL is a constant with no user input:
  ```sql
  SELECT [Status], [Priority], COUNT(*) AS [Count]
  FROM [dbo].[Tasks]
  WHERE [IsDeleted] = 0
  GROUP BY [Status], [Priority]
  ORDER BY
      CASE [Status] WHEN N'Todo' THEN 1 WHEN N'InProgress' THEN 2 WHEN N'Done' THEN 3 ELSE 4 END,
      CASE [Priority] WHEN N'Critical' THEN 1 WHEN N'High' THEN 2 WHEN N'Medium' THEN 3 WHEN N'Low' THEN 4 ELSE 5 END;
  ```
  `WHERE [IsDeleted] = 0` is required because EF's soft-delete query filter doesn't apply to raw SQL. The CASE
  ranks are needed because the enums are stored as strings, which would otherwise sort alphabetically. Any future
  filter must be added as a typed `SqlParameter`, never by concatenating text.

**Delete (DELETE `/api/tasks/{id}`)**: a soft delete. The row is kept with `IsDeleted = 1` and from then on is
hidden from every endpoint (GET, list, PUT, PATCH, summary). Deleting again returns 404. Last write wins, so no
`rowVersion` is needed. There is no `DeletedDate` column: `modifiedDate` records the deletion time, because a
deleted task can't be edited afterwards. To restore a task, set `IsDeleted = 0` in the database.

**Dates:** `modifiedDate` is set to the current UTC time on every write (including delete). `createdDate` never changes.

## Error responses
Every error is an RFC 7807 `application/problem+json` body with the same shape:
```json
{ "type": "...", "title": "Not Found", "status": 404, "detail": "...", "instance": "GET /nope", "traceId": "00-..." }
```
- Validation errors (400) add an `errors` object keyed by field, named exactly like the JSON body or query
  parameter (camelCase: `title`, `priority`, `assignedTo`, `status`), with messages such as
  `"Priority must be one of: Low, Medium, High, Critical."`. Malformed JSON and wrong JSON types (e.g. a number
  for `priority`) are reported under a JSON-path key such as `$.priority` with the generic message
  `"The input was not valid."`. Parser and type details are never returned.
- Unhandled exceptions return a generic 500. The exception message and stack trace are logged server-side
  only and never returned. Quote `traceId` to find the log entry.
- Unknown routes (404) and wrong methods (405) use the same shape.
- Concurrency conflicts (someone else saved the task first) return 409 Conflict.

## Configuration
| Key | Where | Notes |
|-----|-------|-------|
| `Cors:AllowedOrigins` | `appsettings.Development.json` (`http://localhost:5173`) | Exact origins only, no wildcards and no trailing slash. **Required outside Development**: the API refuses to start without it. |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` (`localhost\SQLEXPRESS`, `TaskManagerDb`, Windows auth) | **Required**: the API refuses to start without it. |

**Using a different SQL Server on another machine:** edit `ConnectionStrings:DefaultConnection` in
`src/TaskManager.Api/appsettings.Development.json`, for example `Server=MYPC\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True`.
If you use SQL authentication, keep the password out of source. Use user-secrets instead:
```powershell
dotnet user-secrets init --project src/TaskManager.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;User Id=...;Password=...;TrustServerCertificate=True" --project src/TaskManager.Api
```

To override settings outside Development, use environment variables (`__` as the separator). Don't edit
`appsettings.json`:
```powershell
$env:Cors__AllowedOrigins__0 = "https://tasks.example.com"
```
Secrets never go in source. Use `dotnet user-secrets` locally or environment variables when deployed.

## Logging
Built-in `ILogger` structured logging. Each request is logged once (method, path, status, duration).
Bodies and headers are not logged. Unhandled exceptions are logged by `GlobalExceptionHandler` with the
`traceId` returned to the client.
