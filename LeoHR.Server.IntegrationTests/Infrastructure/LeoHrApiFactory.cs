using LeoHR.Server.Features.Promotions;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Employees;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

namespace LeoHR.Server.IntegrationTests.Infrastructure;

public sealed class LeoHrApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // CI runners can't run Docker, so CI hands in a throwaway Postgres service instead of a container.
    private readonly string? externalConnectionString =
        Environment.GetEnvironmentVariable("LEOHR_TEST_CONNECTION_STRING") is { Length: > 0 } value ? value : null;

    private PostgreSqlContainer? postgres;

    public const string AllowedCorsOrigin = "https://employee.example.test";

    public FakeEmployeeApi Employees { get; } = new();

    public RecordingSmtpClient Smtp { get; } = new();

    public async Task InitializeAsync()
    {
        if (externalConnectionString is null)
        {
            postgres = new PostgreSqlBuilder("postgres:16").Build();
            await postgres.StartAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }

    public HttpClient ClientFor(TestUser user)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, user.ToHeader());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Its own environment, so neither appsettings.Test.json nor a developer's secrets.Test.json is loaded.
        builder.UseEnvironment("IntegrationTest");
        builder.UseSetting("ConnectionStrings:LeoHr", externalConnectionString ?? postgres!.GetConnectionString());
        builder.UseSetting("IdentityServer:Authority", "https://identity.invalid");
        builder.UseSetting("EmployeeApi:BaseUrl", "https://employee-api.invalid");
        builder.UseSetting("EmployeeApi:Auth:TokenEndpoint", "https://identity.invalid/connect/token");
        builder.UseSetting("EmployeeApi:Auth:ClientId", "integration-tests");
        builder.UseSetting("EmployeeApi:Auth:ClientSecret", "integration-tests");
        builder.UseSetting("EmployeeApi:Auth:Scope", "employee");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedCorsOrigin);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmployeeApiClient>();
            services.AddSingleton<IEmployeeApiClient>(Employees);
            services.RemoveAll<ISmtpHttpClient>();
            services.AddSingleton<ISmtpHttpClient>(Smtp);

            // Tests decide which promotions exist, and the employee list is the fake one.
            RemoveHostedService<MissingPromotionsJob>(services);
            RemoveHostedService<PromotionReadyEmailJob>(services);
            RemoveHostedService<EmployeeCacheRefresher>(services);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services.Where(sd => sd.ImplementationType == typeof(T)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
