using Lab1.Models;

namespace Lab1.Desktop.Services;

public class DatabaseService : ApiServiceBase
{
    public async Task<List<Database>> GetAllAsync()
        => await GetAsync<List<Database>>("databases/list") ?? new();

    public async Task<Database?> GetAsync(Guid databaseId)
        => await GetAsync<Database>($"databases?databaseId={databaseId}");

    public async Task CreateAsync(string name)
        => await PostAsync<object>($"databases?name={Uri.EscapeDataString(name)}");

    public async Task DeleteDatabaseAsync(Guid databaseId)
    => await base.DeleteAsync($"databases?databaseId={databaseId}");
}
