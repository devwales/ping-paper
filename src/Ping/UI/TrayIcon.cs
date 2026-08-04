using Forms = System.Windows.Forms;

namespace Ping.UI;

/// <summary>
/// System tray presence, so Ping is reachable even when the bubble is hidden.
/// Also delivers quiet balloon messages (e.g. printer hiccups).
/// </summary>
public class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _toggleItem;

    public event Action? AddTaskRequested;
    public event Action? UpcomingRequested;
    public event Action? SettingsRequested;
    public event Action? ToggleBubbleRequested;
    public event Action? PrintAllTodayRequested;
    public event Action? QuitRequested;

    public TrayIcon()
    {
        var stream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/ping.ico"))?.Stream;
        var icon = stream != null
            ? new System.Drawing.Icon(stream)
            : System.Drawing.SystemIcons.Application;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Add task", null, (_, _) => AddTaskRequested?.Invoke());
        menu.Items.Add("Upcoming", null, (_, _) => UpcomingRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        _toggleItem = new Forms.ToolStripMenuItem("Show or hide bubble", null,
            (_, _) => ToggleBubbleRequested?.Invoke());
        menu.Items.Add(_toggleItem);
        menu.Items.Add("Print all for today", null, (_, _) => PrintAllTodayRequested?.Invoke());
        menu.Items.Add("Settings", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => QuitRequested?.Invoke());

        _icon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = "Ping",
            Visible = true,
            ContextMenuStrip = menu
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) ToggleBubbleRequested?.Invoke();
        };
    }

    public void ShowQuietMessage(string message)
    {
        try { _icon.ShowBalloonTip(3000, "Ping", message, Forms.ToolTipIcon.Info); }
        catch { /* balloons are best-effort */ }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
