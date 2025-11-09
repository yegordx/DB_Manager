namespace Lab1.Models;

public class Column
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public FieldType Type { get; set; }

    public CharInvl? Interval { get; set; }
}
