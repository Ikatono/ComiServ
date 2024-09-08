using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ComiServ.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserTypeEnum
{
    //important that this is 0 as a safety precaution,
    //in case it's accidentally left as default
    Invalid = 0,
    //can create accounts
    Administrator = 1,
    //has basic access
    User = 2,
    //authenticates but does not give access
    Restricted = 3,
    //refuses to authenticate but maintains records
    Disabled = 4,
}
public class UserType
{
    public UserTypeEnum Id { get; set; }
    [MaxLength(26)]
    public string Name { get; set; }
    public ICollection<User> Users { get; set; }
}
