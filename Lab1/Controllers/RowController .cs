using Lab1.Models;
using Lab1.Repositories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rows")]
public class RowController : ControllerBase
{
    private readonly DatabaseRepository _repository;

    public RowController(DatabaseRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("add")]
    public IActionResult AddRow([FromQuery] Guid databaseId, [FromQuery] Guid tableId, [FromBody] Dictionary<string, object> values)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        var missingColumns = table.Columns.Select(c => c.Name).Except(values.Keys).ToList();
        if (missingColumns.Any())
            return BadRequest($"Missing values for columns: {string.Join(", ", missingColumns)}");

        foreach (var column in table.Columns)
        {
            if (!values.TryGetValue(column.Name, out var val)) continue;
            if (!ValidateColumnType(column.Type, val))
                return BadRequest($"Invalid type for column '{column.Name}'. Expected {column.Type}, got {val?.GetType().Name ?? "null"}");
        }

        var row = new Row { Values = values };
        table.Rows.Add(row);
        _repository.Save(db);

        return Ok(new { message = "Row added", rowId = row.Id });
    }

    [HttpPut("update")]
    public IActionResult UpdateRow([FromQuery] Guid databaseId, [FromQuery] Guid tableId, [FromQuery] Guid rowId, [FromBody] Dictionary<string, object> updatedValues)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        var row = table.Rows.FirstOrDefault(r => r.Id == rowId);
        if (row == null) return NotFound($"Row with Id '{rowId}' not found");

        foreach (var kvp in updatedValues)
        {
            var column = table.Columns.FirstOrDefault(c => c.Name == kvp.Key);
            if (column == null)
                return BadRequest($"Column '{kvp.Key}' does not exist in table '{table.Name}'");

            if (!ValidateColumnType(column.Type, kvp.Value))
                return BadRequest($"Invalid type for column '{column.Name}'. Expected {column.Type}, got {kvp.Value?.GetType().Name ?? "null"}");

            row.Values[kvp.Key] = kvp.Value;
        }

        _repository.Save(db);

        return Ok(new { message = "Row updated", rowId = row.Id });
    }

    [HttpDelete("delete")]
    public IActionResult DeleteRow([FromQuery] Guid databaseId, [FromQuery] Guid tableId, [FromQuery] Guid rowId)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        var row = table.Rows.FirstOrDefault(r => r.Id == rowId);
        if (row == null) return NotFound($"Row with Id '{rowId}' not found");

        table.Rows.Remove(row);
        _repository.Save(db);

        return Ok(new { message = "Row deleted", rowId = row.Id });
    }


    private Table? GetTableById(Guid databaseId, Guid tableId, out Database? db)
    {
        db = _repository.GetById(databaseId);
        if (db == null) return null;
        return db.Tables.FirstOrDefault(t => t.Id == tableId);
    }

    private bool ValidateColumnType(FieldType type, object? value)
    {
        if (value == null)
            return false;

        return type switch
        {
            FieldType.Integer => value is int || value is long || value is short || value is byte,
            FieldType.Real => value is double || value is float || value is decimal || value is int || value is long,
            FieldType.Char => value is char || (value is string s && s.Length == 1),
            FieldType.String => value is string,
            FieldType.CharInvl => value is char || (value is string s && s.Length == 1),
            FieldType.StringCharInvl => value is string,
            _ => false
        };
    }

    [HttpPost("intersect")]
    public IActionResult IntersectTables([FromQuery] Guid databaseId, [FromQuery] Guid tableAId, [FromQuery] Guid tableBId, [FromQuery] string resultTableName)
    {
        var db = _repository.GetById(databaseId);
        if (db == null) return NotFound($"Database with Id '{databaseId}' not found");

        var tableA = db.Tables.FirstOrDefault(t => t.Id == tableAId);
        var tableB = db.Tables.FirstOrDefault(t => t.Id == tableBId);

        if (tableA == null || tableB == null)
            return NotFound("One or both tables not found");

        // Проверка структуры таблиц
        if (!tableA.HasSameStructure(tableB))
            return BadRequest("Tables have different structures and cannot be intersected");

        // Создаем новую таблицу для результата
        var resultTable = new Table
        {
            Name = resultTableName,
            Columns = tableA.Columns.Select(c => new Column { Id = c.Id, Name = c.Name, Type = c.Type }).ToList(),
            Rows = tableA.Rows
                    .Where(rowA => tableB.Rows.Any(rowB => AreRowsEqual(rowA, rowB, tableA.Columns)))
                    .Select(row => new Row { Values = new Dictionary<string, object>(row.Values) })
                    .ToList()
        };

        db.Tables.Add(resultTable);
        _repository.Save(db);

        return Ok(new { message = $"Intersection table '{resultTableName}' created", tableId = resultTable.Id });
    }

    private bool AreRowsEqual(Row a, Row b, List<Column> columns)
    {
        foreach (var column in columns)
        {
            if (!a.Values.TryGetValue(column.Name, out var valA)) return false;
            if (!b.Values.TryGetValue(column.Name, out var valB)) return false;
            if (!Equals(valA, valB)) return false;
        }
        return true;
    }
}

