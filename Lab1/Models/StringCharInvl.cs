namespace Lab1.Models;


public class StringCharInvl
{
    public CharInvl Interval { get; set; }

    public bool Validate(string s) => s.All(c => Interval.Validate(c));
}
