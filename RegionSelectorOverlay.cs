using System;
using System.Drawing;
using System.Windows.Forms;

namespace Blueshot
{
    public class RegionSelectorOverlay : Form
    {
        private bool isSelecting = false;
        private Point startPoint;
        private Point endPoint;
        private Rectangle selectionRectangle;
        private Bitmap backgroundImage;
        private Timer animationTimer;
        private float dashOffset = 0;

        public event EventHandler<RegionSelectedEventArgs> RegionSelected;

        public RegionSelectorOverlay()
        {
            InitializeOverlay();
        }

        public new DialogResult ShowDialog()
        {
            CaptureBackground();
            return base.ShowDialog();
        }

        private void InitializeOverlay()
        {
            // Create a fullscreen overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.Opacity = 1.0; // Set to fully opaque - we'll handle transparency in painting
            this.Cursor = Cursors.Cross;

            // Enable double buffering for smooth drawing
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer, true);

            // Initialize animation timer for marching ants effect
            animationTimer = new Timer();
            animationTimer.Interval = 100; // Update every 100ms for smooth animation
            animationTimer.Tick += (s, e) => 
            {
                dashOffset += 1.0f;
                if (dashOffset > 10) dashOffset = 0; // Reset to prevent overflow
                if (!selectionRectangle.IsEmpty)
                {
                    this.Invalidate(); // Repaint to show animation
                }
            };

            // Event handlers
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
        }

        private void CaptureBackground()
        {
            try
            {
                // Capture the screen before showing this overlay
                var bounds = Screen.PrimaryScreen.Bounds;
                backgroundImage = new Bitmap(bounds.Width, bounds.Height);
                
                using (var graphics = Graphics.FromImage(backgroundImage))
                {
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing background: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                endPoint = e.Location;
                selectionRectangle = new Rectangle();
                
                // Start animation when selection begins
                animationTimer.Start();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                UpdateSelectionRectangle();
                this.Invalidate(); // Trigger repaint
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                isSelecting = false;
                endPoint = e.Location;
                UpdateSelectionRectangle();

                // Finalize selection
                CompleteSelection();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelSelection();
            }
        }

        private void UpdateSelectionRectangle()
        {
            int x = Math.Min(startPoint.X, endPoint.X);
            int y = Math.Min(startPoint.Y, endPoint.Y);
            int width = Math.Abs(endPoint.X - startPoint.X);
            int height = Math.Abs(endPoint.Y - startPoint.Y);

            selectionRectangle = new Rectangle(x, y, width, height);
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;

            // Draw the background image (original screenshot) first
            if (backgroundImage != null)
            {
                graphics.DrawImage(backgroundImage, 0, 0);
            }

            // Draw semi-transparent overlay everywhere except selection
            var overlayBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)); // Darker overlay for better contrast
            var screenBounds = this.Bounds;
            
            if (!selectionRectangle.IsEmpty)
            {
                // Create regions for the overlay (everything except selection)
                using (var fullRegion = new Region(screenBounds))
                using (var selectionRegion = new Region(selectionRectangle))
                {
                    fullRegion.Exclude(selectionRegion);
                    graphics.FillRegion(overlayBrush, fullRegion);
                }

                // Draw selection border with thin dotted style and marching ants effect
                using (var borderPen = new Pen(Color.FromArgb(255, 255, 255), 1)) // White outer border
                {
                    borderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    borderPen.DashPattern = new float[] { 4, 4 }; // 4 pixels on, 4 pixels off
                    borderPen.DashOffset = dashOffset; // Animated offset for marching ants
                    graphics.DrawRectangle(borderPen, selectionRectangle);
                }

                // Draw an inner border for contrast
                if (selectionRectangle.Width > 2 && selectionRectangle.Height > 2)
                {
                    var innerRect = new Rectangle(
                        selectionRectangle.X + 1,
                        selectionRectangle.Y + 1,
                        selectionRectangle.Width - 2,
                        selectionRectangle.Height - 2
                    );
                    
                    using (var innerBorderPen = new Pen(Color.FromArgb(0, 0, 0), 1)) // Black inner border
                    {
                        innerBorderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                        innerBorderPen.DashPattern = new float[] { 4, 4 }; // 4 pixels on, 4 pixels off
                        innerBorderPen.DashOffset = dashOffset + 4; // Offset by 4 to create alternating pattern
                        graphics.DrawRectangle(innerBorderPen, innerRect);
                    }
                }

                // Draw corner handles for better visibility
                DrawSelectionHandles(graphics);

                // Draw selection info
                DrawSelectionInfo(graphics);
            }
            else
            {
                // No selection, draw full overlay
                graphics.FillRectangle(overlayBrush, screenBounds);
                
                // Draw instruction text
                DrawInstructions(graphics);
            }

            overlayBrush.Dispose();
        }

        private void DrawSelectionHandles(Graphics graphics)
        {
            if (selectionRectangle.IsEmpty) return;

            var handleSize = 6; // Smaller handles to match thin border style
            var handleBrush = new SolidBrush(Color.FromArgb(0, 120, 215));
            var handlePen = new Pen(Color.White, 1);

            // Corner handles
            var handles = new Rectangle[]
            {
                new Rectangle(selectionRectangle.Left - handleSize/2, selectionRectangle.Top - handleSize/2, handleSize, handleSize),
                new Rectangle(selectionRectangle.Right - handleSize/2, selectionRectangle.Top - handleSize/2, handleSize, handleSize),
                new Rectangle(selectionRectangle.Left - handleSize/2, selectionRectangle.Bottom - handleSize/2, handleSize, handleSize),
                new Rectangle(selectionRectangle.Right - handleSize/2, selectionRectangle.Bottom - handleSize/2, handleSize, handleSize)
            };

            foreach (var handle in handles)
            {
                graphics.FillRectangle(handleBrush, handle);
                graphics.DrawRectangle(handlePen, handle);
            }

            handleBrush.Dispose();
            handlePen.Dispose();
        }

        private void DrawInstructions(Graphics graphics)
        {
            var instructionText = "Click and drag to select a region • Press Esc to cancel";
            var font = new Font("Segoe UI", 16, FontStyle.Regular);
            var textBrush = new SolidBrush(Color.White);
            var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));

            var textSize = graphics.MeasureString(instructionText, font);
            var textLocation = new PointF(
                (this.Width - textSize.Width) / 2,
                (this.Height - textSize.Height) / 2
            );

            // Draw shadow
            graphics.DrawString(instructionText, font, shadowBrush, textLocation.X + 2, textLocation.Y + 2);
            // Draw text
            graphics.DrawString(instructionText, font, textBrush, textLocation);

            font.Dispose();
            textBrush.Dispose();
            shadowBrush.Dispose();
        }

        private void DrawSelectionInfo(Graphics graphics)
        {
            if (selectionRectangle.IsEmpty) return;

            var infoText = $"{selectionRectangle.Width} × {selectionRectangle.Height}";
            var font = new Font("Segoe UI", 12, FontStyle.Bold);
            var textBrush = new SolidBrush(Color.White);
            var backgroundBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));

            var textSize = graphics.MeasureString(infoText, font);
            var textLocation = new PointF(
                selectionRectangle.X + 5,
                selectionRectangle.Y - textSize.Height - 5
            );

            // Adjust if text would be off-screen
            if (textLocation.Y < 0)
            {
                textLocation.Y = selectionRectangle.Y + 5;
            }

            var textBackground = new RectangleF(
                textLocation.X - 3,
                textLocation.Y - 2,
                textSize.Width + 6,
                textSize.Height + 4
            );

            graphics.FillRectangle(backgroundBrush, textBackground);
            graphics.DrawString(infoText, font, textBrush, textLocation);

            font.Dispose();
            textBrush.Dispose();
            backgroundBrush.Dispose();
        }

        private void CompleteSelection()
        {
            // Stop animation
            animationTimer?.Stop();
            
            var selectedRegion = selectionRectangle;
            
            // Ensure minimum size
            if (selectedRegion.Width < 10 || selectedRegion.Height < 10)
            {
                selectedRegion = Rectangle.Empty;
            }

            // Hide the overlay immediately before firing the event
            this.Hide();
            
            // Small delay to ensure overlay is completely hidden from screen
            Application.DoEvents();
            System.Threading.Thread.Sleep(50);

            RegionSelected?.Invoke(this, new RegionSelectedEventArgs(selectedRegion));
            this.Close();
        }

        private void CancelSelection()
        {
            // Stop animation
            animationTimer?.Stop();
            
            // Hide the overlay immediately
            this.Hide();
            Application.DoEvents();
            
            RegionSelected?.Invoke(this, new RegionSelectedEventArgs(Rectangle.Empty));
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
                backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class RegionSelectedEventArgs : EventArgs
    {
        public Rectangle SelectedRegion { get; }

        public RegionSelectedEventArgs(Rectangle selectedRegion)
        {
            SelectedRegion = selectedRegion;
        }
    }
}
