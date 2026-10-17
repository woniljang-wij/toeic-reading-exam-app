using TOEIC.Models.TokenAuth;
using TOEIC.Web.Controllers;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace TOEIC.Web.Tests.Controllers;

public class HomeController_Tests : TOEICWebTestBase
{
    [Fact]
    public async Task Index_Test()
    {
        await AuthenticateAsync(null, new AuthenticateModel
        {
            UserNameOrEmailAddress = "admin",
            Password = "123qwe"
        });

        //Act
        var response = await GetResponseAsStringAsync(
            GetUrl<HomeController>(nameof(HomeController.Index))
        );

        //Assert
        response.ShouldNotBeNullOrEmpty();
    }
}