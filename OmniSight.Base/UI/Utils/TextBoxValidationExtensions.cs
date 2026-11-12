using OmniSight.Base.Localization;
using System.Globalization;
using System.Windows.Forms;

namespace OmniSight.Base.UI.Utils
{
    /// <summary>
    /// 전역에서 TextBox의 데이터 유효성과 범위(Min/Max)를 한 번에 검증하는 확장 메서드 유틸리티
    /// </summary>
    public static class TextBoxValidationExtensions
    {
        /// <summary>
        /// 텍스트박스의 값이 유효한 정수(int)인지, 그리고 지정된 범위(Min~Max) 내에 있는지 검증합니다.
        /// </summary>
        /// <returns>(검증 통과 여부, 에러 메시지)</returns>
        public static (bool IsValid, string ErrorMessage) CheckIntRange(this TextBox txtBox, string fieldName, int min, int max)
        {
            if (string.IsNullOrWhiteSpace(txtBox.Text))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.ValueRequired", $"[{fieldName}] 값을 입력해야 합니다."));
            }

            if (!int.TryParse(txtBox.Text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.IntegerRequired", $"[{fieldName}] 유효한 정수(숫자)로 입력해야 합니다."));
            }

            if (!(value >= min && value <= max))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.OutOfRange", $"[{fieldName}] 허용 범위를 초과했습니다. (허용: {min} ~ {max})"));
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 텍스트박스의 값이 유효한 실수(double)인지, 그리고 지정된 범위 내에 있는지 검증합니다.
        /// </summary>
        public static (bool IsValid, string ErrorMessage) CheckDoubleRange(this TextBox txtBox, string fieldName, double min, double max)
        {
            if (string.IsNullOrWhiteSpace(txtBox.Text))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.ValueRequired", $"[{fieldName}] 값을 입력해야 합니다."));
            }

            if (!InputValidator.TryParseDouble(txtBox.Text, out double value))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.NumberRequired", $"[{fieldName}] 유효한 실수(소수점)로 입력해야 합니다."));
            }

            if (!(value >= min && value <= max))
            {
                return (false, LocalText.Interpolate("Text.TextBoxValidationExtensions.OutOfRange", $"[{fieldName}] 허용 범위를 초과했습니다. (허용: {min} ~ {max})"));
            }

            return (true, string.Empty);
        }
    }
}
