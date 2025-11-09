using Newtonsoft.Json;
using Lab1.Models;

namespace Lab1.Repositories;

public class DatabaseRepository
{
    private readonly string _baseFolder;

    public DatabaseRepository(IConfiguration config)
    {
        _baseFolder = config["Database:FolderPath"] ?? "Databases";

        if (!Directory.Exists(_baseFolder))
            Directory.CreateDirectory(_baseFolder);
    }

    public Database? GetById(Guid databaseId)
    {
        var allDatabases = GetAllDatabases();
        return allDatabases.FirstOrDefault(db => db.Id == databaseId);
    }

    public List<Database> GetAllDatabases()
    {
        var files = Directory.GetFiles(_baseFolder, "*.json");
        var result = new List<Database>();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var db = JsonConvert.DeserializeObject<Database>(json);
                if (db != null)
                    result.Add(db);
            }
            catch
            {
            }
        }

        return result;
    }

    public void Save(Database db)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        var filePath = GetDatabasePath(db);
        var json = JsonConvert.SerializeObject(db, Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

        File.WriteAllText(filePath, json);
    }

    public void Delete(Guid databaseId)
    {
        var db = GetById(databaseId);
        if (db == null) return;

        var filePath = GetDatabasePath(db);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private string GetDatabasePath(Database db)
    {
        return Path.Combine(_baseFolder, $"{db.Id}.json");
    }
}
