# Task Manager

A small task management app: a .NET 9 Web API (EF Core Code First, SQL Server) and a React + Vite UI.

> Status: **step 1 of 11**: solution scaffold and cross-cutting concerns (error handling, logging, CORS).
> Task endpoints, the database and the UI come in later steps.

## Prerequisites
- .NET SDK 9.0.x (`global.json` pins to the 9.0 line and rolls forward to the latest installed 9.0 feature band)
- SQL Server Express at `localhost\SQLEXPRESS` (from step 2 onwards)
- Node.js 20+ (from step 9 onwards)

## Layout
```
src/TaskManager.Api/      Web API (Controllers, Domain, Infrastructure, ...)
tests/TaskManager.Tests/  xUnit tests (Unit/ and Integration/)
src/task-manager-ui/      React + Vite UI (later)
```
NuGet versions are managed centrally in `Directory.Packages.props`. All Microsoft.* framework packages are pinned to 9.0.x.

## Build, test, run
```powershell
dotnet build
dotnet test
dotnet run --project src/TaskManager.Api --launch-profile http   # http://localhost:5157
```
`src/TaskManager.Api/TaskManager.Api.http` has ready-made requests (VS / VS Code REST Client / Rider).

## Endpoints (so far)
| Method | Path      | Notes                     |
|--------|-----------|---------------------------|
| GET    | `/health` | Liveness check, "Healthy" |
| GET    | `/openapi/v1.json` | OpenAPI document (Development only) |

## Error responses
Every error is an RFC 7807 `application/problem+json` body with the same shape:
```json
{ "type": "...", "title": "Not Found", "status": 404, "detail": "...", "instance": "GET /nope", "traceId": "00-..." }
```
- Validation errors (400) add an `errors` object keyed by field.
- Unhandled exceptions return a generic 500. The exception message and stack trace are logged server-side
  only and never returned. Quote `traceId` to find the log entry.
- Unknown routes (404) and wrong methods (405) use the same shape.

## Configuration
| Key | Where | Notes |
|-----|-------|-------|
| `Cors:AllowedOrigins` | `appsettings.Development.json` (`http://localhost:5173`) | Exact origins only, no wildcards and no trailing slash. **Required outside Development**: the API refuses to start without it. |
| `ConnectionStrings:DefaultConnection` | added in step 2 | |

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
