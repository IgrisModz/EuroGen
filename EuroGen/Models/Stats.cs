namespace EuroGen.Models;

public class Stats
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int NumberOfOutput { get; set; }

    public required string PercentOfOutput { get; set; }

    public required string LastRelease { get; set; }
}