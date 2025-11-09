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
    [FromQuery] string type)
    {
        var table = GetTableById(databaseId, tableId, out var db);
        if (table == null)
            return NotFound($"Table with Id '{tableId}' not found");

        if (table.Columns.Any(c => c.Name == name))
            return Conflict($"Column '{name}' already exists");

        // Пробуем распарсить строку в enum FieldType
        if (!Enum.TryParse<FieldType>(type, true, out var parsedType))
            return BadRequest($"Unknown field type '{type}'. Valid types: {string.Join(", ", Enum.GetNames(typeof(FieldType)))}");

        var column = new Column
        {
            Name = name,
            Type = parsedType
        };

        table.Columns.Add(column);

        foreach (var row in table.Rows)
        {
            row.Values[column.Name] = GetDefaultForFieldType(column.Type);
        }

        _repository.Save(db);

        return Ok(new { message = $"Column '{name}' added with type '{parsedType}'", columnId = column.Id });
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