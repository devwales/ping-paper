using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ping.UI;

/// <summary>
/// The floating bubble. Drag it anywhere; a quiet click opens the popup.
/// Position, size, opacity and always-on-top come from settings and persist.
/// </summary>
public partial class BubbleWindow : Window
{
    private const double BaseSize = 72; // design size of Root grid

    public event Action? AddTaskRequested;
    public event Action? UpcomingRequested;
    public event Action? SettingsRequested;
    public event Action? PrintAllTodayRequested;
    public event Action? HideRequested;
    public event Action? QuitRequested;

    private PopupWindow? _popup;

    public BubbleWindow()
    {
        InitializeComponent();

        var menu = new ContextMenu();
        menu.Items.Add(MakeItem("Add task", () => AddTaskRequested?.Invoke()));
        menu.Items.Add(MakeItem("Upcoming", () => UpcomingRequested?.Invoke()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeItem("Print all for today", () => PrintAllTodayRequested?.Invoke()));
        menu.Items.Add(MakeItem("Settings", () => SettingsRequested?.Invoke()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeItem("Hide bubble", () => HideRequested?.Invoke()));
        menu.Items.Add(MakeItem("Quit Ping", () => QuitRequested?.Invoke()));
        ContextMenu = menu;

        Loaded += (_, _) =>
        {
            ApplyAppearance();
            RestorePosition();
        };
    }

    public void ApplyAppearance()
    {
        var s = App.Settings.Current;
        var scale = Math.Clamp(s.BubbleSize, 40, 96) / BaseSize;
        Scaler.ScaleX = scale;
        Scaler.ScaleY = scale;
        Opacity = Math.Clamp(s.BubbleOpacity, 0.4, 1.0);
        Topmost = s.AlwaysOnTop;
    }

    private void RestorePosition()
    {
        var s = App.Settings.Current;
        if (s.BubbleLeft is double left && s.BubbleTop is double top)
        {
            Left = Math.Clamp(left, 0, SystemParameters.VirtualScreenWidth - ActualWidth);
            Top = Math.Clamp(top, 0, SystemParameters.VirtualScreenHeight - ActualHeight);
        }
        else
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - ActualWidth - 28;
            Top = wa.Bottom - ActualHeight - 64;
        }
    }

    private void SavePosition()
    {
        var s = App.Settings.Current;
        s.BubbleLeft = Left;
        s.BubbleTop = Top;
        App.Settings.Save();
    }

    // DragMove() runs its own modal move loop and returns when the button is released, so the
    // whole gesture is decided here. It must NOT be combined with CaptureMouse() (capture
    // breaks the move loop). Drag-vs-click is judged by how far the window travelled: a real
    // drag keeps the new spot; a sloppy click (< 4 px) snaps back and toggles the popup.
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var startLeft = Left;
        var startTop = Top;
        try { DragMove(); } catch (InvalidOperationException) { /* not in a draggable state */ }

        if (Math.Abs(Left - startLeft) + Math.Abs(Top - startTop) >= 4)
        {
            SavePosition();
        }
        else
        {
            Left = startLeft;
            Top = startTop;
            TogglePopup();
        }
        e.Handled = true;
    }

    private void TogglePopup()
    {
        if (_popup != null)
        {
            _popup.Close();
            _popup = null;
            return;
        }

        _popup = new PopupWindow();
        _popup.AddTaskRequested += () => { ClosePopup(); AddTaskRequested?.Invoke(); };
        _popup.UpcomingRequested += () => { ClosePopup(); UpcomingRequested?.Invoke(); };
        _popup.SettingsRequested += () => { ClosePopup(); SettingsRequested?.Invoke(); };
        _popup.PrintAllTodayRequested += () => { ClosePopup(); PrintAllTodayRequested?.Invoke(); };
        _popup.HideRequested += () => { ClosePopup(); HideRequested?.Invoke(); };
        _popup.QuitRequested += () => { ClosePopup(); QuitRequested?.Invoke(); };
        _popup.Closed += (_, _) => _popup = null;
        UiPlacement.PlaceNear(_popup, this);
        _popup.Show();
        _popup.Activate();
    }

    private void ClosePopup()
    {
        _popup?.Close();
        _popup = null;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        // Clicking elsewhere shouldn't leave the popup hanging around.
        // (The popup handles its own lifetime; nothing to do for the bubble itself.)
    }

    private static MenuItem MakeItem(string text, Action onClick)
    {
        var item = new MenuItem { Header = text };
        item.Click += (_, _) => onClick();
        return item;
    }
}
