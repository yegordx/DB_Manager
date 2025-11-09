namespace Lab1.Models;

public class Row
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Dictionary<string, object> Values { get; set; } = new();
}