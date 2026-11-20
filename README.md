# Task Manager

A small task management app: a **.NET 9 Web API** (EF Core 9 Code First on SQL Server Express) and a
**React + Vite** UI in plain CSS. Users can list tasks (filtered by status and priority), view, create, edit,
change status, and soft-delete them, and see a summary of counts by status and priority. The summary is built
with raw SQL rather than EF.

```
src/TaskManager.Api/      Web API: Controllers, Contracts (DTOs), Domain, Data (EF + migrations), Services, Infrastructure
tests/TaskManager.Tests/  xUnit tests: Unit/ and Integration/ (real SQL Server)
src/task-manager-ui/      React + Vite UI (JavaScript + JSDoc, plain CSS, Vitest)
```

## Prerequisites
| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 9.0.x (`global.json`: 9.0.100, `rollForward: latestFeature`, never moves to 10) | `dotnet --list-sdks` |
| dotnet-ef | 9.0.x | `dotnet ef --version` (install: `dotnet tool install --global dotnet-ef --version 9.0.10`) |
| SQL Server | Express at `localhost\SQLEXPRESS`, Windows authentication | service `MSSQL$SQLEXPRESS` running |
| Node.js | 22.12 or later (developed on 26) | `node --version` |

## Fresh-clone checklist
Run from the repo root unless noted. Each step lists what you should see.

1. **Clone** and `cd` into the repo.
2. **Check prerequisites** (table above). If your SQL Server isn't `localhost\SQLEXPRESS`, see [Configuration](#configuration) first.
3. **Create the database:** `dotnet ef database update --project src/TaskManager.Api`
   → it reports applying `InitialCreate` and `SeedTasks`. `TaskManagerDb` now has 15 seed tasks (12 active, 3 soft-deleted).
4. **Run the API tests:** `dotnet test`
   → `Passed! - Failed: 0, Passed: 193`. This creates and migrates its own `TaskManagerDb_Tests` database.
5. **Start the API:** `dotnet run --project src/TaskManager.Api --launch-profile http`
   → listening on `http://localhost:5157`, and `GET http://localhost:5157/health` returns `Healthy`.
6. **Install the UI** in a second terminal: `cd src/task-manager-ui` then `npm ci`
   → it installs the exact versions from `package-lock.json`, with no warnings about Node engines.
7. **Run the UI tests:** `npm test` → `Test Files 10 passed`, `Tests 82 passed`.
8. **Start the UI:** `npm run dev` → open **http://localhost:5173**.
   → The Summary shows 12 active tasks (one per status × priority), and the list shows 12 tasks with Critical first.
9. **Smoke test in the browser:**
   - create a task: the Summary total becomes 13
   - edit it
   - change a row's status with its dropdown: the Summary counts move
   - delete it, confirming in the dialog: the row disappears and the Summary drops

## Running
Use two terminals:
```powershell
# API (repo root)
dotnet run --project src/TaskManager.Api --launch-profile http      # http://localhost:5157

# UI
cd src/task-manager-ui
npm run dev                                                         # http://localhost:5173
```
- The UI calls the API directly at `VITE_API_BASE_URL`. There is no Vite proxy, so the API's CORS policy is
  exercised in development too.
- The dev server is pinned to port 5173 with `strictPort`. If the port is taken, Vite fails rather than moving to a
  port the API's CORS policy would reject.
- `src/TaskManager.Api/TaskManager.Api.http` has ready-made requests for every endpoint (VS, Rider, VS Code REST Client).
- The OpenAPI document is at `/openapi/v1.json` (Development only).
- Schema changes: add a migration with
  `dotnet ef migrations add <Name> --project src/TaskManager.Api --output-dir Data/Migrations`, then apply it with
  `dotnet ef database update`. The app never migrates on startup.

## Tests
| Suite | Command | Needs | Covers |
|-------|---------|-------|--------|
| API (xUnit) | `dotnet test` (repo root) | SQL Server | unit tests (validation, services with EF InMemory + `FakeTimeProvider`, query building, seed data) and integration tests (`WebApplicationFactory` against a real SQL Server database) |
| UI (Vitest + React Testing Library) | `npm test` in `src/task-manager-ui` (`npm run test:watch` to watch) | nothing (API mocked) | the API layer, components, form validation, modal accessibility, and the create, edit, conflict, status and delete flows |

- Integration tests use a separate database, **`TaskManagerDb_Tests`**. It's created and migrated automatically.
- **Respawn** empties every table except `__EFMigrationsHistory` before each database test.
- As a safeguard, tests refuse to run unless the database name ends in `_Tests`.
- To use another server, set `TASKMANAGER_TEST_CONNECTION` (see below).

## Configuration
### API (`src/TaskManager.Api`)
| Key | Default (Development) | Notes |
|-----|-----------------------|-------|
| `ConnectionStrings:DefaultConnection` | `Server=localhost\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True` in `appsettings.Development.json` | **Required.** The API refuses to start without it. Windows auth, so there's no secret. |
| `Cors:AllowedOrigins` | `["http://localhost:5173"]` in `appsettings.Development.json` | Exact origins only: no wildcards, no trailing slash. **Required outside Development.** |

- **Another SQL Server on a different machine:** edit `ConnectionStrings:DefaultConnection` in `appsettings.Development.json`.
- **SQL authentication:** keep the password out of source and use user-secrets instead:
  ```powershell
  dotnet user-secrets init --project src/TaskManager.Api
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;User Id=...;Password=...;TrustServerCertificate=True" --project src/TaskManager.Api
  ```
- **Outside Development:** use environment variables with `__` as the separator, e.g.
  `$env:ConnectionStrings__DefaultConnection = "..."` and `$env:Cors__AllowedOrigins__0 = "https://tasks.example.com"`.
  `appsettings.json` deliberately contains no connection string.

### UI (`src/task-manager-ui`)
| Variable | Default | Notes |
|----------|---------|-------|
| `VITE_API_BASE_URL` | `http://localhost:5157` in `.env.development` (committed, not a secret) | Override in `.env.development.local` (gitignored). If it's missing, the UI shows a configuration error. |

If the UI has to run on a different origin, add that origin to the API's `Cors:AllowedOrigins`.

### Tests
| Variable | Default | Notes |
|----------|---------|-------|
| `TASKMANAGER_TEST_CONNECTION` | `Server=localhost\SQLEXPRESS;Database=TaskManagerDb_Tests;Trusted_Connection=True;TrustServerCertificate=True` | The database name must end in `_Tests`. |

## API reference
| Method | Path | Success | Errors |
|--------|------|---------|--------|
| GET | `/api/tasks?status=&priority=` | 200 + array of tasks (`[]` when nothing matches) | 400 invalid filter value |
| GET | `/api/tasks/summary` | 200 + `[{ status, priority, count }]` (`[]` when there are no active tasks) | |
| GET | `/api/tasks/{id}` | 200 + task | 404 unknown, soft-deleted, or invalid id (0, negative, non-numeric) |
| POST | `/api/tasks` | 201 + `Location: /api/tasks/{id}` + task | 400 validation or bad JSON, 415 non-JSON body |
| PUT | `/api/tasks/{id}` | 200 + updated task (new `rowVersion`) | 400 validation or bad rowVersion, 404, **409 stale rowVersion** |
| PATCH | `/api/tasks/{id}/status` | 200 + task (new `rowVersion`) | 400 invalid status, 404 |
| DELETE | `/api/tasks/{id}` | 204, no body | 404 unknown or already deleted |
| GET | `/health` | 200 `Healthy` | |

**Task** (returned by GET, POST, PUT and PATCH):
```json
{
  "id": 16, "title": "Write release notes", "description": "optional", "status": "Todo", "priority": "High",
  "assignedTo": "Alice", "createdDate": "2026-11-20T10:00:00+00:00", "modifiedDate": "2026-11-20T10:00:00+00:00",
  "rowVersion": "AAAAAAAAB9E="
}
```

**Create and update fields:**
| Field | Rules |
|-------|-------|
| `title` | required, not blank, max 200; trimmed |
| `description` | optional, max 2000; blank becomes null |
| `status` | `Todo` / `InProgress` / `Done`, case-insensitive. Optional on POST (defaults to `Todo`), required on PUT. |
| `priority` | **required**, `Low` / `Medium` / `High` / `Critical`, case-insensitive; numbers are rejected |
| `assignedTo` | optional, max 100; blank becomes null |
| `rowVersion` | **PUT only, required**: the base64 value from the last response for this task |

**How each endpoint behaves:**
- **List:** both filters are optional and case-insensitive. An empty value means no filter. Results are sorted by
  priority (Critical, High, Medium, Low), then newest `createdDate`, then id. Soft-deleted tasks are never returned.
- **POST:** fields that aren't listed above (`id`, `isDeleted`, `createdDate`, …) are ignored.
- **PUT:** a full replacement. An omitted description or assignee is cleared. A stale `rowVersion` returns
  **409 Conflict** and nothing is saved.
- **PATCH:** status only, last write wins (no `rowVersion`). Setting the current status changes nothing.
  The response carries the **new** `rowVersion`.
- **DELETE:** soft delete. The row stays with `IsDeleted = 1` and is hidden from every endpoint, including the
  summary. Deleting again returns 404. To restore a task, set `IsDeleted = 0` in the database.
- **Summary:** counts of active tasks for each status and priority combination that has tasks, ordered Todo,
  InProgress, Done and then Critical…Low. It uses **raw SQL via ADO.NET** (`Services/TaskSummaryService.cs`):
  ```sql
  SELECT [Status], [Priority], COUNT(*) AS [Count]
  FROM [dbo].[Tasks]
  WHERE [IsDeleted] = 0
  GROUP BY [Status], [Priority]
  ORDER BY
      CASE [Status] WHEN N'Todo' THEN 1 WHEN N'InProgress' THEN 2 WHEN N'Done' THEN 3 ELSE 4 END,
      CASE [Priority] WHEN N'Critical' THEN 1 WHEN N'High' THEN 2 WHEN N'Medium' THEN 3 WHEN N'Low' THEN 4 ELSE 5 END;
  ```
- **Dates:** `modifiedDate` is set to the current UTC time (from `TimeProvider`) on every write, including delete.
  `createdDate` never changes.

## Error responses
Every error is an RFC 7807 `application/problem+json` body with the same shape:
```json
{ "type": "...", "title": "Resource not found", "status": 404, "detail": "Task with id '99' was not found.", "instance": "GET /api/tasks/99", "traceId": "00-..." }
```
- **400:** adds an `errors` object keyed exactly like the JSON body or query parameter (camelCase: `title`,
  `priority`, `status`, `assignedTo`, `rowVersion`). Malformed JSON or wrong JSON types are reported under a
  JSON-path key such as `$.priority`, with the generic message "The input was not valid.".
- **409:** "The task was changed by someone else. Reload it and try again."
- **500:** a generic message. The exception and stack trace are logged server-side only. Quote `traceId` to find the log entry.
- **Unknown routes (404) and wrong methods (405):** the same shape.

## Data model and seed
Table `dbo.Tasks` (entity `TaskItem`): `Id` (identity), `Title` nvarchar(200), `Description` nvarchar(2000) null,
`Status` and `Priority` nvarchar(20), `AssignedTo` nvarchar(100) null, `CreatedDate` and `ModifiedDate` datetimeoffset,
`IsDeleted` bit, `RowVersion` rowversion.
- **Constraints:** check constraints `CK_Tasks_Status` and `CK_Tasks_Priority` allow only the enum names.
- **Index:** `IX_Tasks_IsDeleted_Status_Priority` supports filtering and the summary query.
- **Seed:** the `SeedTasks` migration inserts 15 tasks via `HasData` (`Data/SeedData.cs`):
  - ids 1–12 are active, one per status × priority combination
  - ids 13–15 are soft-deleted
  - AssignedTo varies across Alice, Bob, Chen, Dana and Eve, plus some unassigned tasks
  - ids and dates are fixed literals
  - to change the seed, edit `SeedData.cs` and add a new migration. Never edit an applied one.

## UI structure
```
src/task-manager-ui/src/
  api/          httpClient.js (the only fetch; ProblemDetails → ApiError), tasksApi.js, config.js, ApiError.js
  hooks/        useTasks (list + filters), useSummary, useStatusChange
  components/   TaskList/TaskRow/StatusSelect, TaskFilters, TaskSummary, TaskEditor/TaskForm/FormField,
                Modal, ConfirmDeleteDialog, ConflictNotice/UnsavedEdits, badges, EmptyState, ErrorBanner, Notice
  validation/   taskForm.js (client rules mirroring the API; builds request bodies)
  constants/    taskOptions.js (status/priority values, labels, icons), taskRules.js (length limits)
```
- **API layer:** every failure reaches components as an `ApiError` with `title`, `detail`, camelCase `fieldErrors`,
  `traceId`, and a `kind` of `http`, `network` or `config`.
- **Priority display:** colour plus an icon plus a text label (▲▲ Critical, ▲ High, ● Medium, ▼ Low), so it never
  relies on colour alone.
- **Create and edit:** one `TaskForm` inside an accessible `Modal`:
  - edit loads the task fresh with `getTask`
  - server field errors show next to their fields, and anything else shows in a form-level alert
  - on 409 the user's values are kept. "Load latest version" shows the other person's changes and lists "Your unsaved edits".
- **Status dropdown:** the change shows straight away and the row reads "Saving…" while it's in flight. On failure
  the dropdown reverts and an error shows under that row. On success the row takes the new rowVersion.
- **Delete:** a confirmation dialog with Cancel focused by default. A 404 is treated as "already deleted". Afterwards
  focus moves to the Tasks heading.
- **Summary:** a status × priority matrix with totals, independent of the list filters. It refreshes after every
  create, edit, status change and delete.

## Design decisions and trade-offs
- **Soft delete with an EF global query filter.** Deleted rows vanish from every EF query automatically and can be
  restored. Raw SQL has to filter `IsDeleted = 0` itself, and there's a test proving the summary would count deleted
  rows without that filter.
- **Optimistic concurrency only where it pays.**
  - PUT replaces every field, so it requires `rowVersion` and returns 409 on a stale one.
  - PATCH status and DELETE are last-write-wins, so the status dropdown and delete stay one-click and nothing silently
    reverts. The responses carry the new rowVersion so a later PUT doesn't get a false 409.
- **Enums stored as strings**, so the database is readable and protected by check constraints. The catch is that
  sorting by the column would be alphabetical. Both the LINQ list query and the summary SQL sort by an explicit
  `CASE` rank instead.
- **The summary uses plain ADO.NET**, as the brief requires raw SQL rather than EF:
  - it's a constant SQL string with no concatenation; any future filter must be a typed `SqlParameter`
  - it uses the same connection string configuration
  - its results are parsed into the enums
- **Seed data in a migration (`HasData`).** It's deterministic and needs no startup code, but it is applied to
  *every* database the migrations reach.
- **Errors in one shape.** A global exception handler returns ProblemDetails, error keys are camelCase to match the
  JSON, and no stack traces or type names are sent to clients.
- **The UI calls the API directly**, with strict CORS (exact origins) and a pinned dev port. There's no proxy hiding CORS problems.
- **JavaScript with JSDoc rather than TypeScript.** It's a smaller toolchain for about 30 small modules. The trade-off
  is no compile-time type checking.
- **A custom accessible `Modal` rather than `<dialog>`.** jsdom (which the tests run in) doesn't implement
  `showModal`, so focus trapping, Escape and `inert` are implemented by hand and tested directly.
- **Exact version pins** everywhere: `Directory.Packages.props` with Microsoft packages on 9.0.x,
  `package-lock.json`, and `.npmrc` `save-exact`. Versions were at least two weeks old when added.

## Known limitations
- No authentication or authorisation: every endpoint is anonymous.
- No paging on the list (fine for this dataset). If it's needed later, add optional `page`/`pageSize` parameters and
  an `X-Total-Count` header without breaking the response shape.
- Soft-deleted tasks can only be restored directly in the database.
- The seed data is applied to every database that runs the migrations.

## Logging
Built-in `ILogger` structured logging:
- **Requests:** each request is logged once, with method, path, status and duration. Bodies and headers are not logged.
- **Services:** they log writes, e.g. "Created task {TaskId}".
- **Errors:** `GlobalExceptionHandler` logs failures with the `traceId` that is also returned to the client.
