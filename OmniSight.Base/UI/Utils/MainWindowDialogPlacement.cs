using System.Drawing;
using System.Windows.Forms;

namespace OmniSight.Base.UI.Utils
{
    internal static class MainWindowDialogPlacement
    {
        public static Form FindMainWindow(Form fallback = null)
        {
            foreach (Form candidate in System.Windows.Forms.Application.OpenForms)
            {
                if (candidate is OmniSight.Base.UI.MainForm && !candidate.IsDisposed && !candidate.Disposing)
                {
                    return candidate;
                }
            }
            return fallback != null && !fallback.IsDisposed ? fallback : null;
        }
        public static Point CalculateLocation(Rectangle mainBounds, Size dialogSize)
        {
            return new Point(mainBounds.Left + (mainBounds.Width - dialogSize.Width) / 2,
                mainBounds.Top + (mainBounds.Height - dialogSize.Height) / 2);
        }
        public static void Center(Form dialog, Form anchor = null)
        {
            Form main = FindMainWindow(anchor ?? dialog.Owner);
            Rectangle bounds = main == null ? Screen.FromControl(dialog).WorkingArea :
                main.WindowState == FormWindowState.Minimized ? main.RestoreBounds : main.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                bounds = Screen.FromControl(dialog).WorkingArea;
            }
            dialog.StartPosition = FormStartPosition.Manual;
            Point location = CalculateLocation(bounds, dialog.Size);
            if (dialog.Location != location)
            {
                dialog.Location = location;
            }
        }
    }
}
