using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Domain;

namespace TaskManager.Tests.Integration.TestSupport;

/// <summary>
/// Test-only endpoints that throw, registered only by <see cref="TaskManagerApiFactory"/>.
/// Never part of the real API.
/// </summary>
[ApiController]
[Route("test/errors")]
public sealed class TestErrorsController : ControllerBase
{
    public const string SensitiveMessage = "Server=secret-db;Password=hunter2";

    [HttpGet("unhandled")]
    public IActionResult Unhandled() => throw new InvalidOperationException(SensitiveMessage);

    [HttpGet("not-found")]
    public IActionResult MissingResource() => throw new NotFoundException("Task", 42);
}
