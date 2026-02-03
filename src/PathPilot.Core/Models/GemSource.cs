namespace PathPilot.Core.Models;

public class GemSource
{
    public SourceType Type { get; set; } = SourceType.Vendor;
    public int Act { get; set; }
    public string? QuestName { get; set; }
    public string? VendorName { get; set; }
    public string? Classes { get; set; }
    public List<string> AvailableForClasses { get; set; } = new();
}

public enum SourceType
{
    QuestReward,
    Vendor,
    Siosa,
    Lilly,
    Drop
}
