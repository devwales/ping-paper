using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Ping.UI;

/// <summary>
/// Add a task in three taps: words, day, time. Time blocks stay inside your
/// working hours, so there's nothing to type and nothing to get wrong.
/// </summary>
public partial class AddTaskWindow : Window
{
    private DateTime _selectedDate;
    private TimeOnly? _selectedTime;
    private ToggleButton? _pickDayChip;
    private readonly List<ToggleButton> _timeChips = new();

    public AddTaskWindow()
    {
        InitializeComponent();
        _selectedDate = DateTime.Today;
        BuildDayChips();
        BuildTimeChips();
        RefreshTimeEnablement();

        CancelButton.Click += (_, _) => Close();
        SaveButton.Click += (_, _) => TrySave();
        TaskBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) TrySave(); };
        Loaded += (_, _) => TaskBox.Focus();
    }

    // ---------- Day chips ----------

    private void BuildDayChips()
    {
        DayPanel.Children.Clear();
        AddDayChip(DateTime.Today, "Today");
        AddDayChip(DateTime.Today.AddDays(1), "Tomorrow");
        var dayAfter = DateTime.Today.AddDays(2);
        AddDayChip(dayAfter, dayAfter.ToString("dddd"));

        _pickDayChip = MakeChip("Pick day...");
        _pickDayChip.Click += (_, _) => ShowCalendar();
        DayPanel.Children.Add(_pickDayChip);

        ((ToggleButton)DayPanel.Children[0]).IsChecked = true;
    }

    private void AddDayChip(DateTime date, string label)
    {
        var chip = MakeChip(label);
        chip.Tag = date;
        chip.Click += (_, _) =>
        {
            UncheckDays();
            chip.IsChecked = true;
            _selectedDate = date;
            if (chip != _pickDayChip && _pickDayChip != null) _pickDayChip.Content = "Pick day...";
            RefreshTimeEnablement();
        };
        DayPanel.Children.Insert(Math.Max(0, DayPanel.Children.Count - (DayPanel.Children.Contains(_pickDayChip) ? 1 : 0)), chip);
    }

    private void UncheckDays()
    {
        foreach (var child in DayPanel.Children)
            if (child is ToggleButton tb) tb.IsChecked = false;
    }

    private void ShowCalendar()
    {
        var calendar = new Calendar
        {
            DisplayDateStart = DateTime.Today,
            SelectedDate = _selectedDate,
            Background = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };

        var popup = new Popup
        {
            PlacementTarget = _pickDayChip,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("CardBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("DividerBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(6),
                Effect = (System.Windows.Media.Effects.DropShadowEffect)FindResource("SoftShadow"),
                Child = calendar
            }
        };

        calendar.SelectedDatesChanged += (_, _) =>
        {
            if (calendar.SelectedDate is DateTime picked)
            {
                _selectedDate = picked.Date;
                UncheckDays();
                if (_pickDayChip != null)
                {
                    _pickDayChip.IsChecked = true;
                    _pickDayChip.Content = picked.ToString("ddd d MMM");
                }
                RefreshTimeEnablement();
            }
            popup.IsOpen = false;
        };

        popup.IsOpen = true;
    }

    // ---------- Time chips ----------

    private void BuildTimeChips()
    {
        TimePanel.Children.Clear();
        _timeChips.Clear();

        var s = App.Settings.Current;
        var step = Math.Max(15, s.TimeStepMinutes);
        for (var t = s.WorkingStart; t <= s.WorkingEnd; t = t.AddMinutes(step))
        {
            var time = t;
            var chip = MakeChip(time.ToString("HH:mm"));
            chip.Tag = time;
            chip.Click += (_, _) =>
            {
                foreach (var other in _timeChips) other.IsChecked = false;
                chip.IsChecked = true;
                _selectedTime = time;
                MessageNote.Text = "";
            };
            _timeChips.Add(chip);
            TimePanel.Children.Add(chip);
        }
    }

    /// <summary>Times already passed stay visible but can't be picked today.</summary>
    private void RefreshTimeEnablement()
    {
        var isToday = _selectedDate == DateTime.Today;
        var now = TimeOnly.FromDateTime(DateTime.Now);
        ToggleButton? firstEnabled = null;

        foreach (var chip in _timeChips)
        {
            var time = (TimeOnly)chip.Tag!;
            var enabled = !isToday || time > now;
            chip.IsEnabled = enabled;
            if (!enabled && chip.IsChecked == true)
            {
                chip.IsChecked = false;
                _selectedTime = null;
            }
            if (enabled && firstEnabled == null) firstEnabled = chip;
        }

        // One less decision: pre-pick the next sensible slot.
        if (_selectedTime == null && firstEnabled != null)
        {
            firstEnabled.IsChecked = true;
            _selectedTime = (TimeOnly)firstEnabled.Tag!;
        }
    }

    // ---------- Save ----------

    private void TrySave()
    {
        var text = TaskBox.Text.Trim();
        if (text.Length == 0)
        {
            MessageNote.Text = "Give the task a few words first.";
            TaskBox.Focus();
            return;
        }

        if (_selectedTime is not TimeOnly time)
        {
            MessageNote.Text = "Pick a time block above.";
            return;
        }

        var when = _selectedDate.Add(time.ToTimeSpan());
        if (when <= DateTime.Now)
        {
            MessageNote.Text = "That time's already passed - pick a later one.";
            return;
        }

        App.Tasks.Add(text, when);
        DialogResult = true;
        Close();
    }

    private ToggleButton MakeChip(string label)
        => new() { Content = label, Style = (Style)FindResource("ChipToggle"), Margin = new Thickness(0, 0, 6, 6) };
}
