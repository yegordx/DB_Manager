namespace Lab1.Models;

public class Database
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Table> Tables { get; set; } = new();

    public Table GetTable(string name) => Tables.FirstOrDefault(t => t.Name == name);
}
