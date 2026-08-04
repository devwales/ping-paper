using System.Windows;
using Ping.Models;
using Ping.Platform;
using Ping.Printing;

namespace Ping.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadValues();
        WireEvents();
    }

    private void LoadValues()
    {
        var s = App.Settings.Current;

        SpoolerRadio.IsChecked = s.Connection == PrinterConnection.Spooler;
        NetworkRadio.IsChecked = s.Connection == PrinterConnection.Network;
        FileRadio.IsChecked = s.Connection == PrinterConnection.File;
        UpdateConnectionPanels();

        _ = RefreshPrintersAsync();
        PrinterCombo.Text = s.PrinterName;

        HostBox.Text = s.NetworkHost;
        PortBox.Text = s.NetworkPort.ToString();

        Width80.IsChecked = s.PaperWidthMm == 80;
        Width58.IsChecked = s.PaperWidthMm == 58;

        for (var h = 6; h <= 14; h++) StartCombo.Items.Add($"{h:D2}:00");
        for (var h = 12; h <= 23; h++) EndCombo.Items.Add($"{h:D2}:00");
        StartCombo.SelectedItem = s.WorkingStart.ToString("HH:mm");
        EndCombo.SelectedItem = s.WorkingEnd.ToString("HH:mm");
        if (StartCombo.SelectedIndex < 0) StartCombo.SelectedItem = "09:00";
        if (EndCombo.SelectedIndex < 0) EndCombo.SelectedItem = "19:00";

        SizeSlider.Value = Math.Clamp(s.BubbleSize, 48, 72);
        OpacitySlider.Value = Math.Clamp(s.BubbleOpacity, 0.7, 1.0);
        AlwaysTopBox.IsChecked = s.AlwaysOnTop;
        ShowOnStartBox.IsChecked = s.ShowBubbleOnStart;

        AutoStartBox.IsChecked = Autostart.IsEnabled || s.AutoStart;
        CatchUpBox.IsChecked = s.CatchUpEnabled;
    }

    private void WireEvents()
    {
        SpoolerRadio.Checked += (_, _) => UpdateConnectionPanels();
        NetworkRadio.Checked += (_, _) => UpdateConnectionPanels();
        FileRadio.Checked += (_, _) => UpdateConnectionPanels();

        RefreshButton.Click += async (_, _) =>
        {
            var current = PrinterCombo.Text;
            await RefreshPrintersAsync();
            PrinterCombo.Text = current;
        };

        TestButton.Click += (_, _) =>
        {
            var draft = BuildDraft();
            var problem = ValidatePrinter(draft);
            if (problem != null)
            {
                PrinterStatus.Text = problem;
                return;
            }
            App.Printer.EnqueueTest(draft);
            PrinterStatus.Text = "Test print on its way...";
        };

        App.Printer.StatusChanged += OnPrinterStatus;
        Closed += (_, _) => App.Printer.StatusChanged -= OnPrinterStatus;

        SaveButton.Click += (_, _) => Save();
        CancelButton.Click += (_, _) => Close();
    }

    private void OnPrinterStatus(string message) => PrinterStatus.Text = message;

    private async Task RefreshPrintersAsync()
    {
        RefreshButton.IsEnabled = false;
        try
        {
            // Off the UI thread: InstalledPrinters can block for seconds on
            // unreachable network printers.
            var names = await Task.Run(() => PrinterDiscovery.Discover().Select(p => p.Name).ToList());
            PrinterCombo.Items.Clear();
            foreach (var name in names)
                PrinterCombo.Items.Add(name);
            if (names.Count == 0)
                PrinterStatus.Text = "No printers found. Is the printer on and the Print Spooler service running?";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void UpdateConnectionPanels()
    {
        SpoolerPanel.Visibility = SpoolerRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        NetworkPanel.Visibility = NetworkRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        FileNote.Visibility = FileRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private AppSettings BuildDraft()
    {
        var s = App.Settings.Current;
        return new AppSettings
        {
            Connection = NetworkRadio.IsChecked == true ? PrinterConnection.Network
                       : FileRadio.IsChecked == true ? PrinterConnection.File
                       : PrinterConnection.Spooler,
            PrinterName = PrinterCombo.Text.Trim(),
            NetworkHost = HostBox.Text.Trim(),
            NetworkPort = int.TryParse(PortBox.Text.Trim(), out var port) ? port : 9100,
            PaperWidthMm = Width58.IsChecked == true ? 58 : 80,
            // everything else carries over
            BubbleLeft = s.BubbleLeft, BubbleTop = s.BubbleTop,
            BubbleSize = SizeSlider.Value, BubbleOpacity = OpacitySlider.Value,
            AlwaysOnTop = AlwaysTopBox.IsChecked == true,
            ShowBubbleOnStart = ShowOnStartBox.IsChecked == true,
            WorkingStart = TimeOnly.Parse((string)StartCombo.SelectedItem),
            WorkingEnd = TimeOnly.Parse((string)EndCombo.SelectedItem),
            TimeStepMinutes = s.TimeStepMinutes,
            AutoStart = AutoStartBox.IsChecked == true,
            CatchUpEnabled = CatchUpBox.IsChecked == true,
        };
    }

    private static string? ValidatePrinter(AppSettings s) => s.Connection switch
    {
        PrinterConnection.Spooler when string.IsNullOrWhiteSpace(s.PrinterName)
            => "Pick a printer first (or press Refresh).",
        PrinterConnection.Network when string.IsNullOrWhiteSpace(s.NetworkHost)
            => "Add the printer's network address first.",
        _ => null
    };

    private void Save()
    {
        var draft = BuildDraft();

        if (draft.WorkingEnd <= draft.WorkingStart)
        {
            PrinterStatus.Text = "Working hours: the end time needs to be after the start.";
            return;
        }

        var s = App.Settings.Current;
        s.Connection = draft.Connection;
        s.PrinterName = draft.PrinterName;
        s.NetworkHost = draft.NetworkHost;
        s.NetworkPort = draft.NetworkPort;
        s.PaperWidthMm = draft.PaperWidthMm;
        s.WorkingStart = draft.WorkingStart;
        s.WorkingEnd = draft.WorkingEnd;
        s.BubbleSize = draft.BubbleSize;
        s.BubbleOpacity = draft.BubbleOpacity;
        s.AlwaysOnTop = draft.AlwaysOnTop;
        s.ShowBubbleOnStart = draft.ShowBubbleOnStart;
        s.AutoStart = draft.AutoStart;
        s.CatchUpEnabled = draft.CatchUpEnabled;
        App.Settings.Save();

        Autostart.Set(s.AutoStart);

        DialogResult = true;
        Close();
    }
}
