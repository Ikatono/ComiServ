using ComiServ.Entities;

namespace ComiServ.Services;

public interface IAuthenticationService
{
    public bool Tested { get; }
    public User? User { get; }
    public void Authenticate(User user);
    public void FailAuth();
}
//acts as a per-request container of authentication info
public class AuthenticationService : IAuthenticationService
{
    public bool Tested { get; private set; } = false;

    public User? User { get; private set; }
    public AuthenticationService()
    {

    }
    public void Authenticate(User user)
    {
        User = user;
        Tested = true;
    }
    public void FailAuth()
    {
        User = null;
        Tested = true;
    }
}
