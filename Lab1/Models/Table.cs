namespace Lab1.Models;

public class Table
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; set; } = new();
    public List<Row> Rows { get; set; } = new();

    public bool HasSameStructure(Table other)
    {
        if (other == null) return false;

        if (Columns.Count != other.Columns.Count) return false;

        for (int i = 0; i < Columns.Count; i++)
        {
            var a = Columns[i];
            var b = other.Columns[i];

            if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal))
                return false;

            if (a.Type != b.Type)
                return false;

            if (a.Type is FieldType.CharInvl or FieldType.StringCharInvl)
            {
                if (a.Interval == null || b.Interval == null)
                    return false;

                if (a.Interval.Start != b.Interval.Start ||
                    a.Interval.End != b.Interval.End)
                    return false;
            }
        }

        return true;
    }
}
