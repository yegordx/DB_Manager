namespace Lab1.Models;

public class Table
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; set; } = new();
    public List<Row> Rows { get; set; } = new();

    public bool HasSameStructure(Table other)
    {
        if (Columns.Count != other.Columns.Count) return false;
        for (int i = 0; i < Columns.Count; i++)
            if (Columns[i].Type != other.Columns[i].Type) return false;
        return true;
    }
}