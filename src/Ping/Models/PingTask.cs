namespace Ping.Models;

/// <summary>
/// A single timed task. A task prints once, at (or just after) its scheduled time.
/// The 'source' column leaves room for a future companion app that only adds tasks.
/// </summary>
public class PingTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Text { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PrintedAt { get; set; }
    public bool Cancelled { get; set; }
    public string Source { get; set; } = "desktop";

    public bool IsPending => PrintedAt == null && !Cancelled;

    public string FriendlyDay
    {
        get
        {
            if (ScheduledAt.Date == DateTime.Today) return "Today";
            if (ScheduledAt.Date == DateTime.Today.AddDays(1)) return "Tomorrow";
            return ScheduledAt.ToString("dddd d MMM");
        }
    }

    public string FriendlyTime => ScheduledAt.ToString("HH:mm");
}
