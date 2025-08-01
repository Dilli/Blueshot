using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Blueshot
{
    public partial class SplashForm : Form
    {
        private Timer fadeTimer;
        private double opacity = 0.0;
        private bool fadingIn = true;
        private Bitmap backgroundImage;

        public SplashForm()
        {
            InitializeComponent();
            GenerateBackgroundImage();
            InitializeFadeTimer();
            this.Opacity = 0;
        }

        private void InitializeComponent()
        {
            this.Text = "Blueshot";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(15, 15, 23);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // Create rounded corners
            var path = new GraphicsPath();
            int radius = 20;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            this.Region = new Region(path);
        }

        private void GenerateBackgroundImage()
        {
            backgroundImage = new Bitmap(600, 400);
            using (var g = Graphics.FromImage(backgroundImage))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Create gradient background
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 600, 400),
                    Color.FromArgb(15, 15, 23),
                    Color.FromArgb(25, 25, 40),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(brush, 0, 0, 600, 400);
                }

                // Add abstract tech pattern
                DrawTechPattern(g);
                
                // Add glowing particles
                DrawGlowingParticles(g);
                
                // Add geometric overlay
                DrawGeometricOverlay(g);
            }
        }

        private void DrawTechPattern(Graphics g)
        {
            var random = new Random(42); // Fixed seed for consistency
            using (var pen = new Pen(Color.FromArgb(40, 0, 120, 215), 1))
            {
                // Draw connected nodes network
                var nodes = new Point[20];
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i] = new Point(
                        random.Next(50, 550),
                        random.Next(50, 350)
                    );
                }

                // Draw connections
                for (int i = 0; i < nodes.Length; i++)
                {
                    for (int j = i + 1; j < nodes.Length; j++)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(nodes[i].X - nodes[j].X, 2) +
                            Math.Pow(nodes[i].Y - nodes[j].Y, 2)
                        );

                        if (distance < 120)
                        {
                            var alpha = (int)(60 * (1 - distance / 120));
                            using (var connectionPen = new Pen(Color.FromArgb(alpha, 0, 150, 255), 1))
                            {
                                g.DrawLine(connectionPen, nodes[i], nodes[j]);
                            }
                        }
                    }
                }

                // Draw nodes
                foreach (var node in nodes)
                {
                    using (var nodeBrush = new SolidBrush(Color.FromArgb(100, 0, 120, 215)))
                    {
                        g.FillEllipse(nodeBrush, node.X - 3, node.Y - 3, 6, 6);
                    }
                }
            }
        }

        private void DrawGlowingParticles(Graphics g)
        {
            var random = new Random(123);
            for (int i = 0; i < 30; i++)
            {
                var x = random.Next(0, 600);
                var y = random.Next(0, 400);
                var size = random.Next(2, 8);
                var alpha = random.Next(30, 100);

                using (var brush = new SolidBrush(Color.FromArgb(alpha, 0, 200, 255)))
                {
                    g.FillEllipse(brush, x, y, size, size);
                }
            }
        }

        private void DrawGeometricOverlay(Graphics g)
        {
            // Draw subtle hexagonal pattern
            using (var pen = new Pen(Color.FromArgb(20, 255, 255, 255), 1))
            {
                for (int x = -50; x < 650; x += 60)
                {
                    for (int y = -50; y < 450; y += 52)
                    {
                        var offsetX = (y / 52) % 2 == 0 ? 0 : 30;
                        DrawHexagon(g, pen, x + offsetX, y, 15);
                    }
                }
            }
        }

        private void DrawHexagon(Graphics g, Pen pen, int centerX, int centerY, int radius)
        {
            var points = new Point[6];
            for (int i = 0; i < 6; i++)
            {
                var angle = Math.PI / 3 * i;
                points[i] = new Point(
                    (int)(centerX + radius * Math.Cos(angle)),
                    (int)(centerY + radius * Math.Sin(angle))
                );
            }
            g.DrawPolygon(pen, points);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Draw background image
            if (backgroundImage != null)
            {
                g.DrawImage(backgroundImage, 0, 0);
            }

            // Draw logo area with glow effect
            var logoRect = new Rectangle(200, 120, 200, 80);
            using (var glowBrush = new LinearGradientBrush(
                logoRect,
                Color.FromArgb(100, 0, 120, 215),
                Color.FromArgb(20, 0, 120, 215),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(glowBrush, logoRect);
            }

            // Draw application title
            using (var titleFont = new Font("Segoe UI", 32, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.White))
            {
                var titleText = "Blueshot";
                var titleSize = g.MeasureString(titleText, titleFont);
                var titleX = (this.Width - titleSize.Width) / 2;
                var titleY = 140;

                // Draw title shadow
                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(titleText, titleFont, shadowBrush, titleX + 2, titleY + 2);
                }

                g.DrawString(titleText, titleFont, titleBrush, titleX, titleY);
            }

            // Draw subtitle
            using (var subtitleFont = new Font("Segoe UI", 12, FontStyle.Regular))
            using (var subtitleBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                var subtitleText = "Professional Screen Capture Tool";
                var subtitleSize = g.MeasureString(subtitleText, subtitleFont);
                var subtitleX = (this.Width - subtitleSize.Width) / 2;
                var subtitleY = 195;

                g.DrawString(subtitleText, subtitleFont, subtitleBrush, subtitleX, subtitleY);
            }

            // Draw loading indicator
            DrawLoadingIndicator(g);

            // Draw version info
            using (var versionFont = new Font("Segoe UI", 9, FontStyle.Regular))
            using (var versionBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            {
                var versionText = "Version 1.0.0";
                var versionSize = g.MeasureString(versionText, versionFont);
                g.DrawString(versionText, versionFont, versionBrush, 
                    this.Width - versionSize.Width - 20, this.Height - 30);
            }
        }

        private void DrawLoadingIndicator(Graphics g)
        {
            var centerX = this.Width / 2;
            var centerY = 280;
            var radius = 20;
            var dotRadius = 3;

            for (int i = 0; i < 8; i++)
            {
                var angle = (DateTime.Now.Millisecond / 100.0 + i * 45) * Math.PI / 180;
                var x = centerX + radius * Math.Cos(angle);
                var y = centerY + radius * Math.Sin(angle);
                
                var alpha = (int)(255 * (1 - i / 8.0));
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 0, 120, 215)))
                {
                    g.FillEllipse(brush, (float)x - dotRadius, (float)y - dotRadius, 
                        dotRadius * 2, dotRadius * 2);
                }
            }
        }

        private void InitializeFadeTimer()
        {
            fadeTimer = new Timer();
            fadeTimer.Interval = 50;
            fadeTimer.Tick += FadeTimer_Tick;
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (fadingIn)
            {
                opacity += 0.1;
                if (opacity >= 1.0)
                {
                    opacity = 1.0;
                    fadingIn = false;
                    
                    // Start auto-close timer after fade in completes
                    var autoCloseTimer = new Timer();
                    autoCloseTimer.Interval = 2000; // Show for 2 seconds
                    autoCloseTimer.Tick += (s, ev) =>
                    {
                        autoCloseTimer.Stop();
                        StartFadeOut();
                    };
                    autoCloseTimer.Start();
                }
            }
            else
            {
                opacity -= 0.1;
                if (opacity <= 0.0)
                {
                    opacity = 0.0;
                    fadeTimer.Stop();
                    this.Close();
                    return;
                }
            }

            this.Opacity = opacity;
            this.Invalidate(); // Refresh loading indicator
        }

        public void StartFadeIn()
        {
            opacity = 0.0;
            fadingIn = true;
            fadeTimer.Start();
        }

        public void StartFadeOut()
        {
            fadingIn = false;
            if (!fadeTimer.Enabled)
            {
                fadeTimer.Start();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            StartFadeOut();
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                StartFadeOut();
            }
            base.OnKeyDown(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fadeTimer?.Dispose();
                backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
