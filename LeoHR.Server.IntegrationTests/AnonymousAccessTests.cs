using System.Net;

using LeoHR.Server.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AnonymousAccessTests(LeoHrApiFactory factory)
{
    [Theory]
    [InlineData("api/health/db")]
    [InlineData("api/features")]
    [InlineData("openapi/v1.json")]
    public async Task A_public_endpoint_answers_without_sign_in(string url)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // The test build has no compiled SPA, so a page can be a 404 here, but never behind sign-in.
    [Theory]
    [InlineData("")]
    [InlineData("promotions/123")]
    public async Task The_app_s_pages_do_not_require_sign_in(string url)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(url);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_endpoint_without_an_access_rule_requires_sign_in()
    {
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddControllers().AddApplicationPart(typeof(UnattributedTestController).Assembly)));
        HttpClient client = app.CreateClient();

        HttpResponseMessage anonymous = await client.GetAsync(UnattributedTestController.Route);
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, TestUser.Employee(factory.Employees.Add().Id).ToHeader());
        HttpResponseMessage signedIn = await client.GetAsync(UnattributedTestController.Route);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
    }
}

[ApiController]
[Route(Route)]
public sealed class UnattributedTestController : ControllerBase
{
    public const string Route = "api/test/unattributed";

    [HttpGet]
    public string Get()
    {
        return "reached";
    }
}
