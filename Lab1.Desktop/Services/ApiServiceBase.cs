using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

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

    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(text))
            return $"Request failed with status {(int)response.StatusCode} ({response.StatusCode})";

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var msgProp) &&
                    msgProp.ValueKind == JsonValueKind.String)
                {
                    return msgProp.GetString() ?? text;
                }

                if (root.TryGetProperty("error", out var errProp) &&
                    errProp.ValueKind == JsonValueKind.String)
                {
                    return errProp.GetString() ?? text;
                }
            }
        }
        catch
        {
        }

        return text;
    }


    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorMessage(response));

        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object? body = null)
    {
        var response = await _client.PostAsJsonAsync(endpoint, body ?? new { });

        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorMessage(response));

        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task DeleteAsync(string endpoint)
    {
        var response = await _client.DeleteAsync(endpoint);

        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorMessage(response));
    }

    protected async Task<T?> PutAsync<T>(string endpoint, object body)
    {
        var response = await _client.PutAsJsonAsync(endpoint, body);

        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorMessage(response));

        return await response.Content.ReadFromJsonAsync<T>();
    }
}
