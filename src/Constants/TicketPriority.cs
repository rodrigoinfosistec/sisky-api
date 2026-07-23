namespace SiskyApi.Constants;

public static class TicketPriority
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Urgent = "urgent";

    public static readonly string[] All = [Low, Medium, High, Urgent];
}