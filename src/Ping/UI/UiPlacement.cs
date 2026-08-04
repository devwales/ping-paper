using System.Windows;
using System.Windows.Forms;

namespace Ping.UI;

/// <summary>
/// Places small popup windows next to the bubble (above it if there's room,
/// below otherwise), always fully on screen.
/// </summary>
public static class UiPlacement
{
    public static void PlaceNear(Window popup, Window anchor)
    {
        popup.WindowStartupLocation = WindowStartupLocation.Manual;
        popup.Loaded += (_, _) =>
        {
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);

            var left = anchor.Left + anchor.Width / 2 - popup.ActualWidth / 2;
            var above = anchor.Top - popup.ActualHeight - 10;
            var top = above >= wa.Top ? above : anchor.Top + anchor.Height + 10;

            left = Math.Clamp(left, wa.Left + 8, wa.Right - popup.ActualWidth - 8);
            top = Math.Clamp(top, wa.Top + 8, wa.Bottom - popup.ActualHeight - 8);

            popup.Left = left;
            popup.Top = top;
        };
    }
}
