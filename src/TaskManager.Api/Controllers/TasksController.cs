using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

/// <summary>Task endpoints. Errors are returned as RFC 7807 ProblemDetails.</summary>
[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    /// <summary>Creates the controller.</summary>
    public TasksController(ITaskService taskService) => _taskService = taskService;

    /// <summary>Creates a task.</summary>
    /// <param name="request">The task to create.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <response code="201">Created. The <c>Location</c> header points to the new task.</response>
    /// <response code="400">Validation failed or the body is not valid JSON.</response>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Lists tasks, optionally filtered by <c>status</c> and/or <c>priority</c> (case-insensitive).
    /// Sorted by priority (Critical, High, Medium, Low), then newest first. Soft-deleted tasks are excluded.
    /// </summary>
    /// <param name="query">Optional filters.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <response code="200">The matching tasks (an empty array when nothing matches).</response>
    /// <response code="400">A filter value is not a valid status or priority.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List([FromQuery] TaskListQuery query, CancellationToken cancellationToken) =>
        Ok(await _taskService.ListAsync(query, cancellationToken));

    /// <summary>Gets a task by id. Soft-deleted tasks return 404.</summary>
    /// <param name="id">Task id (a positive integer).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <response code="200">The task.</response>
    /// <response code="404">No such task, or it has been deleted.</response>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken) =>
        await _taskService.GetByIdAsync(id, cancellationToken);
}
