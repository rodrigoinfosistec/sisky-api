namespace SiskyApi.Constants;

public static class TicketStatus
{
    public const string Open = "open";
    public const string InProgress = "in_progress";
    public const string Resolved = "resolved";
    public const string Closed = "closed";

    public static readonly string[] All = [Open, InProgress, Resolved, Closed];
}