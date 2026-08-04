using System.Collections.Concurrent;
using System.IO;
using Ping.Data;
using Ping.Models;

namespace Ping.Printing;

/// <summary>
/// Background print queue. Printing never blocks the UI. If the printer is
/// offline or busy, jobs retry a few times with a pause between attempts, and
/// a calm message is raised if it still can't get through.
/// </summary>
public class PrintQueue : IDisposable
{
    private record Job(byte[] Payload, string Kind, string? TaskId, int Attempt);

    private readonly SettingsStore _settings;
    private readonly BlockingCollection<Job> _jobs = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryPause = TimeSpan.FromSeconds(20);

    /// <summary>Raised (on UI thread) with gentle status messages, e.g. "Printer not found - check USB connection".</summary>
    public event Action<string>? StatusChanged;

    public PrintQueue(SettingsStore settings)
    {
        _settings = settings;
        _worker = Task.Run(ProcessAsync);
    }

    public IPrintTransport CurrentTransport() => BuildTransport(_settings.Current);

    public static IPrintTransport BuildTransport(AppSettings s) => s.Connection switch
    {
        PrinterConnection.Network => new NetworkTransport(s.NetworkHost, s.NetworkPort),
        PrinterConnection.File => new FileTransport(Path.Combine(App.DataFolder, "receipts")),
        _ => new SpoolerTransport(string.IsNullOrWhiteSpace(s.PrinterName)
            ? PrinterDiscovery.BestGuess() ?? s.PrinterName
            : s.PrinterName),
    };

    public void EnqueueSingle(Models.PingTask task, bool missed = false)
    {
        var payload = ReceiptComposer.SingleTask(task, _settings.Current.PaperWidthMm, missed);
        _jobs.Add(new Job(payload, "single", task.Id, 0));
    }

    public void EnqueueBatch(IReadOnlyList<Models.PingTask> tasks, string kind)
    {
        if (tasks.Count == 0) return;
        var width = _settings.Current.PaperWidthMm;
        var payload = kind switch
        {
            "catchup" => ReceiptComposer.MissedCatchUp(tasks, width),
            "today" => ReceiptComposer.TodayPrintAll(tasks, width),
            _ => ReceiptComposer.Batch(tasks, "Ping", width),
        };
        var taskIds = string.Join(",", tasks.Select(t => t.Id));
        _jobs.Add(new Job(payload, kind, taskIds, 0));
    }

    /// <summary>Test print; pass a draft settings object to try unsaved settings from the Settings window.</summary>
    public void EnqueueTest(AppSettings? draft = null)
    {
        var effective = draft ?? _settings.Current;
        var transport = BuildTransport(effective);
        var payload = ReceiptComposer.TestPrint(effective.PaperWidthMm, transport.Describe());
        _jobs.Add(new Job(payload, "test", null, 0));
    }

    private async Task ProcessAsync()
    {
        foreach (var job in _jobs.GetConsumingEnumerable(_cts.Token))
        {
            try
            {
                var transport = CurrentTransport();
                await transport.SendAsync(job.Payload, _cts.Token);
                MarkDone(job, "ok", null);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (InvalidOperationException ex)
            {
                if (job.Attempt + 1 < MaxAttempts)
                {
                    _jobs.Add(job with { Attempt = job.Attempt + 1 });
                    try { await Task.Delay(RetryPause, _cts.Token); }
                    catch (OperationCanceledException) { return; }
                }
                else
                {
                    MarkDone(job, "failed", ex.Message);
                    Raise(ex.Message);
                }
            }
        }
    }

    private void MarkDone(Job job, string result, string? detail)
    {
        try
        {
            var store = new TaskStore();
            if (job.TaskId != null)
            {
                foreach (var id in job.TaskId.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    if (result == "ok") store.MarkPrinted(id);
                store.LogPrint(job.TaskId, job.Kind, result, detail);
            }
            else
            {
                store.LogPrint(null, job.Kind, result, detail);
            }
        }
        catch
        {
            // Logging must never take the queue down.
        }
    }

    private void Raise(string message)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => StatusChanged?.Invoke(message));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _jobs.CompleteAdding();
        try { _worker.Wait(TimeSpan.FromSeconds(2)); } catch { /* shutting down */ }
        _jobs.Dispose();
        _cts.Dispose();
    }
}
