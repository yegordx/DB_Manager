using Lab1.Models;

namespace Lab1.Desktop.Services;

public class TableService : ApiServiceBase
{
    public async Task<List<Table>> GetTablesAsync(Guid databaseId)
        => await GetAsync<List<Table>>($"tables/list?databaseId={databaseId}") ?? new();

    public async Task<Table?> GetTableAsync(Guid databaseId, Guid tableId)
        => await GetAsync<Table>($"tables?databaseId={databaseId}&tableId={tableId}");

    public async Task CreateTableAsync(Guid databaseId, string name)
        => await PostAsync<object>($"tables/create?databaseId={databaseId}&tableName={Uri.EscapeDataString(name)}");

    public async Task DeleteTableAsync(Guid databaseId, Guid tableId)
        => await DeleteAsync($"tables?databaseId={databaseId}&tableId={tableId}");


    public Task AddColumnAsync(Guid databaseId, Guid tableId, string name, string type)
        => AddColumnAsync(databaseId, tableId, name, type, null, null);

    public async Task AddColumnAsync(
        Guid databaseId,
        Guid tableId,
        string name,
        string type,
        char? start,
        char? end)
    {
        var url =
            $"tables/addColumn?databaseId={databaseId}" +
            $"&tableId={tableId}" +
            $"&name={Uri.EscapeDataString(name)}" +
            $"&type={Uri.EscapeDataString(type)}";

        if (start.HasValue && end.HasValue)
        {
            url += $"&start={Uri.EscapeDataString(start.Value.ToString())}" +
                   $"&end={Uri.EscapeDataString(end.Value.ToString())}";
        }

        await PostAsync<object>(url);
    }

    public async Task DeleteColumnAsync(Guid databaseId, Guid tableId, Guid columnId)
        => await DeleteAsync($"tables/deleteColumn?databaseId={databaseId}&tableId={tableId}&columnId={columnId}");

    public async Task IntersectTablesAsync(
        Guid databaseId,
        Guid tableAId,
        Guid tableBId,
        string resultName,
        List<string>? columns = null)
    {
        var query =
            $"tables/intersect?databaseId={databaseId}" +
            $"&tableAId={tableAId}" +
            $"&tableBId={tableBId}" +
            $"&resultTableName={Uri.EscapeDataString(resultName)}";

        if (columns is { Count: > 0 })
        {
            var colParam = Uri.EscapeDataString(string.Join(",", columns));
            query += $"&columns={colParam}";
        }

        await PostAsync<object>(query);
    }
}
