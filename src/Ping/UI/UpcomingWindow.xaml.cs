using System.Windows;

namespace Ping.UI;

/// <summary>
/// A deliberately small peek at what's coming - never the whole list.
/// Click anywhere else and it's gone.
/// </summary>
public partial class UpcomingWindow : Window
{
    private const int MaxShown = 5;

    public UpcomingWindow()
    {
        InitializeComponent();

        var upcoming = App.Tasks.GetUpcoming(MaxShown);
        List.ItemsSource = upcoming;
        EmptyNote.Visibility = upcoming.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        PrintAllButton.Click += (_, _) =>
        {
            var (printed, message) = App.Scheduler.PrintAllForToday();
            StatusNote.Text = message;
            if (printed) PrintAllButton.IsEnabled = false;
        };
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        try { Close(); } catch (InvalidOperationException) { /* already closing */ }
    }
}
