using ComiServ.Entities;
using ComiServ.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Text;

namespace ComiServ.Middleware
{
    //only user of a type in `authorized` are permitted past this middleware
    //auth header is only checked once, so you can place multiple in the pipeline to further restrict
    //some endpoints
    public class BasicAuthenticationMiddleware(RequestDelegate next, UserTypeEnum[] authorized)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext httpContext, ComicsContext context, IAuthenticationService auth)
        {
            if (!auth.Tested)
            {
                var authHeader = httpContext.Request.Headers.Authorization.SingleOrDefault();
                if (authHeader is string header)
                {
                    if (header.StartsWith("Basic"))
                    {
                        header = header[5..].Trim();
                        byte[] data = Convert.FromBase64String(header);
                        string decoded = Encoding.UTF8.GetString(data);
                        var split = decoded.Split(':', 2);
                        if (split.Length == 2)
                        {
                            var user = split[0];
                            var pass = split[1];
                            var userCon = context.Users
                                .Include(u => u.UserType)
                                .SingleOrDefault(u => EF.Functions.Like(u.Username, user));
                            if (userCon is not null && userCon.UserTypeId != UserTypeEnum.Disabled)
                            {
                                var bPass = Encoding.UTF8.GetBytes(pass);
                                var salt = userCon.Salt;
                                var hashed = User.Hash(bPass, salt);
                                if (hashed.SequenceEqual(userCon.HashedPassword))
                                    auth.Authenticate(userCon);
                            }
                        }
                    }
                    //handle other schemes here maybe
                }
                else
                {
                    auth.FailAuth();
                }
            }
            if (authorized.Length == 0 || authorized.Contains(auth.User?.UserTypeId ?? UserTypeEnum.Invalid))
            {
                await _next(httpContext);
            }
            else if (auth.User is not null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                httpContext.Response.Headers.WWWAuthenticate = "Basic";
            }
        }
    }
    public static class BasicAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder builder, IEnumerable<UserTypeEnum> authorized)
        {
            //keep a private copy of the array
            return builder.UseMiddleware<BasicAuthenticationMiddleware>(authorized.ToArray());
        }
    }
}
