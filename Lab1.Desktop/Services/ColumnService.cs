using Lab1.Models;

namespace Lab1.Desktop.Services;

public class ColumnService : ApiServiceBase
{
    public async Task<List<Column>> GetColumnsAsync(Guid databaseId, Guid tableId)
    {
        var table = await GetAsync<Table>($"tables?databaseId={databaseId}&tableId={tableId}");
        return table?.Columns ?? new List<Column>();
    }

    public async Task AddColumnAsync(Guid databaseId, Guid tableId, string name, FieldType type)
    {
        var typeStr = Uri.EscapeDataString(type.ToString());
        await PostAsync<object>($"tables/addColumn?databaseId={databaseId}&tableId={tableId}&name={Uri.EscapeDataString(name)}&type={typeStr}");
    }

    public async Task DeleteColumnAsync(Guid databaseId, Guid tableId, Guid columnId)
        => await DeleteAsync($"tables/deleteColumn?databaseId={databaseId}&tableId={tableId}&columnId={columnId}");
}
