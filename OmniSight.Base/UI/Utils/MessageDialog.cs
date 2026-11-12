using System;
using System.Windows.Forms;
using OmniSight.Base.Logging;
using OmniSight.Base.UI.Forms;

namespace OmniSight.Base.UI.Utils
{
    public static class MessageDialog
    {
        public static DialogResult Show(string text, string caption = null, MessageSeverity level = MessageSeverity.Info,
            MessageBoxButtons buttons = MessageBoxButtons.OK, int autoCloseSeconds = 0, Form owner = null, LogManager log = null)
        {
            owner = owner ?? Form.ActiveForm ?? MainWindowDialogPlacement.FindMainWindow();
            if (owner != null && owner.InvokeRequired)
            {
                return (DialogResult)owner.Invoke(new Func<DialogResult>(() =>
                    Show(text, caption, level, buttons, autoCloseSeconds, owner, log)));
            }

            using (var dialog = new MessageDialogForm(text, caption, level, buttons, autoCloseSeconds))
            {
                string record = $"[{level}] {caption} - {text}";
                if (level == MessageSeverity.Error || level == MessageSeverity.CriticalAlarm)
                {
                    log?.Alarm(record);
                }
                else
                {
                    log?.System(record);
                }

                return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            }
        }
    }
}
