using Anyware.TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetAllTasks;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetTaskById;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.UpdateTaskStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Anyware.TaskManagement.API.Controller
{

    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    [Produces("application/json")]
    public sealed class TasksController : ControllerBase
    {
        private readonly ISender _sender;

        public TasksController(ISender sender)
            => _sender = sender;

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateTask(
            [FromBody] CreateTaskRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateTaskCommand(
                request.Title,
                request.Description,
                request.Priority);

            var result = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                actionName: nameof(GetTaskById),
                routeValues: new { id = result.Id },
                value: result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTaskById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTaskByIdQuery(id), cancellationToken);
            return Ok(result);
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllTasks(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetAllTasksQuery(), cancellationToken);
            return Ok(result);
        }


        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateTaskStatus(
            [FromRoute] Guid id,
            [FromBody] UpdateTaskStatusRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateTaskStatusCommand(id, request.NewStatus);
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
    }
}