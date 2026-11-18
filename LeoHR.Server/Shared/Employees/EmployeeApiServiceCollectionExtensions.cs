using System.Net;
using System.Net.Http.Headers;

using Duende.AccessTokenManagement;

namespace LeoHR.Server.Shared.Employees;

public static class EmployeeApiServiceCollectionExtensions
{
    private const string TokenClientName = "employee.api";

    public static IServiceCollection AddEmployeeApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<EmployeeApiOptions>(configuration.GetSection(EmployeeApiOptions.SectionName));

        EmployeeApiOptions opts =
            configuration.GetSection(EmployeeApiOptions.SectionName).Get<EmployeeApiOptions>() ?? new EmployeeApiOptions();

        // OAuth2 client-credentials token, cached and auto-refreshed (Employee API tokens live ~5 min).
        services.AddClientCredentialsTokenManagement()
            .AddClient(TokenClientName, client =>
            {
                client.TokenEndpoint = new Uri(opts.Auth.TokenEndpoint);
                client.ClientId = ClientId.Parse(opts.Auth.ClientId);
                client.ClientSecret = ClientSecret.Parse(opts.Auth.ClientSecret);
                client.Scope = Scope.Parse(opts.Auth.Scope);
            });

        services.AddClientCredentialsHttpClient(
            EmployeeApiClient.HttpClientName,
            ClientCredentialsClientName.Parse(TokenClientName),
            client =>
            {
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli,
            });

        services.AddScoped<IEmployeeApiClient, EmployeeApiClient>();
        services.AddHostedService<EmployeeCacheRefresher>();
        return services;
    }
}
