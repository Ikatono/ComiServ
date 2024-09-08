using ComiServ.Entities;

namespace ComiServ.Models;

public class UserModifyRequest
{
    public string Username { get; set; }
    public string? NewUsername { get; set; }
    public UserTypeEnum? NewUserType { get; set; }
}
