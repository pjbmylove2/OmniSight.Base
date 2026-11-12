using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OmniSight.Base.UI
{
    /// <summary>이미지 파일 없이 부팅 화면의 다각형 배경을 그립니다.</summary>
    public sealed class StartupBrandPanel : Panel
    {
        private Color _primaryColor = Color.RosyBrown;
        private Color _secondaryColor = Color.FromArgb(47, 47, 47);

        public Color PrimaryColor
        {
            get => _primaryColor;
            set { _primaryColor = value; Invalidate(); }
        }

        public Color SecondaryColor
        {
            get => _secondaryColor;
            set { _secondaryColor = value; Invalidate(); }
        }

        public StartupBrandPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (var gradient = new LinearGradientBrush(ClientRectangle,
                _primaryColor, _secondaryColor, LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(gradient, ClientRectangle);
            }

            // 좌표는 화면 크기에 대한 비율로 지정해 DPI가 달라도 패턴을 유지합니다.
            FillTriangle(e.Graphics, Color.FromArgb(15, Color.White),
                0.00f, 0.00f, 0.17f, 0.00f, 0.04f, 0.35f);
            FillTriangle(e.Graphics, Color.FromArgb(20, Color.White),
                0.17f, 0.00f, 0.27f, 0.50f, 0.04f, 0.35f);
            FillTriangle(e.Graphics, Color.FromArgb(11, Color.White),
                0.04f, 0.35f, 0.27f, 0.50f, 0.09f, 0.83f);
            FillTriangle(e.Graphics, Color.FromArgb(17, Color.White),
                0.09f, 0.83f, 0.27f, 0.50f, 0.21f, 1.00f);
            FillTriangle(e.Graphics, Color.FromArgb(15, Color.White),
                0.73f, 0.00f, 0.89f, 0.00f, 0.74f, 0.37f);
            FillTriangle(e.Graphics, Color.FromArgb(22, Color.White),
                0.89f, 0.00f, 1.00f, 0.00f, 0.84f, 0.40f);
            FillTriangle(e.Graphics, Color.FromArgb(14, Color.White),
                0.84f, 0.40f, 1.00f, 0.56f, 1.00f, 1.00f);
            FillTriangle(e.Graphics, Color.FromArgb(11, Color.White),
                0.84f, 0.40f, 1.00f, 1.00f, 0.76f, 0.89f);
            FillTriangle(e.Graphics, Color.FromArgb(10, Color.White),
                0.32f, 0.00f, 0.59f, 0.00f, 0.53f, 0.30f);
            FillTriangle(e.Graphics, Color.FromArgb(9, Color.White),
                0.24f, 0.77f, 0.56f, 1.00f, 0.13f, 1.00f);
        }

        private void FillTriangle(Graphics graphics, Color color,
            float x1, float y1, float x2, float y2, float x3, float y3)
        {
            using (var brush = new SolidBrush(color))
            {
                graphics.FillPolygon(brush, new[] {
                    new PointF(x1 * Width, y1 * Height),
                    new PointF(x2 * Width, y2 * Height),
                    new PointF(x3 * Width, y3 * Height) });
            }
        }
    }

    /// <summary>운영체제 테마에 관계없이 동일한 녹색 진행 막대를 표시합니다.</summary>
    public sealed class StartupProgressBar : Control
    {
        private readonly Timer _animationTimer;
        private int _value;
        private int _animationOffset;
        private bool _isMarquee = true;

        public int Value
        {
            get => _value;
            set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public bool IsMarquee
        {
            get => _isMarquee;
            set
            {
                if (_isMarquee == value)
                {
                    return;
                }

                _isMarquee = value;
                if (IsHandleCreated)
                {
                    if (value)
                    {
                        _animationTimer.Start();
                    }
                    else
                    {
                        _animationTimer.Stop();
                    }
                }
                Invalidate();
            }
        }

        public StartupProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            AccessibleName = "초기화 진행률";
            _animationTimer = new Timer { Interval = 35 };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_isMarquee)
            {
                _animationTimer.Start();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _animationTimer.Stop();
            base.OnHandleDestroyed(e);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            int travelWidth = _value > 0
                ? (int)Math.Round(Width * _value / 100.0)
                : Width;
            _animationOffset = (_animationOffset + 5) % Math.Max(1, travelWidth + 56);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var track = new SolidBrush(Color.FromArgb(227, 234, 241)))
            {
                e.Graphics.FillRectangle(track, ClientRectangle);
            }

            int fillWidth = (int)Math.Round(Width * _value / 100.0);
            if (fillWidth > 0)
            {
                using (var fill = new SolidBrush(Color.FromArgb(28, 190, 72)))
                {
                    e.Graphics.FillRectangle(fill, 0, 0, fillWidth, Height);
                }
            }

            if (!_isMarquee || Width <= 0 || Height <= 0)
            {
                return;
            }

            // 실제 진행률은 유지하고, 밝은 띠만 표시된 영역 안에서 움직입니다.
            // 첫 단계 전에는 전체 트랙에 움직이는 녹색 띠를 보여 줍니다.
            int travelWidth = fillWidth > 0 ? fillWidth : Width;
            const int bandWidth = 56;
            int bandX = _animationOffset - bandWidth;
            var bandBounds = new Rectangle(bandX, 0, bandWidth, Height);
            using (var band = new LinearGradientBrush(bandBounds, Color.Transparent,
                Color.Transparent, LinearGradientMode.Horizontal))
            {
                Color center = fillWidth > 0
                    ? Color.FromArgb(155, Color.White)
                    : Color.FromArgb(220, 28, 190, 72);
                band.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.Transparent, center, Color.Transparent },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                GraphicsState state = e.Graphics.Save();
                try
                {
                    e.Graphics.SetClip(new Rectangle(0, 0, travelWidth, Height));
                    e.Graphics.FillRectangle(band, bandBounds);
                }
                finally
                {
                    e.Graphics.Restore(state);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Tick -= AnimationTimer_Tick;
                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
