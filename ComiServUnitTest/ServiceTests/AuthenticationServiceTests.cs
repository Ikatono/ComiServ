using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComiServ.Entities;
using ComiServ.Services;

namespace ComiServUnitTest.ServiceTests;

[TestClass]
public class AuthenticationServiceTests
{
    [TestMethod]
    public void FailAuth()
    {
        IAuthenticationService auth = new AuthenticationService();
        Assert.IsFalse(auth.Tested);
        auth.FailAuth();
        Assert.IsTrue(auth.Tested);
        Assert.IsNull(auth.User);
    }
    [TestMethod]
    public void AuthenticateUser()
    {
        IAuthenticationService auth = new AuthenticationService();
        User user = new()
        {
            Username = "NewUser",
            UserTypeId = UserTypeEnum.User,
        };
        Assert.IsFalse(auth.Tested);
        auth.Authenticate(user);
        Assert.IsTrue(auth.Tested);
        Assert.IsNotNull(auth.User);
        Assert.AreSame(user, auth.User);
    }
}
