using System.Net.Http;
using System.Net.Http.Json;

namespace Lab1.Desktop.Services;

public abstract class ApiServiceBase
{
    protected readonly HttpClient _client;

    protected ApiServiceBase()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7262/api/")
        };
    }

    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object? body = null)
    {
        var response = await _client.PostAsJsonAsync(endpoint, body ?? new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task DeleteAsync(string endpoint)
    {
        var response = await _client.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<T?> PutAsync<T>(string endpoint, object body)
    {
        var response = await _client.PutAsJsonAsync(endpoint, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
