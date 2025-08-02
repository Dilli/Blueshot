using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Blueshot
{
    public class RoundedToolStripRenderer : ToolStripProfessionalRenderer
    {
        private int cornerRadius;
        private Color backgroundColor;
        private Color borderColor;

        public RoundedToolStripRenderer(int cornerRadius = 6, Color? backgroundColor = null, Color? borderColor = null)
        {
            this.cornerRadius = cornerRadius;
            this.backgroundColor = backgroundColor ?? Color.FromArgb(248, 248, 248);
            this.borderColor = borderColor ?? Color.FromArgb(200, 200, 200);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStrip)
            {
                using (var brush = new SolidBrush(backgroundColor))
                using (var path = GetRoundedRectanglePath(e.AffectedBounds, cornerRadius))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    
                    // Draw border
                    using (var pen = new Pen(borderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripButton button)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-1, -1); // Slight inset for better appearance

                Color buttonColor;
                if (button.Pressed)
                {
                    buttonColor = Color.FromArgb(0, 84, 142); // Darker VS Code blue when pressed
                }
                else if (button.Selected || button.Checked)
                {
                    buttonColor = Color.FromArgb(0, 122, 204); // VS Code blue when selected/checked
                }
                else
                {
                    // Transparent background for normal state
                    return;
                }

                using (var brush = new SolidBrush(buttonColor))
                using (var path = GetRoundedRectanglePath(bounds, 3)) // Smaller radius for buttons
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Ensure text is white when button is selected/pressed
            if (e.Item is ToolStripButton button && (button.Selected || button.Checked || button.Pressed))
            {
                e.TextColor = Color.White;
            }
            base.OnRenderItemText(e);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2F;

            // Handle cases where radius is larger than rectangle dimensions
            var actualRadius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            diameter = actualRadius * 2F;

            if (actualRadius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.StartFigure();
            
            // Top-left arc
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            
            // Top-right arc
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            
            // Bottom-right arc
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            
            // Bottom-left arc
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedPanel : Panel
    {
        private int cornerRadius = 6;
        private Color borderColor = Color.FromArgb(200, 200, 200);
        private int borderWidth = 1;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value;
                this.Invalidate();
            }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                this.Invalidate();
            }
        }

        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (cornerRadius > 0)
            {
                using (var path = GetRoundedRectanglePath(this.ClientRectangle, cornerRadius))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    
                    // Set the region to create rounded corners
                    this.Region = new Region(path);
                    
                    // Draw border if specified
                    if (borderWidth > 0)
                    {
                        using (var pen = new Pen(borderColor, borderWidth))
                        {
                            var borderRect = this.ClientRectangle;
                            borderRect.Inflate(-borderWidth / 2, -borderWidth / 2);
                            using (var borderPath = GetRoundedRectanglePath(borderRect, cornerRadius))
                            {
                                e.Graphics.DrawPath(pen, borderPath);
                            }
                        }
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2F;

            // Handle cases where radius is larger than rectangle dimensions
            var actualRadius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            diameter = actualRadius * 2F;

            if (actualRadius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.StartFigure();
            
            // Top-left arc
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            
            // Top-right arc
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            
            // Bottom-right arc
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            
            // Bottom-left arc
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedPictureBox : PictureBox
    {
        private int cornerRadius = 4;
        private Color borderColor = Color.FromArgb(200, 200, 200);
        private int borderWidth = 1;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value;
                this.Invalidate();
            }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                this.Invalidate();
            }
        }

        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (cornerRadius > 0 && this.Image != null)
            {
                using (var path = GetRoundedRectanglePath(this.ClientRectangle, cornerRadius))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.SetClip(path);
                    
                    // Draw the image
                    base.OnPaint(e);
                    
                    // Reset clip
                    e.Graphics.ResetClip();
                    
                    // Set the region to create rounded corners for hit testing
                    this.Region = new Region(path);
                    
                    // Draw border if specified
                    if (borderWidth > 0)
                    {
                        using (var pen = new Pen(borderColor, borderWidth))
                        {
                            var borderRect = this.ClientRectangle;
                            borderRect.Inflate(-borderWidth / 2, -borderWidth / 2);
                            using (var borderPath = GetRoundedRectanglePath(borderRect, cornerRadius))
                            {
                                e.Graphics.DrawPath(pen, borderPath);
                            }
                        }
                    }
                }
            }
            else
            {
                base.OnPaint(e);
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2F;

            // Handle cases where radius is larger than rectangle dimensions
            var actualRadius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            diameter = actualRadius * 2F;

            if (actualRadius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.StartFigure();
            
            // Top-left arc
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            
            // Top-right arc
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            
            // Bottom-right arc
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            
            // Bottom-left arc
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }
    }
}
