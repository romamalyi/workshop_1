using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Models;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeoHR.Server.IntegrationTests;

// The tests record email instead of sending it, so the whitelist is checked on the bound settings.
[Collection(ApiCollection.Name)]
public sealed class EmailWhitelistSettingTests(LeoHrApiFactory factory)
{
    [Fact]
    public void The_whitelist_is_on_in_an_environment_that_does_not_configure_it()
    {
        Assert.True(factory.Services.GetRequiredService<IOptions<EmailSettings>>().Value.WhitelistEnabled);
    }

    // Reads the committed appsettings files only: starting the app in another environment would load the
    // developer's secrets.{Environment}.json and migrate the database it names.
    [Theory]
    [InlineData("Test", true)]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void Only_Production_turns_the_whitelist_off(string environment, bool expected)
    {
        string contentRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json")
            .Build();

        EmailSettings settings = configuration.GetSection(ConfigurationSections.Email).Get<EmailSettings>()!;

        Assert.Equal(expected, settings.WhitelistEnabled);
    }
}
