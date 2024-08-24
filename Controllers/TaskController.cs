using ComiServ.Background;
using ComiServ.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ComiServ.Controllers
{
    [Route("api/v1/tasks")]
    [ApiController]
    public class TaskController(
        ComicsContext context
        ,ITaskManager manager
        ,IComicScanner scanner
        ,ILogger<TaskController> logger
        ) : ControllerBase
    {
        private readonly ComicsContext _context = context;
        private readonly ITaskManager _manager = manager;
        private readonly IComicScanner _scanner = scanner;
        private readonly ILogger<TaskController> _logger = logger;
        private readonly CancellationTokenSource cancellationToken = new();
        [HttpGet]
        [ProducesResponseType<Truncated<string>>(StatusCodes.Status200OK)]
        public IActionResult GetTasks(
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
    }
}
