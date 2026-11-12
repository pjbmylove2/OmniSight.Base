using System;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using OmniSight.Base.Localization;

namespace OmniSight.Base.UI.Utils
{
    /// <summary>정적 화면 문구만 연결합니다. 데이터 값과 운전 상태 문구는 덮어쓰지 않습니다.</summary>
    public static class UiLocalization
    {
        private sealed class CaptionState
        {
            public CaptionState() { }
            public string Source;
            public string LastApplied;
        }
        private static readonly ConditionalWeakTable<object, CaptionState> Captions =
            new ConditionalWeakTable<object, CaptionState>();
        private static readonly ConditionalWeakTable<object, object> Hooks =
            new ConditionalWeakTable<object, object>();
        public static void Apply(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (root.InvokeRequired)
            {
                if (root.IsHandleCreated)
                {
                    root.BeginInvoke(new Action(() => Apply(root)));
                }

                return;
            }
            ApplyChildren(root, root.GetType().Name);
        }

        private static void ApplyChildren(Control control, string viewName)
        {
            if (control is Form || control is UserControl)
            {
                viewName = control.GetType().Name;
            }

            // 번역표의 Binding이 실수로 설정되더라도 입력·데이터 컨트롤은 건드리지 않는다.
            if (IsStaticCaption(control))
            {
                control.Text = Caption(control, control.Text,
                    "UI." + viewName + "." + (control is Form ? "$this" : control.Name), "Text");
            }

            var grid = control as DataGridView;
            if (grid != null)
            {
                if (!Hooks.TryGetValue(grid, out object gridHook))
                {
                    grid.CellFormatting += Grid_CellFormatting;
                    Hooks.Add(grid, new object());
                }
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.HeaderText = Caption(column, column.HeaderText,
                        "UI." + viewName + "." + column.Name + ".HeaderText", "HeaderText");
                }
            }

            if (control is ComboBox combo && combo.DropDownStyle == ComboBoxStyle.DropDownList && !Hooks.TryGetValue(combo, out object hook))
            {
                combo.FormattingEnabled = true;
                combo.Format += Combo_Format;
                Hooks.Add(combo, new object());
            }
            if (control is ToolStrip strip) { ApplyItems(strip.Items, viewName); }
            if (control.ContextMenuStrip != null) { ApplyItems(control.ContextMenuStrip.Items, viewName); }

            foreach (Control child in control.Controls)
            {
                ApplyChildren(child, viewName);
            }
            if (control is ILocalizedView localized) { localized.RefreshLocalizedText(); }
        }

        private static string Caption(object owner, string current, string key, string binding)
        {
            CaptionState state = Captions.GetOrCreateValue(owner);
            string korean = LocalizationManager.GetKorean(key);
            if (state.Source == null || current != state.LastApplied)
            {
                state.Source = korean != null && (current == korean || current == LocalizationManager.Get(key, null))
                    ? korean : LocalizationManager.GetSource(current);
            }
            string translated;
            // A live value must never be overwritten by its designer's initial caption.
            if (korean != null && !LocalizationManager.TryGetBoundText(key, binding, out translated))
            {
                // Dynamic labels are localized by their producer, never through source aliases.
                state.Source = current;
                state.LastApplied = current;
                return current;
            }
            state.LastApplied = korean == state.Source && LocalizationManager.TryGetBoundText(key, binding, out translated)
                ? translated : LocalizationManager.TranslateSource(state.Source);
            return state.LastApplied;
        }

        private static void ApplyItems(ToolStripItemCollection items, string viewName)
        {
            foreach (ToolStripItem item in items)
            {
                item.Text = Caption(item, item.Text, "UI." + viewName + "." + item.Name, "Text");
                if (item is ToolStripDropDownItem dropdown) { ApplyItems(dropdown.DropDownItems, viewName); }
            }
        }

        private static void Combo_Format(object sender, ListControlConvertEventArgs e)
        {
            // Display only. SelectedItem, SelectedValue, enum values and saved JSON do not change.
            if (e.ListItem is Enum enumValue)
            {
                e.Value = LocalizationManager.GetEnumText(enumValue);
            }
            else if (e.ListItem is string)
            {
                e.Value = LocalizationManager.TranslateSource(Convert.ToString(e.Value));
            }
        }

        private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is Enum value)
            {
                e.Value = LocalizationManager.GetEnumText(value);
                e.FormattingApplied = true;
            }
        }

        private static bool IsStaticCaption(Control control)
        {
            return control is Label || control is ButtonBase ||
                   control is GroupBox || control is TabPage ||
                   control is Form;
        }
    }
}
