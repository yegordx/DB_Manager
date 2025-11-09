using Lab1.Models;

namespace Lab1.Desktop.Services;

public class ColumnService : ApiServiceBase
{
    public async Task<List<Column>> GetColumnsAsync(Guid databaseId, Guid tableId)
    {
        var table = await GetAsync<Table>($"tables?databaseId={databaseId}&tableId={tableId}");
        return table?.Columns ?? new List<Column>();
    }

    public async Task AddColumnAsync(
        Guid databaseId,
        Guid tableId,
        string name,
        FieldType type,
        char? start = null,
        char? end = null)
    {
        var typeStr = Uri.EscapeDataString(type.ToString());
        var url =
            $"tables/addColumn?databaseId={databaseId}" +
            $"&tableId={tableId}" +
            $"&name={Uri.EscapeDataString(name)}" +
            $"&type={typeStr}";

        if ((type == FieldType.CharInvl || type == FieldType.StringCharInvl)
            && start.HasValue && end.HasValue)
        {
            url += $"&start={Uri.EscapeDataString(start.Value.ToString())}" +
                   $"&end={Uri.EscapeDataString(end.Value.ToString())}";
        }

        await PostAsync<object>(url);
    }

    public async Task DeleteColumnAsync(Guid databaseId, Guid tableId, Guid columnId)
        => await DeleteAsync($"tables/deleteColumn?databaseId={databaseId}&tableId={tableId}&columnId={columnId}");
}
