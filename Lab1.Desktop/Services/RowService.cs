using Lab1.Models;

namespace Lab1.Desktop.Services;

public class RowService : ApiServiceBase
{
    public async Task<List<Row>> GetRowsAsync(Guid databaseId, Guid tableId)
    {
        var table = await GetAsync<Table>($"tables?databaseId={databaseId}&tableId={tableId}");
        return table?.Rows ?? new List<Row>();
    }

    public async Task AddRowAsync(Guid databaseId, Guid tableId, Dictionary<string, object> values)
        => await PostAsync<object>($"rows/add?databaseId={databaseId}&tableId={tableId}", values);

    public async Task UpdateRowAsync(Guid databaseId, Guid tableId, Guid rowId, Dictionary<string, object> updatedValues)
        => await PutAsync<object>($"rows/update?databaseId={databaseId}&tableId={tableId}&rowId={rowId}", updatedValues);

    public async Task DeleteRowAsync(Guid databaseId, Guid tableId, Guid rowId)
        => await DeleteAsync($"rows/delete?databaseId={databaseId}&tableId={tableId}&rowId={rowId}");

    public async Task IntersectTablesAsync(Guid databaseId, Guid tableAId, Guid tableBId, string resultName)
        => await PostAsync<object>($"rows/intersect?databaseId={databaseId}&tableAId={tableAId}&tableBId={tableBId}&resultTableName={Uri.EscapeDataString(resultName)}");
}
