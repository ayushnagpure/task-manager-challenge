# Task Manager - Project Instructions

## Stack
- Backend: .NET 9 Web API (/src/TaskManager.Api), EF Core 9 Code First, SQL Server
- Frontend: React + Vite (/src/task-manager-ui), plain CSS
- Tests: xUnit (/tests/TaskManager.Tests), Vitest for UI
- Target framework: net9.0. Pin all Microsoft.* NuGet packages to 9.0.x (never 10.x)

## Conventions
- Follow .NET naming conventions; async/await with CancellationToken
- Controllers thin; business logic in services behind interfaces (DI)
- DTOs for requests/responses; never expose entities directly
- Enums stored as strings in DB; validate with DataAnnotations or FluentValidation
- Soft delete via IsDeleted + EF global query filter
- React: functional components, hooks, one component per file, API calls in an /api layer

## Non-functional requirements (apply to every change)
- Logging: ILogger structured logging at service and middleware boundaries
- Errors: global exception middleware returning ProblemDetails; no stack traces to clients
- Security: validate all input, parameterize all raw SQL, no secrets in source (use user-secrets or env vars)
- Testability: services depend on interfaces; add tests with each feature
- Docs: XML comments on public API, keep README current

## Workflow rules
- Always propose a plan before implementing; wait for my approval
- Write or extend tests alongside each feature and run `dotnet test` before finishing
- Keep changes small: one logical feature per iteration
- Do not commit; I will commit after verifying

## Database
- SQL Server instance: localhost\SQLEXPRESS (SQL Server 2019 Express); database TaskManagerDb; Windows auth
- Connection string key: ConnectionStrings:DefaultConnection (dev value in appsettings.Development.json; README documents how to change it for another machine)
