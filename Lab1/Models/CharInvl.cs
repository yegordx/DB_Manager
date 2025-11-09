namespace Lab1.Models;

public class CharInvl
{
    public char Start { get; set; }
    public char End { get; set; }

    public bool Validate(char c) => c >= Start && c <= End;
}
