Server=localhost\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True
---------------------------------------------------------------------------------------------------

I'm working on a small task management app and want to use you as my pair
throughout. CLAUDE.md in the repo root has my conventions, so please read it first.

The goal is a small task management app. .NET 9 Web API with EF Core code first
on my local SQL Server Express (localhost\SQLEXPRESS), and a simple React + Vite
front end on top. Users can list tasks (filter by status and priority), view one,
create, update, and soft delete. There also needs to be a
GET /api/tasks/summary endpoint that uses raw SQL, not EF, to return task counts
grouped by status and priority.

I don't want any code yet. I'd like to prepare a plan first. Can you propose:

- how you'd lay out the solution (api, tests, ui), and whether a small shared
  library is worth it here
- how you'd model the Task entity and handle soft delete
- how you'd do validation and a global exception handler so every error comes
  back in the same shape
- where logging should go
- how we'd seed 10 or so sample tasks
- how we'd test it. so I want failure cases
  and filtering covered too
- a breakdown into small steps where each one can be built, tested and committed
  on its own

For Security taking CORS, config/secrets and
the raw SQL that we should handle properly from the start.

If you're unsure about something , tell me instead of picking
quietly. I'd rather decide than find out later. Don't start implementing until we confirm the plan.