using Lab1.Models;

namespace Lab1.Desktop.Services;

public class TableService : ApiServiceBase
{
    public async Task<List<Table>> GetTablesAsync(Guid databaseId)
        => await GetAsync<List<Table>>($"tables/list?databaseId={databaseId}") ?? new();

    public async Task<Table?> GetTableAsync(Guid databaseId, Guid tableId)
        => await GetAsync<Table>($"tables?databaseId={databaseId}&tableId={tableId}");

    public async Task CreateTableAsync(Guid databaseId, string tableName)
        => await PostAsync<object>($"tables/create?databaseId={databaseId}&tableName={Uri.EscapeDataString(tableName)}");

    public async Task DeleteTableAsync(Guid databaseId, Guid tableId)
        => await DeleteAsync($"tables?databaseId={databaseId}&tableId={tableId}");

    public async Task RenameTableAsync(Guid databaseId, Guid tableId, string newName)
    {
        // Спеціальний випадок, API цього не має, але можна видалити + створити копію
        var oldTable = await GetTableAsync(databaseId, tableId);
        if (oldTable == null) return;

        await CreateTableAsync(databaseId, newName);
        await DeleteTableAsync(databaseId, tableId);
    }
}
