using Lab1.Models;
using Lab1.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lab1.Controllers;

[ApiController]
[Route("api/tables")]
public class TableController : ControllerBase
{
    private readonly DatabaseRepository _repository;

    public TableController(DatabaseRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("create")]
    public IActionResult CreateTable([FromQuery] Guid databaseId, [FromQuery] string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return BadRequest("Table name cannot be empty");

        var db = _repository.GetById(databaseId);
        if (db == null) return NotFound($"Database with Id '{databaseId}' not found");

        if (db.Tables.Any(t => t.Name == tableName))
            return Conflict($"Table '{tableName}' already exists in database '{db.Name}'");

        var table = new Table
        {
            Name = tableName
        };

        db.Tables.Add(table);
        _repository.Save(db);

        return Ok(new { message = $"Table '{tableName}' created", tableId = table.Id });
    }

    [HttpGet]
    public IActionResult GetTable([FromQuery] Guid databaseId, [FromQuery] Guid tableId)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        return Ok(table);
    }

    [HttpDelete]
    public IActionResult DeleteTable([FromQuery] Guid databaseId, [FromQuery] Guid tableId)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        db.Tables.Remove(table);
        _repository.Save(db);

        return Ok(new { message = $"Table deleted successfully", tableId = tableId });
    }

    [HttpGet("list")]
    public IActionResult GetAllTables([FromQuery] Guid databaseId)
    {
        var db = _repository.GetById(databaseId);
        if (db == null) return NotFound($"Database with Id '{databaseId}' not found");

        var tables = db.Tables.Select(t => new { t.Id, t.Name }).ToList();
        return Ok(tables);
    }

    [HttpPost("addColumn")]
    public IActionResult AddColumn(
    [FromQuery] Guid databaseId,
    [FromQuery] Guid tableId,
    [FromQuery] string name,
    [FromQuery] string type,
    [FromQuery] string? start = null,
    [FromQuery] string? end = null)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null)
            return NotFound($"Table with Id '{tableId}' not found");

        if (table.Columns.Any(c => c.Name == name))
            return Conflict($"Column '{name}' already exists");

        if (!Enum.TryParse<FieldType>(type, true, out var parsedType))
            return BadRequest($"Unknown field type '{type}'");

        var column = new Column
        {
            Name = name,
            Type = parsedType
        };

        if (parsedType == FieldType.CharInvl || parsedType == FieldType.StringCharInvl)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                return BadRequest("For CharInvl or StringCharInvl you must specify 'start' and 'end' characters.");

            if (start.Length != 1 || end.Length != 1)
                return BadRequest("'start' and 'end' must be single characters.");

            var startChar = start[0];
            var endChar = end[0];

            if (startChar > endChar)
                return BadRequest("'start' must be <= 'end'.");

            column.Interval = new CharInvl { Start = startChar, End = endChar };
        }

        table.Columns.Add(column);

        foreach (var row in table.Rows)
            row.Values[column.Name] = GetDefaultForFieldType(column.Type);

        _repository.Save(db);

        return Ok(new
        {
            message = $"Column '{name}' added with type '{parsedType}'",
            columnId = column.Id
        });
    }


    [HttpDelete("deleteColumn")]
    public IActionResult DeleteColumn([FromQuery] Guid databaseId, [FromQuery] Guid tableId, [FromQuery] Guid columnId)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null) return NotFound($"Table with Id '{tableId}' not found");

        var column = table.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null) return NotFound($"Column with Id '{columnId}' not found");

        table.Columns.Remove(column);

        foreach (var row in table.Rows)
        {
            row.Values.Remove(column.Name);
        }

        _repository.Save(db);

        return Ok(new { message = $"Column '{column.Name}' deleted", columnId = columnId });
    }

    [HttpPost("intersect")]
    public IActionResult IntersectTables(
    [FromQuery] Guid databaseId,
    [FromQuery] Guid tableAId,
    [FromQuery] Guid tableBId,
    [FromQuery] string resultTableName,
    [FromQuery] string? columns = null)
    {
        var db = _repository.GetById(databaseId);
        if (db == null)
            return NotFound($"Database with Id '{databaseId}' not found");

        var tableA = db.Tables.FirstOrDefault(t => t.Id == tableAId);
        var tableB = db.Tables.FirstOrDefault(t => t.Id == tableBId);

        if (tableA == null || tableB == null)
            return NotFound("One or both tables not found");

        List<string>? keyColumns = null;
        if (!string.IsNullOrWhiteSpace(columns))
        {
            keyColumns = columns
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToList();

            if (keyColumns.Count == 0)
                keyColumns = null;
        }

        List<Column> compareColumns;

        if (keyColumns == null)
        {
            var err = ValidateFullStructure(tableA, tableB, out compareColumns);
            if (err != null)
                return BadRequest(err);
        }
        else
        {
            var err = ValidateKeyColumns(tableA, tableB, keyColumns, out compareColumns);
            if (err != null)
                return BadRequest(err);
        }


        var resultTable = new Table
        {
            Name = resultTableName,
            Columns = tableA.Columns
                .Select(c => new Column
                {
                    Id = Guid.NewGuid(),          
                    Name = c.Name,
                    Type = c.Type,
                    Interval = c.Interval == null
                        ? null
                        : new CharInvl { Start = c.Interval.Start, End = c.Interval.End }
                })
                .ToList()
        };

        foreach (var rowA in tableA.Rows)
        {
            bool existsInB = tableB.Rows.Any(rowB => RowsEqual(rowA, rowB, compareColumns));

            if (existsInB)
            {
                resultTable.Rows.Add(new Row
                {
                    Values = new Dictionary<string, object>(rowA.Values)
                });
            }
        }

        db.Tables.Add(resultTable);
        _repository.Save(db);

        return Ok(new
        {
            message = $"Intersection table '{resultTableName}' created",
            tableId = resultTable.Id
        });
    }

    private string? ValidateFullStructure(Table a, Table b, out List<Column> compareColumns)
    {
        compareColumns = new List<Column>();

        if (a.Columns.Count != b.Columns.Count)
            return $"Tables have different column count: {a.Columns.Count} vs {b.Columns.Count}.";

        for (int i = 0; i < a.Columns.Count; i++)
        {
            var ca = a.Columns[i];
            var cb = b.Columns[i];

            if (!string.Equals(ca.Name, cb.Name, StringComparison.Ordinal))
                return $"Column #{i + 1} name mismatch: '{ca.Name}' vs '{cb.Name}'.";

            if (ca.Type != cb.Type)
                return $"Column '{ca.Name}' type mismatch: {ca.Type} vs {cb.Type}.";

            if (!IntervalsEqual(ca, cb))
                return $"Column '{ca.Name}' interval mismatch.";

            compareColumns.Add(ca);
        }

        return null;
    }

    private string? ValidateKeyColumns(
        Table a,
        Table b,
        List<string> keyColumns,
        out List<Column> compareColumns)
    {
        compareColumns = new List<Column>();

        foreach (var name in keyColumns)
        {
            var ca = a.Columns.FirstOrDefault(c => c.Name == name);
            if (ca == null)
                return $"Table '{a.Name}' does not contain column '{name}'.";

            var cb = b.Columns.FirstOrDefault(c => c.Name == name);
            if (cb == null)
                return $"Table '{b.Name}' does not contain column '{name}'.";

            if (ca.Type != cb.Type)
                return $"Column '{name}' type mismatch: {ca.Type} vs {cb.Type}.";

            if (!IntervalsEqual(ca, cb))
                return $"Column '{name}' interval mismatch.";

            compareColumns.Add(ca);
        }

        return null;
    }

    private bool IntervalsEqual(Column a, Column b)
    {
        if (a.Type != FieldType.CharInvl && a.Type != FieldType.StringCharInvl)
            return true;

        if (a.Interval == null && b.Interval == null) return true;
        if (a.Interval == null || b.Interval == null) return false;

        return a.Interval.Start == b.Interval.Start &&
               a.Interval.End == b.Interval.End;
    }

    private bool RowsEqual(Row a, Row b, IEnumerable<Column> columns)
    {
        foreach (var col in columns)
        {
            if (!a.Values.TryGetValue(col.Name, out var va)) return false;
            if (!b.Values.TryGetValue(col.Name, out var vb)) return false;

            if (!Equals(va, vb)) return false;
        }

        return true;
    }

    private bool AreColumnTypesCompatible(Column a, Column b, out string error)
    {
        error = string.Empty;

        if (a.Type != b.Type)
        {
            error = $"Column '{a.Name}' type mismatch: {a.Type} vs {b.Type}.";
            return false;
        }

        if (a.Type is FieldType.CharInvl or FieldType.StringCharInvl)
        {
            if (a.Interval == null || b.Interval == null)
            {
                error = $"Column '{a.Name}' of type {a.Type} must have interval in both tables.";
                return false;
            }

            if (a.Interval.Start != b.Interval.Start || a.Interval.End != b.Interval.End)
            {
                error =
                    $"Column '{a.Name}' interval mismatch: [{a.Interval.Start}-{a.Interval.End}] vs [{b.Interval.Start}-{b.Interval.End}].";
                return false;
            }
        }

        return true;
    }

    private sealed class RowKey
    {
        public object?[] Values { get; }

        private RowKey(object?[] values) => Values = values;

        public static RowKey FromRow(Row row, IReadOnlyList<Column> columns)
        {
            var vals = new object?[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                row.Values.TryGetValue(columns[i].Name, out var v);
                vals[i] = v;
            }

            return new RowKey(vals);
        }
    }

    private sealed class RowKeyComparer : IEqualityComparer<RowKey>
    {
        public bool Equals(RowKey? x, RowKey? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Values.Length != y.Values.Length) return false;

            for (int i = 0; i < x.Values.Length; i++)
            {
                if (!Equals(x.Values[i], y.Values[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(RowKey obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var v in obj.Values)
                {
                    hash = hash * 31 + (v?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }

    private Table? GetTableById(Guid databaseId, Guid tableId, out Database? db)
    {
        db = _repository.GetById(databaseId);
        if (db == null) return null;

        return db.Tables.FirstOrDefault(t => t.Id == tableId);
    }

    private object? GetDefaultForFieldType(FieldType type)
    {
        return type switch
        {
            FieldType.Integer => 0,
            FieldType.Real => 0.0,
            FieldType.Char => '\0',
            FieldType.String => string.Empty,
            FieldType.CharInvl => '\0',
            FieldType.StringCharInvl => string.Empty,
            _ => null
        };
    }
}