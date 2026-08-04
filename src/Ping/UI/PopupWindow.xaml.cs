using System.Windows;

namespace Ping.UI;

/// <summary>
/// The little panel that appears when the bubble is clicked.
/// Two real actions - Add task (loud) and Upcoming (quiet) - plus a muted
/// footer row for the rare stuff. Click anywhere else and it melts away.
/// </summary>
public partial class PopupWindow : Window
{
    public event Action? AddTaskRequested;
    public event Action? UpcomingRequested;
    public event Action? SettingsRequested;
    public event Action? PrintAllTodayRequested;
    public event Action? HideRequested;
    public event Action? QuitRequested;

    public PopupWindow()
    {
        InitializeComponent();
        AddButton.Click += (_, _) => AddTaskRequested?.Invoke();
        UpcomingButton.Click += (_, _) => UpcomingRequested?.Invoke();
        SettingsButton.Click += (_, _) => SettingsRequested?.Invoke();
        PrintTodayButton.Click += (_, _) => PrintAllTodayRequested?.Invoke();
        HideButton.Click += (_, _) => HideRequested?.Invoke();
        QuitButton.Click += (_, _) => QuitRequested?.Invoke();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        try { Close(); } catch (InvalidOperationException) { /* already closing */ }
    }
}
