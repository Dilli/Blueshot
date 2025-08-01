using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Blueshot
{
    public partial class PreviewForm : Form
    {
        private PictureBox pictureBox;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private ToolStrip annotationToolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel sizeLabel;
        private ToolStripStatusLabel zoomLabel;
        private Panel imagePanel;
        private Bitmap screenshot;
        private Bitmap workingImage;
        private float zoomFactor = 1.0f;

        // Screenshot management
        private List<ScreenshotItem> screenshots = new List<ScreenshotItem>();
        private int currentScreenshotIndex = 0;
        private Panel thumbnailPanel;
        private Button previousButton;
        private Button nextButton;
        private Label screenshotCountLabel;

        // Annotation tools
        private bool isAnnotating = false;
        private AnnotationTool currentTool = AnnotationTool.None;
        private Color annotationColor = Color.Red;
        private int annotationThickness = 3;
        private bool isDrawing = false;
        private Point startPoint;
        private Point currentPoint;
        private List<AnnotationObject> annotations = new List<AnnotationObject>();

        // Counter feature
        private int counterNumber = 1;

        // Selection and manipulation
        private AnnotationObject selectedAnnotation = null;
        private bool isDragging = false;
        private bool isResizing = false;
        private Point lastMousePosition;
        private ResizeHandle activeResizeHandle = ResizeHandle.None;

        // Text editing
        private TextBox inlineTextBox = null;
        private AnnotationObject editingTextAnnotation = null;

        // Crop functionality
        private bool isCropping = false;
        private Rectangle cropRectangle = Rectangle.Empty;

        public class ScreenshotItem
        {
            public Bitmap OriginalImage { get; set; }
            public Bitmap WorkingImage { get; set; }
            public List<AnnotationObject> Annotations { get; set; } = new List<AnnotationObject>();
            public DateTime CaptureTime { get; set; }
            public string FileName { get; set; }
            public Bitmap Thumbnail { get; set; }

            public ScreenshotItem(Bitmap originalImage)
            {
                OriginalImage = new Bitmap(originalImage);
                WorkingImage = new Bitmap(originalImage);
                CaptureTime = DateTime.Now;
                FileName = $"Screenshot_{CaptureTime:yyyyMMdd_HHmmss}";
                CreateThumbnail();
            }

            private void CreateThumbnail()
            {
                const int thumbnailSize = 120;
                var aspectRatio = (float)OriginalImage.Width / OriginalImage.Height;
                int thumbWidth, thumbHeight;
                
                if (aspectRatio > 1)
                {
                    thumbWidth = thumbnailSize;
                    thumbHeight = (int)(thumbnailSize / aspectRatio);
                }
                else
                {
                    thumbWidth = (int)(thumbnailSize * aspectRatio);
                    thumbHeight = thumbnailSize;
                }
                
                Thumbnail = new Bitmap(thumbWidth, thumbHeight);
                using (var g = Graphics.FromImage(Thumbnail))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(OriginalImage, 0, 0, thumbWidth, thumbHeight);
                }
            }

            public void UpdateWorkingImage()
            {
                WorkingImage?.Dispose();
                WorkingImage = new Bitmap(OriginalImage);
                
                // Note: Annotations will be applied when needed by the parent form
            }

            public void Dispose()
            {
                OriginalImage?.Dispose();
                WorkingImage?.Dispose();
                Thumbnail?.Dispose();
            }
        }

        public enum AnnotationTool
        {
            None,
            Highlight,
            Rectangle,
            Line,
            Arrow,
            Text,
            Counter,
            Select,
            Crop
        }

        public enum ResizeHandle
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Top,
            Bottom,
            Left,
            Right
        }

        public class AnnotationObject
        {
            public AnnotationTool Tool { get; set; }
            public Point StartPoint { get; set; }
            public Point EndPoint { get; set; }
            public Color Color { get; set; }
            public int Thickness { get; set; }
            public bool IsSelected { get; set; }
            public string Text { get; set; } = "";
            public Font Font { get; set; } = new Font("Segoe UI", 12, FontStyle.Regular);
            public int CounterNumber { get; set; } = 0;

            public Rectangle GetBounds()
            {
                if (Tool == AnnotationTool.Text)
                {
                    // For text, calculate bounds based on text size
                    using (var g = Graphics.FromImage(new Bitmap(1, 1)))
                    {
                        var textSize = g.MeasureString(Text ?? "Sample", Font);
                        return new Rectangle(StartPoint.X, StartPoint.Y, (int)textSize.Width, (int)textSize.Height);
                    }
                }
                else if (Tool == AnnotationTool.Counter)
                {
                    // For counter, create a circular bounds
                    var radius = Math.Max(15, Thickness * 5);
                    return new Rectangle(StartPoint.X - radius, StartPoint.Y - radius, radius * 2, radius * 2);
                }
                
                return new Rectangle(
                    Math.Min(StartPoint.X, EndPoint.X),
                    Math.Min(StartPoint.Y, EndPoint.Y),
                    Math.Abs(EndPoint.X - StartPoint.X),
                    Math.Abs(EndPoint.Y - StartPoint.Y)
                );
            }

            public bool ContainsPoint(Point point)
            {
                var bounds = GetBounds();
                
                // Expand bounds slightly for easier selection
                bounds.Inflate(5, 5);
                
                switch (Tool)
                {
                    case AnnotationTool.Line:
                    case AnnotationTool.Arrow:
                        return IsPointNearLine(point, StartPoint, EndPoint, 8);
                    case AnnotationTool.Counter:
                        // Check if point is within circular bounds
                        var radius = Math.Max(15, Thickness * 5) + 5; // Add 5 for easier selection
                        var dx = point.X - StartPoint.X;
                        var dy = point.Y - StartPoint.Y;
                        return (dx * dx + dy * dy) <= (radius * radius);
                    case AnnotationTool.Text:
                        return bounds.Contains(point);
                    default:
                        return bounds.Contains(point);
                }
            }

            private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd, int tolerance)
            {
                // Calculate distance from point to line segment
                var A = point.X - lineStart.X;
                var B = point.Y - lineStart.Y;
                var C = lineEnd.X - lineStart.X;
                var D = lineEnd.Y - lineStart.Y;

                var dot = A * C + B * D;
                var lenSq = C * C + D * D;
                
                if (lenSq == 0) return Math.Sqrt(A * A + B * B) <= tolerance;

                var param = dot / lenSq;

                Point closest;
                if (param < 0)
                {
                    closest = lineStart;
                }
                else if (param > 1)
                {
                    closest = lineEnd;
                }
                else
                {
                    closest = new Point(
                        (int)(lineStart.X + param * C),
                        (int)(lineStart.Y + param * D)
                    );
                }

                var dx = point.X - closest.X;
                var dy = point.Y - closest.Y;
                return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
            }

            public void Move(Point offset)
            {
                StartPoint = new Point(StartPoint.X + offset.X, StartPoint.Y + offset.Y);
                if (Tool != AnnotationTool.Text)
                {
                    EndPoint = new Point(EndPoint.X + offset.X, EndPoint.Y + offset.Y);
                }
            }

            public void Resize(Point newEndPoint)
            {
                if (Tool != AnnotationTool.Text)
                {
                    EndPoint = newEndPoint;
                }
            }
        }

        public PreviewForm(Bitmap capturedImage)
        {
            InitializeComponent();
            AddScreenshot(capturedImage);
            SetupPreview();
        }

        // Add a new constructor for existing instances
        public PreviewForm()
        {
            InitializeComponent();
            SetupPreview();
        }

        public void AddScreenshot(Bitmap capturedImage)
        {
            var screenshotItem = new ScreenshotItem(capturedImage);
            screenshots.Add(screenshotItem);
            currentScreenshotIndex = screenshots.Count - 1;
            
            // Update UI to show the new screenshot
            if (screenshots.Count == 1)
            {
                screenshot = screenshotItem.OriginalImage;
                workingImage = new Bitmap(screenshotItem.WorkingImage);
                annotations = screenshotItem.Annotations;
            }
            else
            {
                LoadScreenshot(currentScreenshotIndex);
            }
            
            // Only update UI components if they are initialized
            if (thumbnailPanel != null)
            {
                UpdateThumbnailPanel();
                UpdateNavigationButtons();
                UpdateScreenshotCounter();
            }
        }

        private void LoadScreenshot(int index)
        {
            if (index < 0 || index >= screenshots.Count) return;
            
            currentScreenshotIndex = index;
            var screenshotItem = screenshots[index];
            
            screenshot = screenshotItem.OriginalImage;
            workingImage?.Dispose();
            workingImage = new Bitmap(screenshotItem.WorkingImage);
            annotations = screenshotItem.Annotations;
            
            // Clear current selection
            selectedAnnotation = null;
            
            // Update the display only if pictureBox is initialized
            if (pictureBox != null)
            {
                pictureBox.Image = workingImage;
                UpdateImageDisplay();
                UpdateStatusLabels();
                pictureBox.Invalidate();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Screenshot Preview - Blueshot";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Normal;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Icon = SystemIcons.Application;
            this.MinimumSize = new Size(600, 400);

            // Create menu strip
            CreateMenuStrip();

            // Create toolbar
            CreateToolStrip();

            // Create annotation toolbar
            CreateAnnotationToolStrip();

            // Create main image panel with scrolling
            CreateImagePanel();

            // Create status strip
            CreateStatusStrip();

            // Set up layout
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(imagePanel);
            this.Controls.Add(annotationToolStrip);
            this.Controls.Add(toolStrip);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);

            // Event handlers
            this.KeyDown += OnKeyDown;
            this.FormClosing += OnFormClosing;
            this.Resize += OnFormResize;
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(250, 250, 250),
                Font = new Font("Segoe UI", 9)
            };

            // File menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Save", Properties.Resources.Save ?? null, (s, e) => QuickSave());
            fileMenu.DropDownItems.Add("Save &As...", Properties.Resources.SaveAs ?? null, (s, e) => SaveAs());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Copy to Clipboard", Properties.Resources.Copy ?? null, (s, e) => CopyToClipboard());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Close", null, (s, e) => this.Close());

            // Edit menu
            var editMenu = new ToolStripMenuItem("&Edit");
            editMenu.DropDownItems.Add("&Copy", Properties.Resources.Copy ?? null, (s, e) => CopyToClipboard());
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Select &All", null, (s, e) => { /* Future: Select all functionality */ });

            // View menu
            var viewMenu = new ToolStripMenuItem("&View");
            viewMenu.DropDownItems.Add("&Zoom In", Properties.Resources.ZoomIn ?? null, (s, e) => ZoomIn());
            viewMenu.DropDownItems.Add("Zoom &Out", Properties.Resources.ZoomOut ?? null, (s, e) => ZoomOut());
            viewMenu.DropDownItems.Add("&Actual Size", Properties.Resources.ActualSize ?? null, (s, e) => ActualSize());
            viewMenu.DropDownItems.Add("&Fit to Window", Properties.Resources.FitToWindow ?? null, (s, e) => FitToWindow());
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("&Fullscreen", null, (s, e) => ToggleFullscreen());

            // Tools menu (for future extensions)
            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Settings...", Properties.Resources.Settings ?? null, (s, e) => ShowSettings());

            // Help menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About Blueshot...", Properties.Resources.About ?? null, (s, e) => ShowAbout());

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, toolsMenu, helpMenu });
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip
            {
                BackColor = Color.FromArgb(245, 245, 245),
                GripStyle = ToolStripGripStyle.Hidden,
                Font = new Font("Segoe UI", 9),
                ImageScalingSize = new Size(24, 24)
            };

            // Save buttons
            var saveButton = new ToolStripButton("", CreateSaveIcon(), (s, e) => QuickSave())
            {
                ToolTipText = "Save to Desktop (Ctrl+S)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var saveAsButton = new ToolStripButton("", CreateSaveIcon(), (s, e) => SaveAs())
            {
                ToolTipText = "Save As... (Ctrl+Shift+S)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var copyButton = new ToolStripButton("", CreateCopyIcon(), (s, e) => CopyToClipboard())
            {
                ToolTipText = "Copy to Clipboard (Ctrl+C)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            // Separator
            var separator1 = new ToolStripSeparator();

            // Zoom controls
            var zoomInButton = new ToolStripButton("Zoom In", CreateZoomInIcon(), (s, e) => ZoomIn())
            {
                ToolTipText = "Zoom In (Ctrl++)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var zoomOutButton = new ToolStripButton("Zoom Out", CreateZoomOutIcon(), (s, e) => ZoomOut())
            {
                ToolTipText = "Zoom Out (Ctrl+-)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var actualSizeButton = new ToolStripButton("", CreateActualSizeIcon(), (s, e) => ActualSize())
            {
                ToolTipText = "Actual Size (Ctrl+0)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var fitToWindowButton = new ToolStripButton("", CreateFitIcon(), (s, e) => FitToWindow())
            {
                ToolTipText = "Fit to Window (Ctrl+F)",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            // Separator
            var separator2 = new ToolStripSeparator();

            // Color selector for annotation
            var annotationColorLabel = new ToolStripLabel("Color:");
            var annotationColorButton = new ToolStripButton("■", null, ShowColorPicker)
            {
                ToolTipText = "Choose Annotation Color",
                ForeColor = annotationColor,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Name = "ColorButton"
            };

            // Thickness selector for annotation
            var annotationThicknessLabel = new ToolStripLabel("Thickness:");
            var annotationThicknessCombo = new ToolStripComboBox("AnnotationThicknessCombo")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 60
            };
            annotationThicknessCombo.Items.AddRange(new object[] { "1", "2", "3", "4", "5", "6", "8", "10", "12", "15" });
            annotationThicknessCombo.SelectedItem = annotationThickness.ToString();
            annotationThicknessCombo.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(annotationThicknessCombo.SelectedItem?.ToString(), out int thickness))
                {
                    annotationThickness = thickness;
                }
            };

            // Another separator
            var separator3 = new ToolStripSeparator();

            // Counter controls
            var counterLabel = new ToolStripLabel("Counter:");
            var counterButton = new ToolStripButton("", CreateCounterIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Counter))
            {
                ToolTipText = "Add Counter Number",
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            
            var counterResetButton = new ToolStripButton("Reset", null, (s, e) => ResetCounter())
            {
                ToolTipText = "Reset Counter to 1",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 8)
            };

            // Another separator
            var separator4 = new ToolStripSeparator();

            // Close button
            var closeButton = new ToolStripButton("", CreateCloseIcon(), (s, e) => this.Close())
            {
                ToolTipText = "Close Preview (Esc)",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Alignment = ToolStripItemAlignment.Right
            };

            toolStrip.Items.AddRange(new ToolStripItem[] {
                saveButton, saveAsButton, copyButton, separator1,
                zoomInButton, zoomOutButton, actualSizeButton, fitToWindowButton, separator2,
                annotationColorLabel, annotationColorButton, annotationThicknessLabel, annotationThicknessCombo, separator3,
                counterLabel, counterButton, counterResetButton, separator4,
                closeButton
            });
        }

        private void CreateAnnotationToolStrip()
        {
            annotationToolStrip = new ToolStrip
            {
                BackColor = Color.FromArgb(248, 248, 248),
                GripStyle = ToolStripGripStyle.Hidden,
                Font = new Font("Segoe UI", 9),
                ImageScalingSize = new Size(24, 24),
                Dock = DockStyle.Left,
                LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow,
                Width = 90,
                RenderMode = ToolStripRenderMode.Professional
            };

            // Select tool
            var selectButton = new ToolStripButton("Select", CreateSelectIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Select))
            {
                ToolTipText = "Select Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Select
            };

            // Highlight tool
            var highlightButton = new ToolStripButton("Highlight", CreateHighlightIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Highlight))
            {
                ToolTipText = "Highlight Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Highlight
            };

            // Rectangle tool
            var rectangleButton = new ToolStripButton("Rectangle", CreateRectangleIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Rectangle))
            {
                ToolTipText = "Rectangle Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Rectangle
            };

            // Line tool
            var lineButton = new ToolStripButton("Line", CreateLineIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Line))
            {
                ToolTipText = "Line Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Line
            };

            // Arrow tool
            var arrowButton = new ToolStripButton("Arrow", CreateArrowIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Arrow))
            {
                ToolTipText = "Arrow Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Arrow
            };

            // Text tool
            var textButton = new ToolStripButton("Text", CreateTextIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Text))
            {
                ToolTipText = "Text Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Text
            };

            // Counter tool
            var counterToolButton = new ToolStripButton("Counter", CreateCounterIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Counter))
            {
                ToolTipText = "Counter Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Counter
            };

            // Crop tool
            var cropToolButton = new ToolStripButton("Crop", CreateCropIcon(), (s, e) => SetAnnotationTool(AnnotationTool.Crop))
            {
                ToolTipText = "Crop Tool",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Checked = currentTool == AnnotationTool.Crop
            };

            var separator3 = new ToolStripSeparator();

            // Undo button
            var undoButton = new ToolStripButton("Undo", CreateUndoIcon(), (s, e) => UndoLastAnnotation())
            {
                ToolTipText = "Undo Last Annotation",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText
            };

            // Clear annotations button
            var clearButton = new ToolStripButton("Clear All", CreateClearIcon(), (s, e) => ClearAnnotations())
            {
                ToolTipText = "Clear All Annotations",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                TextImageRelation = TextImageRelation.ImageAboveText
            };

            annotationToolStrip.Items.AddRange(new ToolStripItem[] {
                selectButton, 
                new ToolStripSeparator(),
                highlightButton, rectangleButton, lineButton, arrowButton, textButton, counterToolButton, cropToolButton,
                separator3, 
                undoButton, clearButton
            });
        }

        private void CreateImagePanel()
        {
            imagePanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10)
            };

            // Create main container for image and thumbnails
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Create thumbnail panel at the bottom
            CreateThumbnailPanel();

            // Create navigation panel
            CreateNavigationPanel();

            pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Normal,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.None
            };

            mainContainer.Controls.Add(pictureBox);
            imagePanel.Controls.Add(mainContainer);
            imagePanel.Controls.Add(thumbnailPanel);
        }

        private void CreateThumbnailPanel()
        {
            thumbnailPanel = new Panel
            {
                Height = 140,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(5)
            };
        }

        private void CreateNavigationPanel()
        {
            var navPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };

            previousButton = new Button
            {
                Text = "◀ Previous",
                Width = 80,
                Height = 30,
                Location = new Point(10, 5),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            previousButton.FlatAppearance.BorderSize = 0;
            previousButton.Click += (s, e) => NavigateToScreenshot(currentScreenshotIndex - 1);

            nextButton = new Button
            {
                Text = "Next ▶",
                Width = 80,
                Height = 30,
                Location = new Point(100, 5),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            nextButton.FlatAppearance.BorderSize = 0;
            nextButton.Click += (s, e) => NavigateToScreenshot(currentScreenshotIndex + 1);

            screenshotCountLabel = new Label
            {
                Text = "Screenshot 1 of 1",
                Location = new Point(200, 10),
                Size = new Size(150, 20),
                ForeColor = Color.FromArgb(64, 64, 64),
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft
            };

            navPanel.Controls.AddRange(new Control[] { previousButton, nextButton, screenshotCountLabel });
            thumbnailPanel.Controls.Add(navPanel);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 9)
            };

            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            sizeLabel = new ToolStripStatusLabel("Size: 0 × 0")
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            zoomLabel = new ToolStripStatusLabel("Zoom: 100%")
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, sizeLabel, zoomLabel });
        }

        private Bitmap CreateIcon(Color color)
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.FillRectangle(new SolidBrush(color), 2, 2, 12, 12);
                g.DrawRectangle(Pens.DarkGray, 1, 1, 13, 13);
            }
            return icon;
        }

        private Bitmap CreateSaveIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Modern save icon background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(24, 24),
                    Color.FromArgb(0, 120, 215), Color.FromArgb(0, 96, 172)))
                {
                    g.FillRectangle(brush, 3, 3, 18, 18);
                }
                
                // Top corner cutout
                using (var brush = new SolidBrush(Color.Transparent))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    var points = new Point[] { new Point(15, 3), new Point(21, 3), new Point(21, 9) };
                    g.FillPolygon(brush, points);
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
                
                // Disk slot
                using (var brush = new SolidBrush(Color.FromArgb(0, 80, 150)))
                {
                    g.FillRectangle(brush, 8, 17, 8, 3);
                }
                
                // Label area with gradient
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(brush, 6, 7, 12, 8);
                }
                
                // Highlight for depth
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 4, 4, 4, 20);
                    g.DrawLine(pen, 4, 4, 14, 4);
                }
                
                // Border
                using (var pen = new Pen(Color.FromArgb(0, 64, 128), 1))
                {
                    g.DrawRectangle(pen, 3, 3, 18, 18);
                }
            }
            return icon;
        }

        private Bitmap CreateCopyIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Back document (shadow)
                using (var brush = new SolidBrush(Color.FromArgb(120, 120, 120)))
                {
                    g.FillRectangle(brush, 3, 3, 14, 16);
                }
                
                // Front document
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(24, 24),
                    Color.FromArgb(33, 150, 243), Color.FromArgb(21, 101, 192)))
                {
                    g.FillRectangle(brush, 6, 5, 14, 16);
                }
                
                // Document corner fold
                using (var brush = new SolidBrush(Color.Transparent))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    var points = new Point[] { new Point(17, 5), new Point(20, 5), new Point(20, 8) };
                    g.FillPolygon(brush, points);
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
                
                // Fold line
                using (var pen = new Pen(Color.FromArgb(21, 101, 192), 1))
                {
                    g.DrawLine(pen, 17, 5, 17, 8);
                    g.DrawLine(pen, 17, 8, 20, 8);
                }
                
                // Document lines
                using (var pen = new Pen(Color.White, 1))
                {
                    g.DrawLine(pen, 8, 9, 17, 9);
                    g.DrawLine(pen, 8, 12, 17, 12);
                    g.DrawLine(pen, 8, 15, 14, 15);
                }
                
                // Border highlights
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 6, 5, 6, 20);
                    g.DrawLine(pen, 6, 5, 16, 5);
                }
            }
            return icon;
        }

        private Bitmap CreateZoomInIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Magnifying glass circle
                using (var brush = new SolidBrush(Color.FromArgb(76, 175, 80)))
                {
                    g.FillEllipse(brush, 3, 3, 14, 14);
                }
                
                // Handle
                using (var pen = new Pen(Color.FromArgb(76, 175, 80), 3))
                {
                    g.DrawLine(pen, 15, 15, 21, 21);
                }
                
                // Plus sign
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 10, 6, 10, 14);
                    g.DrawLine(pen, 6, 10, 14, 10);
                }
            }
            return icon;
        }

        private Bitmap CreateZoomOutIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Magnifying glass circle
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillEllipse(brush, 3, 3, 14, 14);
                }
                
                // Handle
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 3))
                {
                    g.DrawLine(pen, 15, 15, 21, 21);
                }
                
                // Minus sign
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 6, 10, 14, 10);
                }
            }
            return icon;
        }

        private Bitmap CreateFitIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Outer rectangle
                using (var pen = new Pen(Color.FromArgb(158, 158, 158), 2))
                {
                    g.DrawRectangle(pen, 2, 2, 20, 20);
                }
                
                // Inner rectangle
                using (var brush = new SolidBrush(Color.FromArgb(96, 125, 139)))
                {
                    g.FillRectangle(brush, 6, 6, 12, 12);
                }
                
                // Corner arrows
                using (var pen = new Pen(Color.FromArgb(96, 125, 139), 1))
                {
                    // Top-left
                    g.DrawLine(pen, 4, 8, 4, 4);
                    g.DrawLine(pen, 4, 4, 8, 4);
                    
                    // Top-right
                    g.DrawLine(pen, 16, 4, 20, 4);
                    g.DrawLine(pen, 20, 4, 20, 8);
                    
                    // Bottom-left
                    g.DrawLine(pen, 4, 16, 4, 20);
                    g.DrawLine(pen, 4, 20, 8, 20);
                    
                    // Bottom-right
                    g.DrawLine(pen, 16, 20, 20, 20);
                    g.DrawLine(pen, 20, 20, 20, 16);
                }
            }
            return icon;
        }

        private Bitmap CreateActualSizeIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Outer square
                using (var pen = new Pen(Color.FromArgb(96, 125, 139), 2))
                {
                    g.DrawRectangle(pen, 3, 3, 18, 18);
                }
                
                // Text "1:1"
                using (var brush = new SolidBrush(Color.FromArgb(96, 125, 139)))
                using (var font = new Font("Arial", 8, FontStyle.Bold))
                {
                    var text = "1:1";
                    var size = g.MeasureString(text, font);
                    var x = (24 - size.Width) / 2;
                    var y = (24 - size.Height) / 2;
                    g.DrawString(text, font, brush, x, y);
                }
            }
            return icon;
        }

        private Bitmap CreateCloseIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Circle background
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillEllipse(brush, 2, 2, 20, 20);
                }
                
                // X mark
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 8, 8, 16, 16);
                    g.DrawLine(pen, 16, 8, 8, 16);
                }
            }
            return icon;
        }

        // Annotation tool icons
        private Bitmap CreateSelectIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Modern cursor pointer
                using (var brush = new SolidBrush(Color.FromArgb(64, 64, 64)))
                {
                    var points = new Point[] {
                        new Point(5, 4), new Point(5, 17), new Point(9, 13),
                        new Point(12, 15), new Point(14, 11), new Point(11, 9), new Point(15, 5)
                    };
                    g.FillPolygon(brush, points);
                }
                
                // White highlight for depth
                using (var pen = new Pen(Color.White, 1))
                {
                    g.DrawLine(pen, 6, 5, 6, 15);
                    g.DrawLine(pen, 6, 5, 13, 5);
                }
                
                // Selection handles
                using (var brush = new SolidBrush(Color.FromArgb(33, 150, 243)))
                {
                    g.FillRectangle(brush, 17, 7, 3, 3);
                    g.FillRectangle(brush, 20, 10, 3, 3);
                    g.FillRectangle(brush, 17, 13, 3, 3);
                }
            }
            return icon;
        }

        private Bitmap CreateHighlightIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Highlighter marker body
                using (var brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    g.FillRectangle(brush, 3, 6, 18, 8);
                }
                
                // Cap
                using (var brush = new SolidBrush(Color.FromArgb(255, 152, 0)))
                {
                    g.FillRectangle(brush, 20, 7, 2, 6);
                }
                
                // Tip
                using (var brush = new SolidBrush(Color.FromArgb(255, 235, 59)))
                {
                    g.FillRectangle(brush, 1, 8, 3, 4);
                }
                
                // Text being highlighted
                using (var pen = new Pen(Color.FromArgb(96, 96, 96), 1))
                {
                    g.DrawLine(pen, 5, 16, 19, 16);
                    g.DrawLine(pen, 5, 18, 15, 18);
                }
                
                // Highlight effect
                using (var brush = new SolidBrush(Color.FromArgb(100, 255, 235, 59)))
                {
                    g.FillRectangle(brush, 5, 15, 14, 4);
                }
            }
            return icon;
        }

        private Bitmap CreateRectangleIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Main rectangle
                using (var pen = new Pen(Color.FromArgb(156, 39, 176), 2.5f))
                {
                    g.DrawRectangle(pen, 4, 4, 16, 12);
                }
                
                // Inner shadow effect
                using (var pen = new Pen(Color.FromArgb(50, 0, 0, 0), 1))
                {
                    g.DrawRectangle(pen, 5, 5, 14, 10);
                }
                
                // Corner highlights
                using (var brush = new SolidBrush(Color.FromArgb(200, 156, 39, 176)))
                {
                    g.FillRectangle(brush, 3, 3, 3, 3);
                    g.FillRectangle(brush, 18, 3, 3, 3);
                    g.FillRectangle(brush, 3, 14, 3, 3);
                    g.FillRectangle(brush, 18, 14, 3, 3);
                }
            }
            return icon;
        }

        private Bitmap CreateLineIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Main line with gradient effect
                using (var pen = new Pen(Color.FromArgb(76, 175, 80), 3))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLine(pen, 4, 20, 20, 4);
                }
                
                // Highlight line for depth
                using (var pen = new Pen(Color.FromArgb(100, 200, 200, 200), 1))
                {
                    g.DrawLine(pen, 5, 19, 19, 5);
                }
                
                // End points
                using (var brush = new SolidBrush(Color.FromArgb(76, 175, 80)))
                {
                    g.FillEllipse(brush, 2, 18, 4, 4);
                    g.FillEllipse(brush, 18, 2, 4, 4);
                }
            }
            return icon;
        }

        private Bitmap CreateArrowIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Arrow shaft
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 3))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLine(pen, 4, 20, 16, 8);
                }
                
                // Arrow head with better shape
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    var points = new Point[] {
                        new Point(16, 8), new Point(22, 4), new Point(20, 8), new Point(22, 12)
                    };
                    g.FillPolygon(brush, points);
                }
                
                // Highlight for depth
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 5, 19, 15, 9);
                }
                
                // Start point
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillEllipse(brush, 2, 18, 4, 4);
                }
            }
            return icon;
        }

        private Bitmap CreateTextIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                // Text cursor/insertion point
                using (var pen = new Pen(Color.FromArgb(63, 81, 181), 2))
                {
                    g.DrawLine(pen, 8, 4, 8, 20);
                    g.DrawLine(pen, 6, 4, 10, 4);
                    g.DrawLine(pen, 6, 20, 10, 20);
                }
                
                // Letter "A" 
                using (var brush = new SolidBrush(Color.FromArgb(63, 81, 181)))
                using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    g.DrawString("A", font, brush, 12, 6);
                }
                
                // Text lines
                using (var pen = new Pen(Color.FromArgb(150, 63, 81, 181), 1))
                {
                    g.DrawLine(pen, 4, 16, 20, 16);
                    g.DrawLine(pen, 4, 18, 16, 18);
                }
            }
            return icon;
        }

        private Bitmap CreateClearIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Trash can body
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillRectangle(brush, 7, 8, 10, 12);
                }
                
                // Trash can lid
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillRectangle(brush, 5, 6, 14, 2);
                }
                
                // Handle
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 2))
                {
                    g.DrawArc(pen, 9, 2, 6, 6, 0, -180);
                }
                
                // Vertical lines inside
                using (var pen = new Pen(Color.White, 1.5f))
                {
                    g.DrawLine(pen, 9, 10, 9, 17);
                    g.DrawLine(pen, 12, 10, 12, 17);
                    g.DrawLine(pen, 15, 10, 15, 17);
                }
                
                // Shadow/depth
                using (var brush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillRectangle(brush, 8, 9, 8, 10);
                }
            }
            return icon;
        }

        private Bitmap CreateUndoIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Curved undo arrow
                using (var pen = new Pen(Color.FromArgb(96, 125, 139), 2.5f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawArc(pen, 6, 6, 12, 12, 180, 160);
                }
                
                // Arrow head with better shape
                using (var brush = new SolidBrush(Color.FromArgb(96, 125, 139)))
                {
                    var points = new Point[] {
                        new Point(6, 12), new Point(11, 7), new Point(11, 11), new Point(9, 17)
                    };
                    g.FillPolygon(brush, points);
                }
                
                // Highlight for depth
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawArc(pen, 7, 7, 10, 10, 180, 140);
                }
            }
            return icon;
        }

        private Bitmap CreateCounterIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                // Gradient circle background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(24, 24),
                    Color.FromArgb(76, 175, 80), Color.FromArgb(56, 142, 60)))
                {
                    g.FillEllipse(brush, 2, 2, 20, 20);
                }
                
                // Glossy highlight
                using (var brush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 4, 4, 8, 8);
                }
                
                // Border with depth
                using (var pen = new Pen(Color.FromArgb(27, 94, 32), 1.5f))
                {
                    g.DrawEllipse(pen, 2, 2, 20, 20);
                }
                
                // Number with shadow
                using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
                {
                    var text = counterNumber.ToString();
                    var size = g.MeasureString(text, font);
                    var x = (24 - size.Width) / 2;
                    var y = (24 - size.Height) / 2;
                    
                    // Shadow
                    using (var brush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    {
                        g.DrawString(text, font, brush, x + 1, y + 1);
                    }
                    
                    // Main text
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(text, font, brush, x, y);
                    }
                }
            }
            return icon;
        }

        private Bitmap CreateCropIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Image frame
                using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                {
                    g.FillRectangle(brush, 3, 3, 18, 14);
                }
                
                // Frame border
                using (var pen = new Pen(Color.FromArgb(96, 96, 96), 2))
                {
                    g.DrawRectangle(pen, 3, 3, 18, 14);
                }
                
                // Crop selection with dashed line
                using (var pen = new Pen(Color.FromArgb(33, 150, 243), 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    pen.DashPattern = new float[] { 2, 2 };
                    g.DrawRectangle(pen, 6, 5, 12, 10);
                }
                
                // Corner crop handles
                using (var brush = new SolidBrush(Color.FromArgb(33, 150, 243)))
                {
                    g.FillRectangle(brush, 5, 4, 3, 3);
                    g.FillRectangle(brush, 16, 4, 3, 3);
                    g.FillRectangle(brush, 5, 13, 3, 3);
                    g.FillRectangle(brush, 16, 13, 3, 3);
                }
                
                // Scissors icon overlay
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 1.5f))
                {
                    // Scissor blades
                    g.DrawEllipse(pen, 18, 17, 4, 4);
                    g.DrawLine(pen, 20, 19, 14, 13);
                }
            }
            return icon;
        }

        private void NavigateToScreenshot(int index)
        {
            if (index >= 0 && index < screenshots.Count && index != currentScreenshotIndex)
            {
                // Save current annotations to the current screenshot
                SaveCurrentAnnotations();
                LoadScreenshot(index);
                UpdateNavigationButtons();
                UpdateScreenshotCounter();
                UpdateThumbnailSelection();
            }
        }

        private void SaveCurrentAnnotations()
        {
            if (currentScreenshotIndex >= 0 && currentScreenshotIndex < screenshots.Count)
            {
                var currentItem = screenshots[currentScreenshotIndex];
                currentItem.Annotations = new List<AnnotationObject>(annotations);
                currentItem.UpdateWorkingImage();
                // Apply annotations to working image
                using (var g = Graphics.FromImage(currentItem.WorkingImage))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    foreach (var annotation in annotations)
                    {
                        DrawAnnotation(g, annotation);
                    }
                }
            }
        }

        private void UpdateThumbnailPanel()
        {
            if (thumbnailPanel == null) return;
            
            // Clear existing thumbnails except navigation panel
            var controlsToRemove = thumbnailPanel.Controls.Cast<Control>()
                .Where(c => c is PictureBox || (c is Panel && c.Controls.Count == 0))
                .ToList();
            
            foreach (var control in controlsToRemove)
            {
                thumbnailPanel.Controls.Remove(control);
                control.Dispose();
            }

            // Add thumbnail images
            int x = 10;
            int y = 45; // Below navigation panel
            
            for (int i = 0; i < screenshots.Count; i++)
            {
                var thumbBox = CreateThumbnailBox(screenshots[i], i);
                thumbBox.Location = new Point(x, y);
                thumbnailPanel.Controls.Add(thumbBox);
                x += thumbBox.Width + 10;
            }
        }

        private PictureBox CreateThumbnailBox(ScreenshotItem screenshotItem, int index)
        {
            var thumbBox = new PictureBox
            {
                Image = screenshotItem.Thumbnail,
                Size = new Size(screenshotItem.Thumbnail.Width + 4, screenshotItem.Thumbnail.Height + 4),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BorderStyle = index == currentScreenshotIndex ? BorderStyle.Fixed3D : BorderStyle.FixedSingle,
                BackColor = index == currentScreenshotIndex ? Color.FromArgb(0, 120, 215) : Color.White,
                Cursor = Cursors.Hand,
                Tag = index
            };
            
            thumbBox.Click += (s, e) => NavigateToScreenshot((int)thumbBox.Tag);
            
            // Add context menu for additional actions
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("View", null, (s, e) => NavigateToScreenshot(index));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete", null, (s, e) => 
            {
                if (screenshots.Count > 1)
                {
                    var result = MessageBox.Show($"Delete screenshot {index + 1}?", "Delete Screenshot", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        RemoveScreenshot(index);
                    }
                }
                else
                {
                    MessageBox.Show("Cannot delete the last screenshot.", "Delete Screenshot", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            thumbBox.ContextMenuStrip = contextMenu;
            
            // Add tooltip with capture time
            var tooltip = new ToolTip();
            tooltip.SetToolTip(thumbBox, $"Screenshot {index + 1}\nCaptured: {screenshotItem.CaptureTime:HH:mm:ss}\nRight-click for more options");
            
            return thumbBox;
        }

        private void UpdateNavigationButtons()
        {
            if (previousButton != null && nextButton != null)
            {
                previousButton.Enabled = currentScreenshotIndex > 0;
                nextButton.Enabled = currentScreenshotIndex < screenshots.Count - 1;
            }
        }

        private void UpdateScreenshotCounter()
        {
            if (screenshotCountLabel != null)
            {
                screenshotCountLabel.Text = $"Screenshot {currentScreenshotIndex + 1} of {screenshots.Count}";
            }
        }

        private void UpdateThumbnailSelection()
        {
            if (thumbnailPanel != null)
            {
                foreach (Control control in thumbnailPanel.Controls)
                {
                    if (control is PictureBox thumbBox && thumbBox.Tag is int index)
                    {
                        thumbBox.BorderStyle = index == currentScreenshotIndex ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
                        thumbBox.BackColor = index == currentScreenshotIndex ? Color.FromArgb(0, 120, 215) : Color.White;
                    }
                }
            }
        }

        private void SetupPreview()
        {
            if (screenshots.Count > 0 && currentScreenshotIndex >= 0 && pictureBox != null)
            {
                var currentItem = screenshots[currentScreenshotIndex];
                screenshot = currentItem.OriginalImage;
                workingImage = new Bitmap(currentItem.WorkingImage);
                annotations = currentItem.Annotations;
                
                pictureBox.Image = workingImage;
                UpdateImageDisplay();
                FitToWindow();
                UpdateStatusLabels();
                SetupImageEventHandlers();
                
                // Initialize navigation UI
                UpdateThumbnailPanel();
                UpdateNavigationButtons();
                UpdateScreenshotCounter();
            }
        }

        private void SetupImageEventHandlers()
        {
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.MouseDoubleClick += PictureBox_MouseDoubleClick;
            pictureBox.Paint += PictureBox_Paint;
        }

        // Annotation tool methods
        private void SetAnnotationTool(AnnotationTool tool)
        {
            currentTool = tool;
            isAnnotating = tool != AnnotationTool.None && tool != AnnotationTool.Select;
            
            // Clear selection when switching away from select tool
            if (tool != AnnotationTool.Select && selectedAnnotation != null)
            {
                selectedAnnotation.IsSelected = false;
                selectedAnnotation = null;
                RedrawImage();
            }
            
            // Update toolbar button states
            foreach (ToolStripItem item in annotationToolStrip.Items)
            {
                if (item is ToolStripButton button && !string.IsNullOrEmpty(button.Text) 
                    && !button.Text.Contains("■") && !button.Text.Contains("Undo") && !button.Text.Contains("Clear"))
                {
                    button.Checked = false;
                }
            }
            
            // Set current tool as checked
            var toolNames = new Dictionary<AnnotationTool, string>
            {
                { AnnotationTool.Select, "Select" },
                { AnnotationTool.Highlight, "Highlight" },
                { AnnotationTool.Rectangle, "Rectangle" },
                { AnnotationTool.Line, "Line" },
                { AnnotationTool.Arrow, "Arrow" },
                { AnnotationTool.Text, "Text" },
                { AnnotationTool.Counter, "Counter" },
                { AnnotationTool.Crop, "Crop" }
            };
            
            if (toolNames.ContainsKey(tool))
            {
                foreach (ToolStripItem item in annotationToolStrip.Items)
                {
                    if (item is ToolStripButton button && button.Text == toolNames[tool])
                    {
                        button.Checked = true;
                        break;
                    }
                }
            }

            // Update status
            var navigationHint = screenshots.Count > 1 ? " • ←→ Navigate screenshots" : "";
            statusLabel.Text = tool switch
            {
                AnnotationTool.Select => "Select mode - Click to select, drag to move, resize with handles" + navigationHint,
                AnnotationTool.Highlight => "Highlight mode - Click and drag to highlight areas" + navigationHint,
                AnnotationTool.Rectangle => "Rectangle mode - Click and drag to draw rectangles" + navigationHint,
                AnnotationTool.Line => "Line mode - Click and drag to draw lines" + navigationHint,
                AnnotationTool.Arrow => "Arrow mode - Click and drag to draw arrows" + navigationHint,
                AnnotationTool.Text => "Text mode - Click to add text annotations" + navigationHint,
                AnnotationTool.Counter => $"Counter mode - Click to add counter number ({counterNumber})" + navigationHint,
                AnnotationTool.Crop => "Crop mode - Click and drag to select area to crop, press Enter to apply" + navigationHint,
                _ => "Ready" + navigationHint
            };
        }

        private void ShowColorPicker(object sender, EventArgs e)
        {
            var colorDialog = new ColorDialog
            {
                Color = annotationColor,
                AllowFullOpen = true,
                FullOpen = true
            };
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                annotationColor = colorDialog.Color;
                
                // Update color button appearance in the horizontal toolbar
                var colorButton = toolStrip.Items["ColorButton"] as ToolStripButton;
                if (colorButton != null)
                {
                    colorButton.ForeColor = annotationColor;
                }
            }
        }

        private void ClearAnnotations()
        {
            if (annotations.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to clear all annotations?", 
                    "Clear Annotations", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    annotations.Clear();
                    RedrawImage();
                }
            }
        }

        private void UndoLastAnnotation()
        {
            if (annotations.Count > 0)
            {
                annotations.RemoveAt(annotations.Count - 1);
                RedrawImage();
            }
        }

        private void ResetCounter()
        {
            counterNumber = 1;
            
            // Update the counter icon in the toolbar to show "1"
            var counterButton = toolStrip.Items.Cast<ToolStripItem>()
                .FirstOrDefault(item => item.ToolTipText == "Add Counter Number") as ToolStripButton;
            
            if (counterButton != null)
            {
                counterButton.Image?.Dispose();
                counterButton.Image = CreateCounterIcon();
            }
        }

        private void RedrawImage()
        {
            // Restore original image
            workingImage?.Dispose();
            workingImage = new Bitmap(screenshot);
            
            // Apply all annotations
            using (var g = Graphics.FromImage(workingImage))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                foreach (var annotation in annotations)
                {
                    DrawAnnotation(g, annotation);
                }
            }
            
            pictureBox.Image = workingImage;
            pictureBox.Invalidate();
            
            // Update the current screenshot item's working image
            SaveCurrentAnnotations();
        }

        private void DrawAnnotation(Graphics g, AnnotationObject annotation)
        {
            var rect = new Rectangle(
                Math.Min(annotation.StartPoint.X, annotation.EndPoint.X),
                Math.Min(annotation.StartPoint.Y, annotation.EndPoint.Y),
                Math.Abs(annotation.EndPoint.X - annotation.StartPoint.X),
                Math.Abs(annotation.EndPoint.Y - annotation.StartPoint.Y)
            );

            // Use slightly thicker line if selected (for visual feedback)
            var thickness = annotation.IsSelected ? annotation.Thickness + 1 : annotation.Thickness;
            var color = annotation.IsSelected ? Color.FromArgb(Math.Min(255, annotation.Color.R + 50), 
                Math.Min(255, annotation.Color.G + 50), Math.Min(255, annotation.Color.B + 50)) : annotation.Color;

            switch (annotation.Tool)
            {
                case AnnotationTool.Highlight:
                    using (var brush = new SolidBrush(Color.FromArgb(100, color)))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    break;

                case AnnotationTool.Rectangle:
                    using (var pen = new Pen(color, thickness))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                    break;

                case AnnotationTool.Line:
                    using (var pen = new Pen(color, thickness))
                    {
                        g.DrawLine(pen, annotation.StartPoint, annotation.EndPoint);
                    }
                    break;

                case AnnotationTool.Arrow:
                    DrawArrow(g, annotation.StartPoint, annotation.EndPoint, color, thickness);
                    break;

                case AnnotationTool.Text:
                    using (var brush = new SolidBrush(color))
                    {
                        g.DrawString(annotation.Text ?? "Text", annotation.Font, brush, annotation.StartPoint);
                    }
                    break;

                case AnnotationTool.Counter:
                    DrawCounter(g, annotation.StartPoint, annotation.CounterNumber, color, thickness);
                    break;
            }
        }

        private void DrawArrow(Graphics g, Point start, Point end, Color color, int thickness)
        {
            using (var pen = new Pen(color, thickness))
            {
                // Draw line
                g.DrawLine(pen, start, end);
                
                // Calculate arrow head
                var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
                var arrowLength = thickness * 3;
                var arrowAngle = Math.PI / 6; // 30 degrees
                
                var arrowPoint1 = new Point(
                    (int)(end.X - arrowLength * Math.Cos(angle - arrowAngle)),
                    (int)(end.Y - arrowLength * Math.Sin(angle - arrowAngle))
                );
                
                var arrowPoint2 = new Point(
                    (int)(end.X - arrowLength * Math.Cos(angle + arrowAngle)),
                    (int)(end.Y - arrowLength * Math.Sin(angle + arrowAngle))
                );
                
                // Draw arrow head
                g.DrawLine(pen, end, arrowPoint1);
                g.DrawLine(pen, end, arrowPoint2);
            }
        }

        private void DrawCounter(Graphics g, Point center, int number, Color color, int thickness)
        {
            var radius = Math.Max(15, thickness * 5);
            
            // Draw circle background
            using (var brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }
            
            // Draw circle border
            using (var pen = new Pen(Color.FromArgb(Math.Max(0, color.R - 50), 
                Math.Max(0, color.G - 50), Math.Max(0, color.B - 50)), Math.Max(1, thickness / 2)))
            {
                g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }
            
            // Draw number
            using (var brush = new SolidBrush(Color.White))
            using (var font = new Font("Segoe UI", Math.Max(8, radius / 2), FontStyle.Bold))
            {
                var text = number.ToString();
                var size = g.MeasureString(text, font);
                var x = center.X - size.Width / 2;
                var y = center.Y - size.Height / 2;
                g.DrawString(text, font, brush, x, y);
            }
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var imagePoint = new Point(
                    (int)((e.X + imagePanel.HorizontalScroll.Value) / zoomFactor),
                    (int)((e.Y + imagePanel.VerticalScroll.Value) / zoomFactor)
                );

                // Check if double-clicking on a text annotation to edit it
                foreach (var annotation in annotations)
                {
                    if (annotation.Tool == AnnotationTool.Text && annotation.ContainsPoint(imagePoint))
                    {
                        // Hide any existing text editor first
                        HideInlineTextEditor();
                        
                        // Show inline editor for this text annotation
                        ShowInlineTextEditor(annotation);
                        return;
                    }
                }
            }
        }

        // Mouse event handlers for annotation
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var imagePoint = new Point(
                    (int)((e.X + imagePanel.HorizontalScroll.Value) / zoomFactor),
                    (int)((e.Y + imagePanel.VerticalScroll.Value) / zoomFactor)
                );

                if (currentTool == AnnotationTool.Select)
                {
                    HandleSelectMouseDown(imagePoint);
                }
                else if (currentTool == AnnotationTool.Crop)
                {
                    HandleCropMouseDown(imagePoint);
                }
                else if (isAnnotating)
                {
                    HandleAnnotationMouseDown(imagePoint);
                }
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            var imagePoint = new Point(
                (int)((e.X + imagePanel.HorizontalScroll.Value) / zoomFactor),
                (int)((e.Y + imagePanel.VerticalScroll.Value) / zoomFactor)
            );

            if (currentTool == AnnotationTool.Select)
            {
                HandleSelectMouseMove(imagePoint);
            }
            else if (currentTool == AnnotationTool.Crop)
            {
                HandleCropMouseMove(imagePoint);
            }
            else if (isAnnotating && isDrawing)
            {
                HandleAnnotationMouseMove(imagePoint);
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var imagePoint = new Point(
                    (int)((e.X + imagePanel.HorizontalScroll.Value) / zoomFactor),
                    (int)((e.Y + imagePanel.VerticalScroll.Value) / zoomFactor)
                );

                if (currentTool == AnnotationTool.Select)
                {
                    HandleSelectMouseUp(imagePoint);
                }
                else if (currentTool == AnnotationTool.Crop)
                {
                    HandleCropMouseUp(imagePoint);
                }
                else if (isAnnotating && isDrawing)
                {
                    HandleAnnotationMouseUp(imagePoint);
                }
            }
        }

        private void HandleSelectMouseDown(Point imagePoint)
        {
            // Clear previous selection
            if (selectedAnnotation != null)
            {
                selectedAnnotation.IsSelected = false;
            }

            // Check if clicking on a resize handle of the selected annotation
            if (selectedAnnotation != null)
            {
                activeResizeHandle = GetResizeHandle(selectedAnnotation, imagePoint);
                if (activeResizeHandle != ResizeHandle.None)
                {
                    isResizing = true;
                    lastMousePosition = imagePoint;
                    return;
                }
            }

            // Try to select an annotation
            selectedAnnotation = null;
            for (int i = annotations.Count - 1; i >= 0; i--) // Check from top to bottom
            {
                if (annotations[i].ContainsPoint(imagePoint))
                {
                    selectedAnnotation = annotations[i];
                    selectedAnnotation.IsSelected = true;
                    isDragging = true;
                    lastMousePosition = imagePoint;
                    break;
                }
            }

            pictureBox.Invalidate();
        }

        private void HandleSelectMouseMove(Point imagePoint)
        {
            if (isResizing && selectedAnnotation != null)
            {
                ResizeAnnotation(selectedAnnotation, activeResizeHandle, imagePoint);
                RedrawImage();
            }
            else if (isDragging && selectedAnnotation != null)
            {
                var offset = new Point(
                    imagePoint.X - lastMousePosition.X,
                    imagePoint.Y - lastMousePosition.Y
                );
                selectedAnnotation.Move(offset);
                lastMousePosition = imagePoint;
                RedrawImage();
            }
            else
            {
                // Update cursor based on what's under the mouse
                UpdateCursor(imagePoint);
            }
        }

        private void HandleSelectMouseUp(Point imagePoint)
        {
            isDragging = false;
            isResizing = false;
            activeResizeHandle = ResizeHandle.None;
            pictureBox.Cursor = Cursors.Default;
        }

        private void HandleCropMouseDown(Point imagePoint)
        {
            isCropping = true;
            startPoint = imagePoint;
            currentPoint = startPoint;
            cropRectangle = Rectangle.Empty;
        }

        private void HandleCropMouseMove(Point imagePoint)
        {
            if (isCropping)
            {
                currentPoint = imagePoint;
                
                // Create crop rectangle from start and current points
                var x = Math.Min(startPoint.X, currentPoint.X);
                var y = Math.Min(startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - startPoint.X);
                var height = Math.Abs(currentPoint.Y - startPoint.Y);
                
                cropRectangle = new Rectangle(x, y, width, height);
                pictureBox.Invalidate();
            }
        }

        private void HandleCropMouseUp(Point imagePoint)
        {
            if (isCropping)
            {
                currentPoint = imagePoint;
                
                // Finalize crop rectangle
                var x = Math.Min(startPoint.X, currentPoint.X);
                var y = Math.Min(startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - startPoint.X);
                var height = Math.Abs(currentPoint.Y - startPoint.Y);
                
                cropRectangle = new Rectangle(x, y, width, height);
                
                // Only proceed if we have a meaningful crop area
                if (width > 10 && height > 10)
                {
                    statusLabel.Text = "Crop area selected. Press Enter to apply crop, or Escape to cancel.";
                }
                else
                {
                    cropRectangle = Rectangle.Empty;
                    statusLabel.Text = "Crop mode - Click and drag to select area to crop, press Enter to apply";
                }
                
                pictureBox.Invalidate();
            }
        }

        private void HandleAnnotationMouseDown(Point imagePoint)
        {
            isDrawing = true;
            startPoint = imagePoint;
            currentPoint = startPoint;
        }

        private void HandleAnnotationMouseMove(Point imagePoint)
        {
            currentPoint = imagePoint;
            pictureBox.Invalidate();
        }

        private void HandleAnnotationMouseUp(Point imagePoint)
        {
            isDrawing = false;

            if (currentTool == AnnotationTool.Text)
            {
                // For text tool, create inline text editor
                CreateInlineTextEditor(imagePoint);
                return;
            }

            if (currentTool == AnnotationTool.Counter)
            {
                // For counter tool, create counter annotation immediately
                var annotation = new AnnotationObject
                {
                    Tool = currentTool,
                    StartPoint = imagePoint,
                    EndPoint = imagePoint, // Not used for counter
                    Color = annotationColor,
                    Thickness = annotationThickness,
                    CounterNumber = counterNumber
                };

                annotations.Add(annotation);
                
                // Increment counter for next use
                counterNumber++;
                
                // Update the counter icon in the toolbar
                var counterButton = toolStrip.Items.Cast<ToolStripItem>()
                    .FirstOrDefault(item => item.ToolTipText == "Add Counter Number") as ToolStripButton;
                
                if (counterButton != null)
                {
                    counterButton.Image?.Dispose();
                    counterButton.Image = CreateCounterIcon();
                }

                RedrawImage();
                return;
            }

            var endPoint = imagePoint;

            // Only add annotation if there's actual movement
            if (Math.Abs(endPoint.X - startPoint.X) > 2 || Math.Abs(endPoint.Y - startPoint.Y) > 2)
            {
                var annotation = new AnnotationObject
                {
                    Tool = currentTool,
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    Color = annotationColor,
                    Thickness = annotationThickness
                };

                annotations.Add(annotation);
                RedrawImage();
            }
        }

        private void CreateInlineTextEditor(Point imagePoint)
        {
            // Create a new text annotation if we're not editing an existing one
            if (editingTextAnnotation == null)
            {
                editingTextAnnotation = new AnnotationObject
                {
                    Tool = AnnotationTool.Text,
                    StartPoint = imagePoint,
                    EndPoint = imagePoint,
                    Color = annotationColor,
                    Thickness = annotationThickness,
                    Text = "",
                    Font = new Font("Segoe UI", 12 + annotationThickness * 2, FontStyle.Regular)
                };
                annotations.Add(editingTextAnnotation);
            }

            ShowInlineTextEditor(editingTextAnnotation);
        }

        private void ShowInlineTextEditor(AnnotationObject textAnnotation)
        {
            // Remove any existing inline text editor
            HideInlineTextEditor();

            editingTextAnnotation = textAnnotation;

            // Calculate display position
            var displayPoint = new Point(
                (int)(textAnnotation.StartPoint.X * zoomFactor) - imagePanel.HorizontalScroll.Value,
                (int)(textAnnotation.StartPoint.Y * zoomFactor) - imagePanel.VerticalScroll.Value
            );

            // Adjust for picture box location within the image panel
            displayPoint.X += pictureBox.Left;
            displayPoint.Y += pictureBox.Top;

            // Create inline text box
            inlineTextBox = new TextBox
            {
                Text = textAnnotation.Text ?? "",
                Font = new Font("Segoe UI", Math.Max(8, (int)(textAnnotation.Font.Size * zoomFactor * 0.8f)), FontStyle.Regular),
                ForeColor = textAnnotation.Color,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = displayPoint,
                MinimumSize = new Size(100, 20),
                AutoSize = false,
                Multiline = true
            };

            // Set initial size based on current text or minimum size
            if (!string.IsNullOrEmpty(textAnnotation.Text))
            {
                using (var g = Graphics.FromImage(new Bitmap(1, 1)))
                {
                    var textSize = g.MeasureString(textAnnotation.Text, inlineTextBox.Font);
                    inlineTextBox.Size = new Size(
                        Math.Max(100, (int)textSize.Width + 10),
                        Math.Max(20, (int)textSize.Height + 5)
                    );
                }
            }
            else
            {
                inlineTextBox.Size = new Size(100, 20);
            }

            // Event handlers
            inlineTextBox.KeyDown += InlineTextBox_KeyDown;
            inlineTextBox.LostFocus += InlineTextBox_LostFocus;
            inlineTextBox.TextChanged += InlineTextBox_TextChanged;

            // Add to image panel (so it moves with the image)
            imagePanel.Controls.Add(inlineTextBox);
            inlineTextBox.BringToFront();
            inlineTextBox.Focus();
            inlineTextBox.SelectAll();
        }

        private void InlineTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    // Enter without Shift confirms the text
                    e.Handled = true;
                    FinishTextEditing();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    // Escape cancels text editing
                    e.Handled = true;
                    CancelTextEditing();
                }
            }
            catch (Exception ex)
            {
                // Log error and safely clean up
                MessageBox.Show($"Error in text editing: {ex.Message}", "Text Editing Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                HideInlineTextEditor();
            }
        }

        private void InlineTextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                // Auto-save when losing focus, but only if still valid
                if (inlineTextBox != null && !inlineTextBox.IsDisposed && editingTextAnnotation != null)
                {
                    FinishTextEditing();
                }
            }
            catch (Exception ex)
            {
                // Handle focus loss errors gracefully
                MessageBox.Show($"Error saving text on focus loss: {ex.Message}", "Text Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                HideInlineTextEditor();
            }
        }

        private void InlineTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (inlineTextBox != null && !inlineTextBox.IsDisposed && editingTextAnnotation != null)
                {
                    // Update the annotation text in real-time
                    editingTextAnnotation.Text = inlineTextBox.Text;
                    
                    // Auto-resize the text box
                    using (var g = Graphics.FromImage(new Bitmap(1, 1)))
                    {
                        var textSize = g.MeasureString(inlineTextBox.Text + "W", inlineTextBox.Font); // Add extra char for padding
                        inlineTextBox.Size = new Size(
                            Math.Max(100, (int)textSize.Width + 10),
                            Math.Max(20, (int)textSize.Height + 5)
                        );
                    }
                    
                    // Redraw to show updated text
                    RedrawImage();
                }
            }
            catch (Exception)
            {
                // Silently handle text change errors to avoid disrupting user typing
            }
        }

        private void FinishTextEditing()
        {
            try
            {
                if (editingTextAnnotation != null && inlineTextBox != null)
                {
                    editingTextAnnotation.Text = inlineTextBox.Text;

                    // Remove annotation if text is empty
                    if (string.IsNullOrWhiteSpace(editingTextAnnotation.Text))
                    {
                        annotations.Remove(editingTextAnnotation);
                    }

                    // Safely redraw the image
                    RedrawImage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error finishing text editing: {ex.Message}", "Text Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // Always hide the editor, even if there was an error
                HideInlineTextEditor();
            }
        }

        private void CancelTextEditing()
        {
            if (editingTextAnnotation != null)
            {
                // If this was a new annotation (empty text), remove it
                if (string.IsNullOrEmpty(editingTextAnnotation.Text))
                {
                    annotations.Remove(editingTextAnnotation);
                    RedrawImage();
                }
            }

            HideInlineTextEditor();
        }

        private void HideInlineTextEditor()
        {
            try
            {
                if (inlineTextBox != null)
                {
                    // Remove event handlers first to prevent recursive calls
                    inlineTextBox.KeyDown -= InlineTextBox_KeyDown;
                    inlineTextBox.LostFocus -= InlineTextBox_LostFocus;
                    inlineTextBox.TextChanged -= InlineTextBox_TextChanged;
                    
                    // Remove from panel
                    if (imagePanel != null && imagePanel.Controls.Contains(inlineTextBox))
                    {
                        imagePanel.Controls.Remove(inlineTextBox);
                    }
                    
                    // Dispose safely
                    inlineTextBox.Dispose();
                    inlineTextBox = null;
                }
                editingTextAnnotation = null;
            }
            catch (Exception)
            {
                // Silently handle disposal errors and ensure cleanup
                inlineTextBox = null;
                editingTextAnnotation = null;
            }
        }

        private void ShowTextInputDialog(Point position)
        {
            var inputForm = new Form
            {
                Text = "Enter Text",
                Size = new Size(300, 120),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            var textBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(260, 20),
                Font = new Font("Segoe UI", 10)
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(195, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(115, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            inputForm.Controls.AddRange(new Control[] { textBox, okButton, cancelButton });
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            textBox.Focus();
            textBox.SelectAll();

            if (inputForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var annotation = new AnnotationObject
                {
                    Tool = AnnotationTool.Text,
                    StartPoint = position,
                    EndPoint = position, // Not used for text
                    Color = annotationColor,
                    Thickness = annotationThickness,
                    Text = textBox.Text,
                    Font = new Font("Segoe UI", 12 + annotationThickness * 2, FontStyle.Regular)
                };

                annotations.Add(annotation);
                RedrawImage();
            }

            inputForm.Dispose();
        }

        private ResizeHandle GetResizeHandle(AnnotationObject annotation, Point point)
        {
            var bounds = annotation.GetBounds();
            var handleSize = 8;

            // Text annotations only support moving, not resizing
            if (annotation.Tool == AnnotationTool.Text)
            {
                return ResizeHandle.None;
            }

            // Check corner handles first
            if (IsPointInHandle(point, bounds.Location, handleSize))
                return ResizeHandle.TopLeft;
            if (IsPointInHandle(point, new Point(bounds.Right, bounds.Top), handleSize))
                return ResizeHandle.TopRight;
            if (IsPointInHandle(point, new Point(bounds.Left, bounds.Bottom), handleSize))
                return ResizeHandle.BottomLeft;
            if (IsPointInHandle(point, new Point(bounds.Right, bounds.Bottom), handleSize))
                return ResizeHandle.BottomRight;

            // Check edge handles for rectangles and highlights (not for lines/arrows)
            if (annotation.Tool == AnnotationTool.Rectangle || annotation.Tool == AnnotationTool.Highlight)
            {
                if (IsPointInHandle(point, new Point(bounds.Left + bounds.Width / 2, bounds.Top), handleSize))
                    return ResizeHandle.Top;
                if (IsPointInHandle(point, new Point(bounds.Left + bounds.Width / 2, bounds.Bottom), handleSize))
                    return ResizeHandle.Bottom;
                if (IsPointInHandle(point, new Point(bounds.Left, bounds.Top + bounds.Height / 2), handleSize))
                    return ResizeHandle.Left;
                if (IsPointInHandle(point, new Point(bounds.Right, bounds.Top + bounds.Height / 2), handleSize))
                    return ResizeHandle.Right;
            }

            return ResizeHandle.None;
        }

        private bool IsPointInHandle(Point point, Point handleCenter, int handleSize)
        {
            var handleRect = new Rectangle(
                handleCenter.X - handleSize / 2,
                handleCenter.Y - handleSize / 2,
                handleSize,
                handleSize
            );
            return handleRect.Contains(point);
        }

        private void ResizeAnnotation(AnnotationObject annotation, ResizeHandle handle, Point newPoint)
        {
            var bounds = annotation.GetBounds();

            switch (handle)
            {
                case ResizeHandle.TopLeft:
                    annotation.StartPoint = new Point(
                        Math.Min(newPoint.X, annotation.EndPoint.X - 5),
                        Math.Min(newPoint.Y, annotation.EndPoint.Y - 5)
                    );
                    break;
                case ResizeHandle.TopRight:
                    annotation.StartPoint = new Point(annotation.StartPoint.X, Math.Min(newPoint.Y, annotation.EndPoint.Y - 5));
                    annotation.EndPoint = new Point(Math.Max(newPoint.X, annotation.StartPoint.X + 5), annotation.EndPoint.Y);
                    break;
                case ResizeHandle.BottomLeft:
                    annotation.StartPoint = new Point(Math.Min(newPoint.X, annotation.EndPoint.X - 5), annotation.StartPoint.Y);
                    annotation.EndPoint = new Point(annotation.EndPoint.X, Math.Max(newPoint.Y, annotation.StartPoint.Y + 5));
                    break;
                case ResizeHandle.BottomRight:
                    annotation.EndPoint = new Point(
                        Math.Max(newPoint.X, annotation.StartPoint.X + 5),
                        Math.Max(newPoint.Y, annotation.StartPoint.Y + 5)
                    );
                    break;
                case ResizeHandle.Top:
                    annotation.StartPoint = new Point(annotation.StartPoint.X, Math.Min(newPoint.Y, annotation.EndPoint.Y - 5));
                    break;
                case ResizeHandle.Bottom:
                    annotation.EndPoint = new Point(annotation.EndPoint.X, Math.Max(newPoint.Y, annotation.StartPoint.Y + 5));
                    break;
                case ResizeHandle.Left:
                    annotation.StartPoint = new Point(Math.Min(newPoint.X, annotation.EndPoint.X - 5), annotation.StartPoint.Y);
                    break;
                case ResizeHandle.Right:
                    annotation.EndPoint = new Point(Math.Max(newPoint.X, annotation.StartPoint.X + 5), annotation.EndPoint.Y);
                    break;
            }
        }

        private void UpdateCursor(Point imagePoint)
        {
            var cursor = Cursors.Default;

            // Check if over a selected annotation's resize handle
            if (selectedAnnotation != null)
            {
                var handle = GetResizeHandle(selectedAnnotation, imagePoint);
                cursor = handle switch
                {
                    ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
                    ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
                    ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
                    ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
                    _ => Cursors.Default
                };
            }

            // Check if over any annotation
            if (cursor == Cursors.Default)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation.ContainsPoint(imagePoint))
                    {
                        cursor = annotation.IsSelected ? Cursors.SizeAll : Cursors.Hand;
                        break;
                    }
                }
            }

            pictureBox.Cursor = cursor;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw current annotation preview while drawing
            if (isAnnotating && isDrawing && currentTool != AnnotationTool.Select)
            {
                // Convert coordinates to display coordinates
                var displayStart = new Point(
                    (int)(startPoint.X * zoomFactor),
                    (int)(startPoint.Y * zoomFactor)
                );
                var displayCurrent = new Point(
                    (int)(currentPoint.X * zoomFactor),
                    (int)(currentPoint.Y * zoomFactor)
                );

                var displayThickness = Math.Max(1, (int)(annotationThickness * zoomFactor));

                // Draw preview of current annotation
                switch (currentTool)
                {
                    case AnnotationTool.Highlight:
                        var rect = new Rectangle(
                            Math.Min(displayStart.X, displayCurrent.X),
                            Math.Min(displayStart.Y, displayCurrent.Y),
                            Math.Abs(displayCurrent.X - displayStart.X),
                            Math.Abs(displayCurrent.Y - displayStart.Y)
                        );
                        using (var brush = new SolidBrush(Color.FromArgb(100, annotationColor)))
                        {
                            g.FillRectangle(brush, rect);
                        }
                        break;

                    case AnnotationTool.Rectangle:
                        var rectBounds = new Rectangle(
                            Math.Min(displayStart.X, displayCurrent.X),
                            Math.Min(displayStart.Y, displayCurrent.Y),
                            Math.Abs(displayCurrent.X - displayStart.X),
                            Math.Abs(displayCurrent.Y - displayStart.Y)
                        );
                        using (var pen = new Pen(annotationColor, displayThickness))
                        {
                            g.DrawRectangle(pen, rectBounds);
                        }
                        break;

                    case AnnotationTool.Line:
                        using (var pen = new Pen(annotationColor, displayThickness))
                        {
                            g.DrawLine(pen, displayStart, displayCurrent);
                        }
                        break;

                    case AnnotationTool.Arrow:
                        using (var pen = new Pen(annotationColor, displayThickness))
                        {
                            g.DrawLine(pen, displayStart, displayCurrent);

                            // Draw arrow head preview
                            var angle = Math.Atan2(displayCurrent.Y - displayStart.Y, displayCurrent.X - displayStart.X);
                            var arrowLength = displayThickness * 3;
                            var arrowAngle = Math.PI / 6;

                            var arrowPoint1 = new Point(
                                (int)(displayCurrent.X - arrowLength * Math.Cos(angle - arrowAngle)),
                                (int)(displayCurrent.Y - arrowLength * Math.Sin(angle - arrowAngle))
                            );

                            var arrowPoint2 = new Point(
                                (int)(displayCurrent.X - arrowLength * Math.Cos(angle + arrowAngle)),
                                (int)(displayCurrent.Y - arrowLength * Math.Sin(angle + arrowAngle))
                            );

                            g.DrawLine(pen, displayCurrent, arrowPoint1);
                            g.DrawLine(pen, displayCurrent, arrowPoint2);
                        }
                        break;
                }
            }

            // Draw selection handles for selected annotation
            if (selectedAnnotation != null && currentTool == AnnotationTool.Select)
            {
                DrawSelectionHandles(g, selectedAnnotation);
            }
        }

        private void DrawSelectionHandles(Graphics g, AnnotationObject annotation)
        {
            var bounds = annotation.GetBounds();
            var handleSize = 8;
            var displayHandleSize = Math.Max(6, (int)(handleSize * zoomFactor));

            // Convert bounds to display coordinates
            var displayBounds = new Rectangle(
                (int)(bounds.X * zoomFactor),
                (int)(bounds.Y * zoomFactor),
                (int)(bounds.Width * zoomFactor),
                (int)(bounds.Height * zoomFactor)
            );

            // Selection border
            using (var pen = new Pen(Color.Blue, 1))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(pen, displayBounds);
            }

            // For text annotations, only show corner handles (no edge handles)
            if (annotation.Tool == AnnotationTool.Text)
            {
                // Corner handles only
                DrawHandle(g, displayBounds.Location, displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Right, displayBounds.Top), displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Left, displayBounds.Bottom), displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Right, displayBounds.Bottom), displayHandleSize);
            }
            else
            {
                // Corner handles
                DrawHandle(g, displayBounds.Location, displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Right, displayBounds.Top), displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Left, displayBounds.Bottom), displayHandleSize);
                DrawHandle(g, new Point(displayBounds.Right, displayBounds.Bottom), displayHandleSize);

                // Edge handles for rectangles and highlights (not for lines/arrows)
                if (annotation.Tool == AnnotationTool.Rectangle || annotation.Tool == AnnotationTool.Highlight)
                {
                    DrawHandle(g, new Point(displayBounds.Left + displayBounds.Width / 2, displayBounds.Top), displayHandleSize);
                    DrawHandle(g, new Point(displayBounds.Left + displayBounds.Width / 2, displayBounds.Bottom), displayHandleSize);
                    DrawHandle(g, new Point(displayBounds.Left, displayBounds.Top + displayBounds.Height / 2), displayHandleSize);
                    DrawHandle(g, new Point(displayBounds.Right, displayBounds.Top + displayBounds.Height / 2), displayHandleSize);
                }
            }
        }

        private void DrawHandle(Graphics g, Point center, int size)
        {
            var handleRect = new Rectangle(
                center.X - size / 2,
                center.Y - size / 2,
                size,
                size
            );

            // White fill with blue border
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, handleRect);
            }
            using (var pen = new Pen(Color.Blue, 1))
            {
                g.DrawRectangle(pen, handleRect);
            }
        }

        private void UpdateImageDisplay()
        {
            if (screenshot == null) return;

            var newSize = new Size(
                (int)(screenshot.Width * zoomFactor),
                (int)(screenshot.Height * zoomFactor)
            );

            pictureBox.Size = newSize;
            CenterImage();
            UpdateStatusLabels();
            
            // Update inline text editor position and size if active
            UpdateInlineTextEditorPosition();
        }

        private void UpdateInlineTextEditorPosition()
        {
            if (inlineTextBox != null && editingTextAnnotation != null)
            {
                // Recalculate display position based on current zoom
                var displayPoint = new Point(
                    (int)(editingTextAnnotation.StartPoint.X * zoomFactor),
                    (int)(editingTextAnnotation.StartPoint.Y * zoomFactor)
                );

                // Adjust for picture box location within the image panel
                displayPoint.X += pictureBox.Left;
                displayPoint.Y += pictureBox.Top;

                inlineTextBox.Location = displayPoint;

                // Update font size based on zoom
                var newFontSize = Math.Max(8, (int)(editingTextAnnotation.Font.Size * zoomFactor * 0.8f));
                if (inlineTextBox.Font.Size != newFontSize)
                {
                    inlineTextBox.Font = new Font("Segoe UI", newFontSize, FontStyle.Regular);
                }
            }
        }

        private void CenterImage()
        {
            if (imagePanel.ClientSize.Width > pictureBox.Width)
                pictureBox.Left = (imagePanel.ClientSize.Width - pictureBox.Width) / 2;
            else
                pictureBox.Left = 0;

            if (imagePanel.ClientSize.Height > pictureBox.Height)
                pictureBox.Top = (imagePanel.ClientSize.Height - pictureBox.Height) / 2;
            else
                pictureBox.Top = 0;
        }

        private void UpdateStatusLabels()
        {
            if (screenshot != null)
            {
                sizeLabel.Text = $"Size: {screenshot.Width} × {screenshot.Height} pixels";
                zoomLabel.Text = $"Zoom: {zoomFactor * 100:F0}%";
            }
        }

        // Zoom and view methods
        private void ZoomIn()
        {
            zoomFactor = Math.Min(zoomFactor * 1.25f, 10.0f);
            UpdateImageDisplay();
        }

        private void ZoomOut()
        {
            zoomFactor = Math.Max(zoomFactor / 1.25f, 0.1f);
            UpdateImageDisplay();
        }

        private void ActualSize()
        {
            zoomFactor = 1.0f;
            UpdateImageDisplay();
        }

        private void FitToWindow()
        {
            if (screenshot == null) return;

            var availableWidth = imagePanel.ClientSize.Width - 20;
            var availableHeight = imagePanel.ClientSize.Height - 20;

            var scaleX = (float)availableWidth / screenshot.Width;
            var scaleY = (float)availableHeight / screenshot.Height;

            zoomFactor = Math.Min(scaleX, scaleY);
            zoomFactor = Math.Max(zoomFactor, 0.1f);
            zoomFactor = Math.Min(zoomFactor, 10.0f);

            UpdateImageDisplay();
        }

        private void ToggleFullscreen()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        // Action methods
        private async void QuickSave()
        {
            try
            {
                statusLabel.Text = "Saving...";
                this.Cursor = Cursors.WaitCursor;

                await Task.Run(() =>
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var filePath = Path.Combine(desktop, fileName);
                    screenshot.Save(filePath, ImageFormat.Png);
                });

                statusLabel.Text = "Screenshot saved to desktop";
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Save failed";
                MessageBox.Show($"Error saving screenshot: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void SaveAs()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PNG files (*.png)|*.png|JPEG files (*.jpg)|*.jpg|BMP files (*.bmp)|*.bmp|GIF files (*.gif)|*.gif|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    statusLabel.Text = "Saving...";
                    this.Cursor = Cursors.WaitCursor;

                    // Prepare image with annotations for saving
                    Bitmap imageToSave = null;
                    
                    // Method 1: Create image with annotations
                    if (pictureBox.Image != null)
                    {
                        imageToSave = new Bitmap(pictureBox.Image.Width, pictureBox.Image.Height);
                        using (var g = Graphics.FromImage(imageToSave))
                        {
                            // Draw the base image
                            g.DrawImage(pictureBox.Image, 0, 0);
                            
                            // Draw annotations in image coordinates
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            foreach (var annotation in annotations)
                            {
                                DrawAnnotation(g, annotation);
                            }
                        }
                    }
                    else
                    {
                        // Fallback: Use working image or screenshot
                        imageToSave = workingImage ?? screenshot;
                    }
                    
                    await Task.Run(() =>
                    {
                        var format = saveDialog.FilterIndex switch
                        {
                            2 => ImageFormat.Jpeg,
                            3 => ImageFormat.Bmp,
                            4 => ImageFormat.Gif,
                            _ => ImageFormat.Png
                        };

                        imageToSave.Save(saveDialog.FileName, format);
                    });
                    
                    // Clean up if we created a new image
                    if (imageToSave != workingImage && imageToSave != screenshot)
                    {
                        imageToSave?.Dispose();
                    }

                    statusLabel.Text = $"Screenshot saved as {Path.GetFileName(saveDialog.FileName)}";
                    this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "Save failed";
                    MessageBox.Show($"Error saving screenshot: {ex.Message}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                statusLabel.Text = "Copying to clipboard...";
                
                // Method 1: Try using the rendered image from pictureBox  
                if (pictureBox.Image != null)
                {
                    // Create a copy of the displayed image
                    var imageWithAnnotations = new Bitmap(pictureBox.Image.Width, pictureBox.Image.Height);
                    using (var g = Graphics.FromImage(imageWithAnnotations))
                    {
                        // Draw the base image
                        g.DrawImage(pictureBox.Image, 0, 0);
                        
                        // Draw annotations in image coordinates
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        foreach (var annotation in annotations)
                        {
                            DrawAnnotation(g, annotation);
                        }
                    }
                    
                    Clipboard.SetImage(imageWithAnnotations);
                    imageWithAnnotations.Dispose();
                }
                else
                {
                    // Fallback: Ensure current annotations are applied to working image
                    RedrawImage();
                    
                    // Copy the working image which already has all annotations rendered
                    if (workingImage != null)
                    {
                        Clipboard.SetImage(workingImage);
                    }
                    else
                    {
                        // Final fallback to original screenshot
                        Clipboard.SetImage(screenshot);
                    }
                }
                
                statusLabel.Text = "Screenshot copied to clipboard";
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Copy failed";
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Copy Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSettings()
        {
            MessageBox.Show("Settings panel will be implemented in a future version.", "Settings",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show("Blueshot - Screen Capture Tool\nInspired by Greenshot\n\nVersion 1.0\nBuilt with C# and WinForms",
                "About Blueshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Event handlers
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // If text editor is active, let it handle most keys
            if (inlineTextBox != null && inlineTextBox.Focused)
            {
                // Only handle Escape to cancel editing
                if (e.KeyCode == Keys.Escape)
                {
                    CancelTextEditing();
                    e.Handled = true;
                }
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (selectedAnnotation != null)
                    {
                        // Deselect annotation
                        selectedAnnotation.IsSelected = false;
                        selectedAnnotation = null;
                        RedrawImage();
                        e.Handled = true;
                    }
                    else
                    {
                        this.Close();
                    }
                    break;
                case Keys.Delete:
                    if (selectedAnnotation != null)
                    {
                        annotations.Remove(selectedAnnotation);
                        selectedAnnotation = null;
                        RedrawImage();
                        e.Handled = true;
                    }
                    break;
                case Keys.Enter:
                case Keys.S when e.Control && !e.Shift:
                    QuickSave();
                    break;
                case Keys.S when e.Control && e.Shift:
                    SaveAs();
                    break;
                case Keys.C when e.Control:
                    CopyToClipboard();
                    break;
                case Keys.Oemplus when e.Control:
                case Keys.Add when e.Control:
                    ZoomIn();
                    break;
                case Keys.OemMinus when e.Control:
                case Keys.Subtract when e.Control:
                    ZoomOut();
                    break;
                case Keys.D0 when e.Control:
                case Keys.NumPad0 when e.Control:
                    ActualSize();
                    break;
                case Keys.F when e.Control:
                    FitToWindow();
                    break;
                case Keys.F11:
                    ToggleFullscreen();
                    break;
                case Keys.Left:
                case Keys.PageUp:
                    if (screenshots.Count > 1)
                    {
                        NavigateToScreenshot(currentScreenshotIndex - 1);
                        e.Handled = true;
                    }
                    break;
                case Keys.Right:
                case Keys.PageDown:
                    if (screenshots.Count > 1)
                    {
                        NavigateToScreenshot(currentScreenshotIndex + 1);
                        e.Handled = true;
                    }
                    break;
                case Keys.Home:
                    if (screenshots.Count > 1)
                    {
                        NavigateToScreenshot(0);
                        e.Handled = true;
                    }
                    break;
                case Keys.End:
                    if (screenshots.Count > 1)
                    {
                        NavigateToScreenshot(screenshots.Count - 1);
                        e.Handled = true;
                    }
                    break;
                case Keys.D1:
                    SetAnnotationTool(AnnotationTool.Select);
                    e.Handled = true;
                    break;
                case Keys.D2:
                    SetAnnotationTool(AnnotationTool.Highlight);
                    e.Handled = true;
                    break;
                case Keys.D3:
                    SetAnnotationTool(AnnotationTool.Rectangle);
                    e.Handled = true;
                    break;
                case Keys.D4:
                    SetAnnotationTool(AnnotationTool.Line);
                    e.Handled = true;
                    break;
                case Keys.D5:
                    SetAnnotationTool(AnnotationTool.Arrow);
                    e.Handled = true;
                    break;
                case Keys.D6:
                    SetAnnotationTool(AnnotationTool.Text);
                    e.Handled = true;
                    break;
            }
        }

        public void RemoveScreenshot(int index)
        {
            if (index >= 0 && index < screenshots.Count)
            {
                screenshots[index].Dispose();
                screenshots.RemoveAt(index);
                
                if (screenshots.Count == 0)
                {
                    this.Close();
                    return;
                }
                
                // Adjust current index if necessary
                if (currentScreenshotIndex >= screenshots.Count)
                {
                    currentScreenshotIndex = screenshots.Count - 1;
                }
                else if (currentScreenshotIndex > index)
                {
                    currentScreenshotIndex--;
                }
                
                LoadScreenshot(currentScreenshotIndex);
                UpdateThumbnailPanel();
                UpdateNavigationButtons();
                UpdateScreenshotCounter();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            // Finish any active text editing
            if (inlineTextBox != null)
            {
                FinishTextEditing();
            }
            
            // Save current annotations before closing
            SaveCurrentAnnotations();
            
            pictureBox.Image = null;
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            CenterImage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all screenshots
                foreach (var screenshotItem in screenshots)
                {
                    screenshotItem.Dispose();
                }
                screenshots.Clear();
                
                // Dispose working images
                screenshot?.Dispose();
                workingImage?.Dispose();
                pictureBox?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Placeholder for resources - in a real app, these would be actual icons
    internal static class Properties
    {
        internal static class Resources
        {
            internal static Image Save => null;
            internal static Image SaveAs => null;
            internal static Image Copy => null;
            internal static Image ZoomIn => null;
            internal static Image ZoomOut => null;
            internal static Image ActualSize => null;
            internal static Image FitToWindow => null;
            internal static Image Settings => null;
            internal static Image About => null;
        }
    }
}
