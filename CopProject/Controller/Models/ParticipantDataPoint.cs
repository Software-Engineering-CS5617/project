namespace Controller.Models;

public sealed class ParticipantDataPoint
{
    public double Seconds { get; set; }
    public string TimeLabel { get; set; } = "00:00";
    public int Count { get; set; }
}