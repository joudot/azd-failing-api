using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace SimpleTodo.Api;

[ApiController]
[Route("/lists")]
public class ListsController : ControllerBase
{
    // This TelemetryClient instance can be used to track additional telemetry through the TrackXXX() API.
    // private readonly TelemetryClient _telemetryClient;
    private readonly ListsRepository _repository;
    private readonly ILogger _logger;
    private TelemetryClient _telemetryClient;

    public ListsController(ListsRepository repository, ILogger<ListsController> logger, TelemetryClient telemetryClient)
    {
        _repository = repository;
        _logger = logger;
        // If null, try the followinf sample: https://github.com/Azure-Samples/application-insights-aspnet-sample-opentelemetry/blob/master/src/Sample.MainApi/Controllers/MainController.cs
        _telemetryClient = telemetryClient;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<TodoList>>> GetLists([FromQuery] int? skip = null, [FromQuery] int? batchSize = null)
    {
        return Ok(await _repository.GetListsAsync(skip, batchSize));
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<ActionResult> CreateList([FromBody]CreateUpdateTodoList list)
    {
        var todoList = new TodoList(list.name)
        {
            Description = list.description
        };

        await _repository.AddListAsync(todoList);

        // Establish an operation context and associated telemetry item:
        using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("testListName"))
        {
            if(list.name.Contains("FAIL", StringComparison.InvariantCultureIgnoreCase))
            {
                _telemetryClient.TrackEvent("ListCreationFailure");
                int cpt = 0;
                while (cpt++ < 10)
                {
                    var t = Task.Run(async delegate
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(250));
                                return;
                            });
                    t.Wait();                }

                _telemetryClient.StopOperation(operation);
                throw new IntendedException("List created with FAIL in name");
            }
        }
        return CreatedAtAction(nameof(GetList), new { list_id = todoList.Id }, todoList);
    }

    [HttpGet("{list_id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<TodoList>>> GetList(string list_id)
    {
        var list = await _repository.GetListAsync(list_id);

        return list == null ? NotFound() : Ok(list);
    }

    [HttpPut("{list_id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<TodoList>> UpdateList(string list_id, [FromBody]CreateUpdateTodoList list)
    {
        var existingList = await _repository.GetListAsync(list_id);
        if (existingList == null)
        {
            return NotFound();
        }

        existingList.Name = list.name;
        existingList.Description = list.description;
        existingList.UpdatedDate = DateTimeOffset.UtcNow;

        await _repository.UpdateList(existingList);

        return Ok(existingList);
    }

    [HttpDelete("{list_id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteList(string list_id)
    {
        if (await _repository.GetListAsync(list_id) == null)
        {
            return NotFound();
        }

        await _repository.DeleteListAsync(list_id);

        return NoContent();
    }

    [HttpGet("{list_id}/items")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetListItems(string list_id, [FromQuery] int? skip = null, [FromQuery] int? batchSize = null)
    {
        if (await _repository.GetListAsync(list_id) == null)
        {
            return NotFound();
        }
        return Ok(await _repository.GetListItemsAsync(list_id, skip, batchSize));
    }

    [HttpPost("{list_id}/items")]
    [ProducesResponseType(201)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TodoItem>> CreateListItem(string list_id, [FromBody] CreateUpdateTodoItem item)
    {
        if (await _repository.GetListAsync(list_id) == null)
        {
            return NotFound();
        }

        var newItem = new TodoItem(list_id, item.name)
        {
            Name = item.name,
            Description = item.description,
            State = item.state,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await _repository.AddListItemAsync(newItem);

        if(newItem.Name.Contains("FAIL", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new IntendedException("List created with FAIL in name");
        }
        return CreatedAtAction(nameof(GetListItem), new { list_id = list_id, item_id = newItem.Id }, newItem);
    }

    [HttpGet("{list_id}/items/{item_id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TodoItem>> GetListItem(string list_id, string item_id)
    {
        if (await _repository.GetListAsync(list_id) == null)
        {
            return NotFound();
        }

        var item = await _repository.GetListItemAsync(list_id, item_id);

        return item == null ? NotFound() : Ok(item);
    }

    [HttpPut("{list_id}/items/{item_id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TodoItem>> UpdateListItem(string list_id, string item_id, [FromBody] CreateUpdateTodoItem item)
    {
        var existingItem = await _repository.GetListItemAsync(list_id, item_id);
        if (existingItem == null)
        {
            return NotFound();
        }

        existingItem.Name = item.name;
        existingItem.Description = item.description;
        existingItem.CompletedDate = item.completedDate;
        existingItem.DueDate = item.dueDate;
        existingItem.State = item.state;
        existingItem.UpdatedDate = DateTimeOffset.UtcNow;

        await _repository.UpdateListItem(existingItem);

        return Ok(existingItem);
    }

    [HttpDelete("{list_id}/items/{item_id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteListItem(string list_id, string item_id)
    {
        if (await _repository.GetListItemAsync(list_id, item_id) == null)
        {
            return NotFound();
        }

        await _repository.DeleteListItemAsync(list_id, item_id);

        return NoContent();
    }

    [HttpGet("{list_id}/state/{state}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetListItemsByState(string list_id, string state, [FromQuery] int? skip = null, [FromQuery] int? batchSize = null)
    {
        if (await _repository.GetListAsync(list_id) == null)
        {
            return NotFound();
        }

        return Ok(await _repository.GetListItemsByStateAsync(list_id, state, skip, batchSize));
    }

    public record CreateUpdateTodoList(string name, string? description = null);
    public record CreateUpdateTodoItem(string name, string state, DateTimeOffset? dueDate, DateTimeOffset? completedDate, string? description = null);
}