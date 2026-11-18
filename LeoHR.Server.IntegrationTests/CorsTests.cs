using System.Net;

using LeoHR.Server.IntegrationTests.Infrastructure;

namespace LeoHR.Server.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CorsTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task A_configured_origin_gets_the_CORS_headers()
    {
        HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "api/features");
        request.Headers.Add("Origin", LeoHrApiFactory.AllowedCorsOrigin);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(LeoHrApiFactory.AllowedCorsOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task The_preflight_of_a_request_with_a_bearer_token_is_answered_for_a_configured_origin()
    {
        HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Options, "api/promotions");
        request.Headers.Add("Origin", LeoHrApiFactory.AllowedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, $"Preflight returned {(int)response.StatusCode}");
        Assert.Equal(LeoHrApiFactory.AllowedCorsOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("authorization", string.Join(',', response.Headers.GetValues("Access-Control-Allow-Headers")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Another_origin_gets_no_CORS_headers()
    {
        HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "api/features");
        request.Headers.Add("Origin", "https://other.example.test");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
