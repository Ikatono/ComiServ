using ComiServ.Background;
using ComiServ.Models;
using ComiServ.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Policy;

namespace ComiServ.Controllers;

[Route(ROUTE)]
[ApiController]
public class TaskController(
    ComicsContext context
    ,ITaskManager manager
    ,IComicScanner scanner
    ,ILogger<TaskController> logger
    ) : ControllerBase
{
    public const string ROUTE = "/api/v1/tasks";
    private readonly ComicsContext _context = context;
    private readonly ITaskManager _manager = manager;
    private readonly IComicScanner _scanner = scanner;
    private readonly ILogger<TaskController> _logger = logger;
    private readonly CancellationTokenSource cancellationToken = new();
    [HttpGet]
    [ProducesResponseType<Truncated<string>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetTasks(
        [FromQuery]
        [DefaultValue(20)]
        int limit
        )
    {
        return Ok(new Truncated<string>(limit, _manager.GetTasks(limit+1)));
    }
    [HttpPost("scan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult StartScan()
    {
        _scanner.TriggerLibraryScan();
        return Ok();
    }
    [HttpPost("cancelall")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CancelAllTasks(Services.IAuthenticationService auth, ITaskManager manager)
    {
        if (auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (auth.User.UserTypeId != Entities.UserTypeEnum.Administrator)
            return Forbid();
        manager.CancelAll();
        return Ok();
    }
}
