using ComiServ.Entities;
using ComiServ.Models;
using ComiServ.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Text;

namespace ComiServ.Controllers;

[Route(ROUTE)]
[ApiController]
public class UserController
    : ControllerBase
{
    public const string ROUTE = "/api/v1/users";
    [HttpGet]
    [ProducesResponseType<Paginated<UserDescription>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(IAuthenticationService auth,
        ComicsContext context,
        [FromQuery]
        [DefaultValue(null)]
        string? search,
        [FromQuery]
        [DefaultValue(null)]
        UserTypeEnum? type,
        [FromQuery]
        [DefaultValue(0)]
        int page,
        [FromQuery]
        [DefaultValue(20)]
        int pageSize)
    {
        if (auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        IQueryable<User> users = context.Users;
        if (type is UserTypeEnum t)
            users = users.Where(u => u.UserTypeId == t);
        if (!string.IsNullOrWhiteSpace(search))
            users = users.Where(u => EF.Functions.Like(u.Username, $"%{search}%"));
        return Ok(await Paginated<UserDescription>.CreateAsync(pageSize, page, users
                                                                                .Include(u => u.UserType)
                                                                                .Select(u => new UserDescription(u.Username, u.UserType.Name))));
    }
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddUser(IAuthenticationService auth,
        ComicsContext context,
        [FromBody]
        UserCreateRequest req)
    {
        if (auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        var salt = Entities.User.MakeSalt();
        var bPass = Encoding.UTF8.GetBytes(req.Password);
        var newUser = new Entities.User()
        {
            Username = req.Username,
            Salt = salt,
            HashedPassword = Entities.User.Hash(password: bPass, salt: salt),
            UserTypeId = req.UserType
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync();
        return Ok();
    }
    [HttpDelete("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(IAuthenticationService auth,
        ComicsContext context,
        [FromBody]
        string username)
    {
        if (auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        username = username.Trim();
        var user = await context.Users.SingleOrDefaultAsync(u => EF.Functions.Like(u.Username, $"{username}"));
        if (user is null)
            return BadRequest();
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return Ok();
    }
    [HttpPost("modify")]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ModifyUser(IAuthenticationService auth,
        ComicsContext context,
        [FromBody]
        UserModifyRequest req)
    {
        //must be authenticated
        if (auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        req.Username = req.Username.Trim();
        //must be an admin or changing own username
        if (!req.Username.Equals(auth.User.Username, StringComparison.CurrentCultureIgnoreCase)
            && auth.User.UserTypeId != UserTypeEnum.Administrator)
        {
            return Forbid();
        }
        //only admins can change user type
        if (auth.User.UserTypeId != UserTypeEnum.Administrator
            && req.NewUserType is not null)
        {
            return Forbid();
        }
        var user = await context.Users
            .SingleOrDefaultAsync(u => EF.Functions.Like(u.Username, req.Username));
        if (user is null)
        {
            return BadRequest(RequestError.UserNotFound);
        }
        if (req.NewUsername is not null)
        {
            user.Username = req.NewUsername.Trim();
        }
        if (req.NewUserType is UserTypeEnum nut)
        {
            user.UserTypeId = nut;
        }
        await context.SaveChangesAsync();
        return Ok();
    }
}
