using System.IO;
using System.Windows;
using Ping.Data;
using Ping.Printing;
using Ping.UI;

namespace Ping;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;

    public static TaskStore Tasks { get; private set; } = null!;
    public static SettingsStore Settings { get; private set; } = null!;
    public static PrintQueue Printer { get; private set; } = null!;
    public static Scheduling.TaskScheduler Scheduler { get; private set; } = null!;

    private TrayIcon? _tray;
    private BubbleWindow? _bubble;

    public static string DataFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ping");

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);

        _mutex = new Mutex(true, @"Local\Ping.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // Ping is already running - just leave quietly.
            Shutdown();
            return;
        }

        Directory.CreateDirectory(DataFolder);

        Database.Initialize(Path.Combine(DataFolder, "ping.db"));
        Settings = new SettingsStore();
        Tasks = new TaskStore();

        Printer = new PrintQueue(Settings);
        Printer.StatusChanged += msg => _tray?.ShowQuietMessage(msg);

        Scheduler = new Scheduling.TaskScheduler(Tasks, Settings, Printer);
        Scheduler.Start();

        _tray = new TrayIcon();
        _tray.AddTaskRequested += () => ShowAddTask();
        _tray.UpcomingRequested += () => ShowUpcoming();
        _tray.SettingsRequested += () => ShowSettings();
        _tray.ToggleBubbleRequested += () => ToggleBubble();
        _tray.PrintAllTodayRequested += () => Scheduler.PrintAllForToday();
        _tray.QuitRequested += () => BeginShutdown();

        if (Settings.Current.ShowBubbleOnStart)
            ShowBubble();

        // Print anything missed while the app or PC was off.
        Scheduler.RunCatchUp();

        base.OnStartup(e);
    }

    public void ShowBubble()
    {
        if (_bubble == null)
        {
            _bubble = new BubbleWindow();
            _bubble.AddTaskRequested += () => ShowAddTask(_bubble);
            _bubble.UpcomingRequested += () => ShowUpcoming(_bubble);
            _bubble.SettingsRequested += () => ShowSettings();
            _bubble.PrintAllTodayRequested += () => Scheduler.PrintAllForToday();
            _bubble.HideRequested += () => _bubble.Hide();
            _bubble.QuitRequested += () => BeginShutdown();
            _bubble.Closed += (_, _) => _bubble = null;
        }
        _bubble.Show();
        _bubble.Activate();
    }

    public void ToggleBubble()
    {
        if (_bubble == null || !_bubble.IsVisible)
            ShowBubble();
        else
            _bubble.Hide();
    }

    private void ShowAddTask(Window? anchor = null)
    {
        var win = new AddTaskWindow();
        if (anchor != null) UiPlacement.PlaceNear(win, anchor);
        else win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        win.ShowDialog();
    }

    private void ShowUpcoming(Window? anchor = null)
    {
        var win = new UpcomingWindow();
        if (anchor != null) UiPlacement.PlaceNear(win, anchor);
        else win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        win.Show();
    }

    private void ShowSettings()
    {
        var win = new SettingsWindow();
        win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        win.ShowDialog();
        _bubble?.ApplyAppearance();
    }

    private void BeginShutdown()
    {
        Scheduler.Stop();
        Printer.Dispose();
        _tray?.Dispose();
        _bubble?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.Handled = true; // keep the little helper alive; a dead reminder reminds no one
        System.Windows.MessageBox.Show(
            "Something went wrong, but Ping is still running.\nDetails were saved to crash.log in:\n" + DataFolder,
            "Ping", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private static void LogCrash(Exception? ex)
    {
        try
        {
            File.AppendAllText(Path.Combine(DataFolder, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { /* logging must never take the app down */ }
    }
}
