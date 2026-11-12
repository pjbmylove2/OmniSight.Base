using OmniSight.Base.Localization;
using OmniSight.Base.Configuration;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OmniSight.Base.UI.Utils
{
    /// <summary>
    /// 실행 파일에 포함된 폰트를 로드하고 프로젝트의 WinForms 컨트롤에 자동 적용합니다.
    /// </summary>
    public static class FontManager
    {
        private static PrivateFontCollection _privateFontCollection;
        private static FontFamily _customFontFamily;
        private static IntPtr _fontMemoryPointer = IntPtr.Zero;
        private static bool _initializationAttempted;

        private static readonly object _sync = new object();
        private static readonly Dictionary<string, Font> _sharedFonts = new Dictionary<string, Font>();
        private static readonly ConditionalWeakTable<Control, object> _hookedControls =
            new ConditionalWeakTable<Control, object>();
        private static readonly object _hookMarker = new object();

        private static IntPtr _gdiRegistration;

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr data, uint length, IntPtr reserved, out uint count);

        [DllImport("gdi32.dll")]
        private static extern bool RemoveFontMemResourceEx(IntPtr registration);

        private static FontFamily SelectedFamily => LocalizationManager.CurrentLanguage == LanguageType.CHINESE
            ? null : _customFontFamily;
        private static string FallbackFontName => LocalizationManager.CurrentLanguage == LanguageType.CHINESE
            ? "Microsoft YaHei UI" : "Malgun Gothic";

        /// <summary>
        /// 프로그램 시작 시 임베디드 리소스 폰트를 비관리 메모리에 고정하여 로드합니다.
        /// 폰트 파일을 읽지 못해도 프로그램은 중단하지 않고 맑은 고딕으로 통일합니다.
        /// </summary>
        public static void Initialize(string resourceNamespacePath = "OmniSight.Base.Resources.NanumGothic.ttf")
        {
            if (_initializationAttempted)
            {
                return;
            }

            _initializationAttempted = true;
            Stream fontStream = null;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                fontStream = assembly.GetManifestResourceStream(resourceNamespacePath);

                if (fontStream == null)
                {
                    Trace.WriteLine(
                        LocalText.Get("Text.FontManager.ResourceMissingConsole", "[Font Warning] 임베디드 리소스를 찾을 수 없습니다: ") +
                        resourceNamespacePath + LocalText.Get("Text.FontManager.FallbackFontConsole", ". 기본 폰트로 대체합니다."));
                    return;
                }

                int fontLength = (int)fontStream.Length;
                byte[] fontData = new byte[fontLength];
                int offset = 0;
                while (offset < fontLength)
                {
                    int read = fontStream.Read(fontData, offset, fontLength - offset);
                    if (read <= 0)
                    {
                        throw new EndOfStreamException(LocalText.Get("Text.FontManager.FontDataIncomplete", "임베디드 폰트 데이터를 모두 읽지 못했습니다."));
                    }
                    offset += read;
                }

                // PrivateFontCollection은 AddMemoryFont 호출 후에도 메모리를 참조하므로
                // 응용 프로그램이 종료될 때까지 해제되지 않는 비관리 메모리에 보관합니다.
                _fontMemoryPointer = Marshal.AllocCoTaskMem(fontLength);
                Marshal.Copy(fontData, 0, _fontMemoryPointer, fontLength);

                _privateFontCollection = new PrivateFontCollection();
                _privateFontCollection.AddMemoryFont(_fontMemoryPointer, fontLength);
                _gdiRegistration = AddFontMemResourceEx(_fontMemoryPointer, (uint)fontLength, IntPtr.Zero, out _);
                if (_privateFontCollection.Families.Length > 0)
                {
                    _customFontFamily = _privateFontCollection.Families[0];
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(LocalText.Get("Text.FontManager.LoadErrorConsole", "[Font Error] 임베디드 폰트 로드 중 예외 발생: ") + ex.Message);
                _customFontFamily = null;
            }
            finally
            {
                fontStream?.Dispose();
            }
        }

        /// <summary>
        /// 직접 그리기에 사용할 새 Font를 만듭니다. 호출자가 Dispose해야 합니다.
        /// </summary>
        public static Font CreateFont(float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point)
        {
            FontFamily family = SelectedFamily;
            try
            {
                return family != null
                    ? new Font(family, size, style, unit)
                    : new Font(FallbackFontName, size, style, unit);
            }
            catch (ArgumentException)
            {
                return family != null
                    ? new Font(family, size, FontStyle.Regular, unit)
                    : new Font(FallbackFontName, size, FontStyle.Regular, unit);
            }
        }

        /// <summary>
        /// 컨트롤 트리 전체의 글꼴 계열을 임베디드 폰트로 바꿉니다.
        /// 기존 크기, Bold/Italic 스타일과 표시 단위는 유지합니다. 나중에 추가되는
        /// 자식 컨트롤도 ControlAdded 이벤트를 통해 자동으로 같은 글꼴을 사용합니다.
        /// </summary>
        public static void Apply(Control root)
        {
            if (!_initializationAttempted || root == null || root.IsDisposed)
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

            ApplyToControl(root);
            object marker;
            if (!_hookedControls.TryGetValue(root, out marker))
            {
                _hookedControls.Add(root, _hookMarker);
                root.ControlAdded += ControlAdded;
                root.Disposed += ControlDisposed;
            }

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }
        }

        private static void ControlAdded(object sender, ControlEventArgs e) => Apply(e.Control);

        private static void ControlDisposed(object sender, EventArgs e)
        {
            var control = (Control)sender;
            control.ControlAdded -= ControlAdded;
            control.Disposed -= ControlDisposed;
            _hookedControls.Remove(control);
        }

        /// <summary>단일 컨트롤의 글꼴 계열을 적용합니다.</summary>
        internal static void ApplyToControl(Control control)
        {
            if (!_initializationAttempted || control == null || control.IsDisposed)
            {
                return;
            }

            control.Font = GetSharedFont(control.Font);
            if (control is Label label) { label.UseCompatibleTextRendering = true; }
            if (control is ButtonBase button) { button.UseCompatibleTextRendering = true; }

            // DataGridView는 Control.Font와 별도로 셀 스타일에 폰트를 보관할 수 있으므로
            // 헤더, 기본 셀, 교대 행과 컬럼별 스타일까지 모두 교체합니다.
            var grid = control as DataGridView;
            if (grid != null)
            {
                ApplyGridStyleFont(grid.DefaultCellStyle, grid.Font);
                ApplyGridStyleFont(grid.RowsDefaultCellStyle, grid.Font);
                ApplyGridStyleFont(grid.AlternatingRowsDefaultCellStyle, grid.Font);
                ApplyGridStyleFont(grid.ColumnHeadersDefaultCellStyle, grid.Font);
                ApplyGridStyleFont(grid.RowHeadersDefaultCellStyle, grid.Font);
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    ApplyGridStyleFont(column.DefaultCellStyle, grid.Font);
                }
            }

            var toolStrip = control as ToolStrip;
            if (toolStrip != null)
            {
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    ApplyToolStripItemFont(item, toolStrip.Font);
                }
            }
        }

        public static string ActiveFontName =>
            SelectedFamily?.Name ?? FallbackFontName;

        public static bool IsCustomFontLoaded => _customFontFamily != null;

        private static void ApplyGridStyleFont(DataGridViewCellStyle style, Font inheritedFont)
        {
            if (style != null)
            {
                style.Font = GetSharedFont(style.Font ?? inheritedFont);
            }
        }

        private static void ApplyToolStripItemFont(ToolStripItem item, Font inheritedFont)
        {
            if (item == null)
            {
                return;
            }

            item.Font = GetSharedFont(item.Font ?? inheritedFont);
            var dropDown = item as ToolStripDropDownItem;
            if (dropDown == null)
            {
                return;
            }

            foreach (ToolStripItem child in dropDown.DropDownItems)
            {
                ApplyToolStripItemFont(child, item.Font);
            }
        }

        private static Font GetSharedFont(Font source)
        {
            source = source ?? SystemFonts.DefaultFont;
            if (string.Equals(source.Name, ActiveFontName, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            FontFamily family = SelectedFamily;
            string fontName = family?.Name ?? FallbackFontName;
            string key = fontName + "|" + source.Size.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                         (int)source.Style + "|" + (int)source.Unit + "|" +
                         source.GdiCharSet + "|" + source.GdiVerticalFont;
            lock (_sync)
            {
                Font cached;
                if (_sharedFonts.TryGetValue(key, out cached))
                {
                    return cached;
                }

                try
                {
                    cached = family != null
                        ? new Font(family, source.Size, source.Style, source.Unit,
                            source.GdiCharSet, source.GdiVerticalFont)
                        : new Font(fontName, source.Size, source.Style, source.Unit,
                            source.GdiCharSet, source.GdiVerticalFont);
                }
                catch (ArgumentException)
                {
                    cached = family != null
                        ? new Font(family, source.Size, FontStyle.Regular, source.Unit,
                            source.GdiCharSet, source.GdiVerticalFont)
                        : new Font(fontName, source.Size, FontStyle.Regular, source.Unit,
                            source.GdiCharSet, source.GdiVerticalFont);
                }

                _sharedFonts.Add(key, cached);
                return cached;
            }
        }

        public static void Release()
        {
            lock (_sync)
            {
                foreach (Font font in _sharedFonts.Values)
                {
                    font.Dispose();
                }
                _sharedFonts.Clear();
            }

            _customFontFamily?.Dispose();
            _customFontFamily = null;
            if (_gdiRegistration != IntPtr.Zero)
            {
                RemoveFontMemResourceEx(_gdiRegistration);
                _gdiRegistration = IntPtr.Zero;
            }

            if (_privateFontCollection != null)
            {
                _privateFontCollection.Dispose();
                _privateFontCollection = null;
            }
            if (_fontMemoryPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_fontMemoryPointer);
                _fontMemoryPointer = IntPtr.Zero;
            }
            _initializationAttempted = false;
        }
    }
}
