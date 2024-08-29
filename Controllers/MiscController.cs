using Microsoft.EntityFrameworkCore;
using ComiServ.Entities;
using Microsoft.AspNetCore.Mvc;
using ComiServ.Services;
using ComiServ.Background;
using ComiServ.Models;
using System.ComponentModel;

namespace ComiServ.Controllers;

[Route(ROUTE)]
[ApiController]
public class MiscController(ComicsContext context, ILogger<MiscController> logger, IConfigService config, IAuthenticationService auth)
    : ControllerBase
{
    public const string ROUTE = "/api/v1/";
    ComicsContext _context = context;
    ILogger<MiscController> _logger = logger;
    IConfigService _config = config;
    IAuthenticationService _auth = auth;
    [HttpGet("authors")]
    [ProducesResponseType<Paginated<AuthorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuthors(
        [FromQuery]
        [DefaultValue(0)]
        int page,
        [FromQuery]
        [DefaultValue(20)]
        int pageSize
        )
    {
        if (_auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (_auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        var items = _context.Authors
            .OrderBy(a => a.ComicAuthors.Count())
            .Select(a => new AuthorResponse(a.Name, a.ComicAuthors.Count()));
        return Ok(await Paginated<AuthorResponse>.CreateAsync(pageSize, page, items));
    }
    [HttpGet("tags")]
    [ProducesResponseType<Paginated<TagResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTags(
        [FromQuery]
        [DefaultValue(0)]
        int page,
        [FromQuery]
        [DefaultValue(20)]
        int pageSize
        )
    {
        if (_auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (_auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        var items = _context.Tags
            .OrderBy(t => t.ComicTags.Count())
            .Select(t => new TagResponse(t.Name, t.ComicTags.Count()));
        return Ok(await Paginated<TagResponse>.CreateAsync(pageSize, page, items));
    }
}
