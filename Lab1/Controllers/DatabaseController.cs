using Lab1.Models;
using Lab1.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lab1.Controllers;

[ApiController]
[Route("api/databases")]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseRepository _repository;

    public DatabaseController(DatabaseRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public IActionResult CreateDatabase([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Database name cannot be empty");

        var existingDatabases = _repository.GetAllDatabases();
        if (existingDatabases.Any(db => db.Name == name))
            return Conflict($"Database '{name}' already exists");

        var db = new Database { Name = name };
        _repository.Save(db);

        return Ok(new { message = $"Database '{name}' created successfully", databaseId = db.Id });
    }

    [HttpGet]
    public IActionResult GetDatabase([FromQuery] Guid databaseId)
    {
        var db = _repository.GetById(databaseId);
        if (db == null)
            return NotFound($"Database with Id '{databaseId}' not found");

        return Ok(db);
    }

    [HttpDelete]
    public IActionResult DeleteDatabaseAsync([FromQuery] Guid databaseId)
    {
        var db = _repository.GetById(databaseId);
        if (db == null)
            return NotFound($"Database with Id '{databaseId}' not found");

        _repository.Delete(databaseId);
        return Ok(new { message = $"Database '{db.Name}' deleted successfully" });
    }

    [HttpGet("list")]
    public IActionResult GetAllDatabases()
    {
        var databases = _repository.GetAllDatabases();
        var result = databases.Select(db => new { db.Id, db.Name }).ToList();
        return Ok(result);
    }
}