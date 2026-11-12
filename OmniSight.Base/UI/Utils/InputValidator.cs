using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace OmniSight.Base.UI.Utils
{
    public static class InputValidator
    {
        private sealed class NumericEdit
        {
            public string LastValid = string.Empty;
        }

        private static readonly ConditionalWeakTable<TextBox, NumericEdit> NumericEdits =
            new ConditionalWeakTable<TextBox, NumericEdit>();

        public static void EnforceNumericInput(object sender, KeyPressEventArgs e,
            bool allowDecimal = false, bool allowNegative = false)
        {
            if (sender is TextBox box && !char.IsControl(e.KeyChar))
            {
                string next = box.Text.Remove(box.SelectionStart, box.SelectionLength)
                    .Insert(box.SelectionStart, e.KeyChar.ToString());
                if (!IsNumericEdit(next, allowDecimal, allowNegative))
                {
                    e.Handled = true;
                }
            }
        }

        public static void EnforceTextInput(object sender, KeyPressEventArgs e,
            bool allowNumbers = true, bool allowSpecialChars = true)
        {
            if (sender is TextBox && !char.IsControl(e.KeyChar) &&
                !IsAllowedText(e.KeyChar, allowNumbers, allowSpecialChars))
            {
                e.Handled = true;
            }
        }

        /// <summary>Keep the previous valid edit when pasted text is malformed.</summary>
        public static void RevertInvalidNumericPaste(object sender, bool allowDecimal = false, bool allowNegative = false)
        {
            if (!(sender is TextBox box))
            {
                return;
            }

            NumericEdit edit = NumericEdits.GetValue(box, _ => new NumericEdit());
            if (IsNumericEdit(box.Text, allowDecimal, allowNegative))
            {
                edit.LastValid = box.Text;
                return;
            }

            box.Text = IsNumericEdit(edit.LastValid, allowDecimal, allowNegative) ? edit.LastValid : string.Empty;
            box.SelectionStart = box.TextLength;
        }

        public static void RevertInvalidTextPaste(object sender, bool allowNumbers = true, bool allowSpecialChars = true)
        {
            if (sender is TextBox box && box.Text.Any(c => !IsAllowedText(c, allowNumbers, allowSpecialChars)))
            {
                box.Text = new string(box.Text.Where(c => IsAllowedText(c, allowNumbers, allowSpecialChars)).ToArray());
                box.SelectionStart = box.TextLength;
            }
        }

        internal static bool TryParseDouble(string text, out double value)
        {
            value = 0;
            string normalized = NormalizeDecimal(text.Trim());
            return IsNumericEdit(normalized, allowDecimal: true, allowNegative: true) &&
                double.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out value) && !double.IsInfinity(value) && !double.IsNaN(value);
        }

        private static bool IsNumericEdit(string text, bool allowDecimal, bool allowNegative)
        {
            string normalized = NormalizeDecimal(text);
            int start = allowNegative && normalized.StartsWith("-", StringComparison.Ordinal) ? 1 : 0;
            bool hasPoint = false;
            for (int index = start; index < normalized.Length; index++)
            {
                char character = normalized[index];
                if (character >= '0' && character <= '9')
                {
                    continue;
                }

                if (!allowDecimal || character != '.' || hasPoint)
                {
                    return false;
                }

                hasPoint = true;
            }

            return true;
        }

        private static string NormalizeDecimal(string text)
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            return separator == "." ? text : text.Replace(separator, ".");
        }

        private static bool IsAllowedText(char value, bool allowNumbers, bool allowSpecialChars) =>
            (allowNumbers || !char.IsDigit(value)) && (allowSpecialChars || char.IsLetterOrDigit(value));
    }
}
