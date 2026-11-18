using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeoHR.Server.IntegrationTests.Infrastructure;

public static class HttpClientJson
{
    // Same enum handling as the API's JsonStringEnumConverter.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostJson<T>(this HttpClient client, string url, T body)
    {
        return client.PostAsJsonAsync(url, body, Options);
    }

    public static Task<HttpResponseMessage> PutJson<T>(this HttpClient client, string url, T body)
    {
        return client.PutAsJsonAsync(url, body, Options);
    }

    public static Task<HttpResponseMessage> PatchJson<T>(this HttpClient client, string url, T body)
    {
        return client.PatchAsJsonAsync(url, body, Options);
    }

    public static async Task<T> GetOk<T>(this HttpClient client, string url)
    {
        return await (await client.GetAsync(url)).ReadOk<T>();
    }

    public static async Task<T> ReadOk<T>(this HttpResponseMessage response)
    {
        await response.EnsureSuccess();
        return (await response.Content.ReadFromJsonAsync<T>(Options))!;
    }

    // Puts the API's error body in the failure message, which a bare EnsureSuccessStatusCode hides.
    public static async Task EnsureSuccess(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} returned {(int)response.StatusCode}: {body}");
        }
    }
}
