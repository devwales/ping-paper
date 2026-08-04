using Microsoft.Win32;
using Ping.Data;
using Ping.Printing;

namespace Ping.Scheduling;

/// <summary>
/// Watches for tasks whose time has come and hands them to the print queue.
///
/// Catch-up behaviour (on app start and on wake from sleep):
///   - tasks from today whose time already passed  -> one grouped "While you were away" receipt
///   - tasks from previous days, never printed     -> quietly retired (no paper storm)
///
/// "Print all for today" prints today's remaining tasks on one receipt and marks them done.
/// </summary>
public class TaskScheduler
{
    private readonly TaskStore _tasks;
    private readonly SettingsStore _settings;
    private readonly PrintQueue _printer;
    private readonly HashSet<string> _enqueued = new();
    private readonly object _lock = new();
    private System.Threading.Timer? _timer;

    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(15);

    public TaskScheduler(TaskStore tasks, SettingsStore settings, PrintQueue printer)
    {
        _tasks = tasks;
        _settings = settings;
        _printer = printer;
    }

    public void Start()
    {
        _timer = new System.Threading.Timer(_ => OnTick(), null, Tick, Tick);
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public void Stop()
    {
        _timer?.Dispose();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            RunCatchUp();
    }

    private void OnTick()
    {
        try
        {
            var now = DateTime.Now;
            foreach (var task in _tasks.GetDue(now))
            {
                // Catch-up handles missed ones; the timer only prints tasks due "right now"
                // (within the last few minutes) so a task added while Ping runs prints on time,
                // and anything older was already dealt with by RunCatchUp.
                if (task.ScheduledAt.Date != now.Date) continue;
                if (now - task.ScheduledAt > TimeSpan.FromMinutes(2)) continue;

                lock (_lock)
                {
                    if (!_enqueued.Add(task.Id)) continue;
                }
                _printer.EnqueueSingle(task);
            }
        }
        catch
        {
            // A bad tick must never kill the scheduler.
        }
    }

    /// <summary>
    /// Print what was missed while Ping (or the PC) was off. Runs on startup and on wake.
    /// </summary>
    public void RunCatchUp()
    {
        try
        {
            var expired = _tasks.ExpireBeforeToday();
            if (expired > 0)
                _tasks.LogPrint(null, "expire", "ok", $"{expired} old task(s) quietly retired");

            if (!_settings.Current.CatchUpEnabled) return;

            var now = DateTime.Now;
            var missed = _tasks.GetDue(now)
                .Where(t => t.ScheduledAt.Date == now.Date)
                .ToList();
            if (missed.Count == 0) return;

            lock (_lock)
            {
                foreach (var t in missed) _enqueued.Add(t.Id);
            }

            if (missed.Count == 1)
                _printer.EnqueueSingle(missed[0], missed: true);
            else
                _printer.EnqueueBatch(missed, "catchup");
        }
        catch
        {
            // Never let catch-up crash startup.
        }
    }

    /// <summary>
    /// Prints every remaining task scheduled for today on a single receipt,
    /// and (via the print queue) marks them as printed.
    /// </summary>
    public (bool printed, string message) PrintAllForToday()
    {
        var remaining = _tasks.GetRemainingToday();
        if (remaining.Count == 0)
            return (false, "Nothing left for today.");

        lock (_lock)
        {
            foreach (var t in remaining) _enqueued.Add(t.Id);
        }
        _printer.EnqueueBatch(remaining, "today");
        return (true, remaining.Count == 1 ? "Printing 1 task." : $"Printing {remaining.Count} tasks.");
    }
}
