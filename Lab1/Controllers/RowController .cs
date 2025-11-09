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
    public IActionResult AddRow(
    [FromQuery] Guid databaseId,
    [FromQuery] Guid tableId,
    [FromBody] Dictionary<string, object> values)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        var missingColumns = table.Columns.Select(c => c.Name).Except(values.Keys).ToList();
        if (missingColumns.Any())
            return BadRequest($"Missing values for columns: {string.Join(", ", missingColumns)}");

        foreach (var kvp in values)
        {
            var column = table.Columns.FirstOrDefault(c => c.Name == kvp.Key);
            if (column == null)
                return BadRequest($"Column '{kvp.Key}' does not exist in table '{table.Name}'");

            if (!ValidateColumnValue(column, kvp.Value, out var error))
                return BadRequest(new { message = error });
        }

        var row = new Row { Values = values };
        table.Rows.Add(row);
        _repository.Save(db);

        return Ok(new { message = "Row added", rowId = row.Id });
    }

    [HttpPut("update")]
    public IActionResult UpdateRow(
    [FromQuery] Guid databaseId,
    [FromQuery] Guid tableId,
    [FromQuery] Guid rowId,
    [FromBody] Dictionary<string, object> updatedValues)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null)
            return NotFound($"Table with Id '{tableId}' not found");

        var row = table.Rows.FirstOrDefault(r => r.Id == rowId);
        if (row == null)
            return NotFound($"Row with Id '{rowId}' not found");

        foreach (var kvp in updatedValues)
        {
            var column = table.Columns.FirstOrDefault(c => c.Name == kvp.Key);
            if (column == null)
                return BadRequest($"Column '{kvp.Key}' does not exist in table '{table.Name}'");

            if (!ValidateColumnValue(column, kvp.Value, out var error))
                return BadRequest(new { message = error });

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


    private bool ValidateColumnValue(Column column, object? value, out string? error)
    {
        error = null;

        if (value == null)
        {
            error = $"Value for column '{column.Name}' cannot be null";
            return false;
        }

        switch (column.Type)
        {
            case FieldType.Integer:
                if (value is int or long or short or byte)
                    return true;

                error = $"Expected integer for column '{column.Name}'";
                return false;

            case FieldType.Real:
                if (value is double or float or decimal or int or long)
                    return true;

                error = $"Expected real number for column '{column.Name}'";
                return false;

            case FieldType.Char:
                if (value is char) return true;
                if (value is string s1 && s1.Length == 1) return true;

                error = $"Expected single char for column '{column.Name}'";
                return false;

            case FieldType.String:
                if (value is string) return true;

                error = $"Expected string for column '{column.Name}'";
                return false;

            case FieldType.CharInvl:
                {
                    if (column.Interval == null)
                    {
                        error = $"Column '{column.Name}' has no interval configured";
                        return false;
                    }

                    char c;

                    if (value is char ch) c = ch;
                    else if (value is string s2 && s2.Length == 1) c = s2[0];
                    else
                    {
                        error = $"Expected single char for column '{column.Name}'";
                        return false;
                    }

                    if (!column.Interval.Validate(c))
                    {
                        error = $"Value '{c}' is out of range [{column.Interval.Start}-{column.Interval.End}] for column '{column.Name}'";
                        return false;
                    }

                    return true;
                }

            case FieldType.StringCharInvl:
                {
                    if (column.Interval == null)
                    {
                        error = $"Column '{column.Name}' has no interval configured";
                        return false;
                    }

                    if (value is not string s)
                    {
                        error = $"Expected string for column '{column.Name}'";
                        return false;
                    }

                    foreach (var ch in s)
                    {
                        if (!column.Interval.Validate(ch))
                        {
                            error = $"Character '{ch}' in value '{s}' is out of range [{column.Interval.Start}-{column.Interval.End}] for column '{column.Name}'";
                            return false;
                        }
                    }

                    return true;
                }

            default:
                error = $"Unknown type {column.Type}";
                return false;
        }
    }
}

