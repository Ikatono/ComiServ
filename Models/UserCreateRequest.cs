using ComiServ.Entities;

namespace ComiServ.Models
{
    public class UserCreateRequest
    {
        public string Username { get; set; }
        public UserTypeEnum UserType { get; set; }
        //NOT HASHED do not persist this object
        public string Password { get; set; }
    }
}
