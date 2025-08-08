using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

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
        private Panel mainContainer; // Container with scroll bars
        private Panel fileExplorerPanel; // File explorer panel
        private TreeView fileExplorerTreeView; // File explorer tree view
        private ContextMenuStrip fileExplorerContextMenu; // Context menu for file explorer
        private ContextMenuStrip homeButtonContextMenu; // Context menu for home button
        private string customHomePath = ""; // Custom home path
        private bool isFileExplorerVisible = true; // Always visible
        private Bitmap screenshot;
        private Bitmap workingImage;
        private float zoomFactor = 1.0f;

        // Screenshot management
        private List<ScreenshotItem> screenshots = new List<ScreenshotItem>();
        private int currentScreenshotIndex = 0;
        private Panel thumbnailPanel;
        private RoundedPanel thumbnailPanelRounded;
        private RoundedButton previousButton;
        private RoundedButton nextButton;
        private Label screenshotCountLabel;

        // Annotation tools
        private bool isAnnotating = false;
        private AnnotationTool currentTool = AnnotationTool.None;
        private Color annotationFillColor = Color.FromArgb(100, Color.Yellow); // Semi-transparent yellow for fills
        private Color annotationLineColor = Color.FromArgb(255, Color.Red); // Solid red for lines/borders
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
        
        // Direct text input without TextBox control
        private bool isTypingText = false;
        private string currentTypingText = "";
        private int textCursorPosition = 0;

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
                if (originalImage == null)
                {
                    throw new ArgumentNullException(nameof(originalImage), "Original image cannot be null");
                }
                
                if (originalImage.Width <= 0 || originalImage.Height <= 0)
                {
                    throw new ArgumentException("Original image must have valid dimensions", nameof(originalImage));
                }
                
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
            public Color FillColor { get; set; } = Color.FromArgb(100, Color.Yellow);
            public Color LineColor { get; set; } = Color.Red;
            public int Thickness { get; set; }
            public bool IsSelected { get; set; }
            public string Text { get; set; } = "";
            public Font Font { get; set; } = new Font("Segoe UI", 12, FontStyle.Regular);
            public int CounterNumber { get; set; } = 0;
            public bool IsTextRegion { get; set; } = false; // Flag for region-based text

            // Legacy property for backward compatibility
            public Color Color 
            { 
                get => LineColor; 
                set => LineColor = value; 
            }

            public Rectangle GetBounds()
            {
                if (Tool == AnnotationTool.Text)
                {
                    if (IsTextRegion)
                    {
                        // For region-based text, use the actual region bounds
                        return new Rectangle(
                            Math.Min(StartPoint.X, EndPoint.X),
                            Math.Min(StartPoint.Y, EndPoint.Y),
                            Math.Abs(EndPoint.X - StartPoint.X),
                            Math.Abs(EndPoint.Y - StartPoint.Y)
                        );
                    }
                    else
                    {
                        // For point-based text, calculate bounds based on text size
                        using (var g = Graphics.FromImage(new Bitmap(1, 1)))
                        {
                            var textSize = g.MeasureString(Text ?? "Sample", Font);
                            return new Rectangle(StartPoint.X, StartPoint.Y, (int)textSize.Width, (int)textSize.Height);
                        }
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
                
                // Move EndPoint for all annotations except point-based text annotations
                if (Tool != AnnotationTool.Text || IsTextRegion)
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
            try
            {
                InitializeComponent();
                
                if (capturedImage != null)
                {
                    AddScreenshot(capturedImage);
                }
                
                SetupPreview();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PreviewForm constructor: {ex.Message}");
                MessageBox.Show($"Error creating preview window: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add a new constructor for existing instances
        public PreviewForm()
        {
            InitializeComponent();
            SetupPreview();
        }

        public void AddScreenshot(Bitmap capturedImage)
        {
            try
            {
                if (capturedImage == null)
                {
                    Logger.LogError("AddScreenshot called with null image");
                    MessageBox.Show("Cannot add screenshot: Image is null", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (capturedImage.Width <= 0 || capturedImage.Height <= 0)
                {
                    Logger.LogError($"AddScreenshot called with invalid dimensions: {capturedImage.Width}x{capturedImage.Height}");
                    MessageBox.Show($"Cannot add screenshot: Invalid image dimensions ({capturedImage.Width}x{capturedImage.Height})", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

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
                if (thumbnailPanelRounded != null)
                {
                    UpdateThumbnailPanel();
                    UpdateNavigationButtons();
                    UpdateScreenshotCounter();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in AddScreenshot: {ex.Message}");
                MessageBox.Show($"Error adding screenshot: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            // Set borderless style first, then maximize to respect taskbar
            this.FormBorderStyle = FormBorderStyle.None; // Hide title bar
            this.WindowState = FormWindowState.Normal; // Start normal, then maximize properly
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.ShowInTaskbar = true; // Ensure form shows in taskbar
            this.Icon = LoadApplicationIcon();
            this.MinimumSize = new Size(600, 400);

            // Create menu strip (but don't add it to controls - hidden)
            CreateMenuStrip();

            // Create toolbar
            CreateToolStrip();

            // Create annotation toolbar
            CreateAnnotationToolStrip();

            // Create file explorer panel
            CreateFileExplorerPanel();

            // Create file explorer context menu
            CreateFileExplorerContextMenu();

            // Create home button context menu
            CreateHomeButtonContextMenu();

            // Load settings (custom home path)
            LoadSettings();

            // Create main image panel with scrolling
            CreateImagePanel();

            // Create status strip
            CreateStatusStrip();

            // Set up layout (without menu strip)
            this.Controls.Add(imagePanel);
            this.Controls.Add(CreateFileExplorerSplitter());
            this.Controls.Add(fileExplorerPanel);
            this.Controls.Add(toolStrip);
            this.Controls.Add(statusStrip);

            // Initialize file explorer to home folder
            InitializeFileExplorerToHome();

            // Event handlers
            this.KeyDown += OnKeyDown;
            this.KeyPress += OnKeyPress;
            this.FormClosing += OnFormClosing;
            this.Resize += OnFormResize;
            this.SizeChanged += OnSizeChanged;
            this.Load += OnFormLoad; // Add Load event handler
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            // Set window to maximize while respecting taskbar
            MaximizeRespectingTaskbar();
        }

        private Icon LoadApplicationIcon()
        {
            try
            {
                // Try Application.StartupPath first (same as MainForm)
                string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
                if (File.Exists(iconPath))
                {
                    var customIcon = new Icon(iconPath);
                    
                    // Ensure the icon is valid
                    if (customIcon.Width > 0 && customIcon.Height > 0)
                    {
                        return customIcon;
                    }
                }
                
                // Also try relative path as fallback
                string relativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(relativePath))
                {
                    var customIcon = new Icon(relativePath);
                    if (customIcon.Width > 0 && customIcon.Height > 0)
                    {
                        return customIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load custom application icon", "LoadApplicationIcon", ex);
            }
            
            // Fallback to system application icon
            return SystemIcons.Application;
        }

        private void MaximizeRespectingTaskbar()
        {
            // Get the working area (screen minus taskbar)
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            
            // Set window bounds to working area
            this.Bounds = workingArea;
        }

        private bool IsEffectivelyMaximized()
        {
            // Check if window is maximized either by WindowState or by bounds matching working area
            return this.WindowState == FormWindowState.Maximized || 
                   this.Bounds.Equals(Screen.FromControl(this).WorkingArea);
        }

        protected override void SetVisibleCore(bool value)
        {
            // Ensure the form is always visible in taskbar when shown
            if (value)
            {
                this.ShowInTaskbar = true;
            }
            base.SetVisibleCore(value);
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
                ImageScalingSize = new Size(32, 32),
                RenderMode = ToolStripRenderMode.ManagerRenderMode,
                Renderer = new RoundedToolStripRenderer(6, Color.FromArgb(245, 245, 245), Color.FromArgb(200, 200, 200))
            };

            // File Explorer button (first button) - now shows home folder
            var fileExplorerButton = new ToolStripButton("", CreateFileExplorerIcon(), (s, e) => GoToHomeFolder())
            {
                ToolTipText = "Go to Home Folder",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Checked = true // Always visible
            };

            // Separator after file explorer
            var separatorFileExplorer = new ToolStripSeparator();

            // Save buttons
            var saveButton = new ToolStripButton("", CreateSaveIcon(), (s, e) => QuickSave())
            {
                ToolTipText = "Save to Selected Folder (Ctrl+S)",
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

            // Fill color selector for annotation
            var fillColorLabel = new ToolStripLabel("Fill:");
            var fillColorButton = new ToolStripButton("■", null, ShowFillColorPicker)
            {
                ToolTipText = "Choose Fill Color",
                ForeColor = annotationFillColor,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Name = "FillColorButton"
            };

            // Line color selector for annotation
            var lineColorLabel = new ToolStripLabel("Line:");
            var lineColorButton = new ToolStripButton("■", null, ShowLineColorPicker)
            {
                ToolTipText = "Choose Line Color",
                ForeColor = annotationLineColor,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Name = "LineColorButton"
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

            // Window control buttons
            var minimizeButton = new ToolStripButton("", CreateMinimizeIcon(), (s, e) => this.WindowState = FormWindowState.Minimized)
            {
                ToolTipText = "Minimize Window",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Alignment = ToolStripItemAlignment.Right
            };

            var maximizeButton = new ToolStripButton("", CreateMaximizeIcon(), (s, e) => ToggleMaximize())
            {
                ToolTipText = "Maximize/Restore Window",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Alignment = ToolStripItemAlignment.Right
            };

            // Close button
            var closeButton = new ToolStripButton("", CreateCloseIcon(), (s, e) => this.Close())
            {
                ToolTipText = "Close Preview (Esc)",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Alignment = ToolStripItemAlignment.Right
            };

            toolStrip.Items.AddRange(new ToolStripItem[] {
                fileExplorerButton, separatorFileExplorer,
                saveButton, copyButton, separator1,
                zoomInButton, zoomOutButton, actualSizeButton, fitToWindowButton, separator2,
                fillColorLabel, fillColorButton, lineColorLabel, lineColorButton, annotationThicknessLabel, annotationThicknessCombo, separator3,
                counterLabel, counterButton, counterResetButton, separator4,
                closeButton, maximizeButton, minimizeButton
            });
        }

        private void CreateAnnotationToolStrip()
        {
            annotationToolStrip = new ToolStrip
            {
                BackColor = Color.FromArgb(248, 248, 248),
                GripStyle = ToolStripGripStyle.Hidden,
                Font = new Font("Segoe UI", 9),
                ImageScalingSize = new Size(32, 32),
                Dock = DockStyle.Top,
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                Height = 50,
                RenderMode = ToolStripRenderMode.ManagerRenderMode,
                Renderer = new RoundedToolStripRenderer(6, Color.FromArgb(248, 248, 248), Color.FromArgb(200, 200, 200))
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
                AutoScroll = false, // Disable on main panel, enable on content
                BackColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(0) // Remove padding to align with file explorer
            };

            // Add annotation toolbar at the top of the content area
            annotationToolStrip.Dock = DockStyle.Top;
            imagePanel.Controls.Add(annotationToolStrip);

            // Create main container for image and thumbnails
            mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true, // Enable scrolling for large images
                Padding = new Padding(10)
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
            // Create the container panel first
            thumbnailPanel = new Panel
            {
                Height = 200, // Increased from 140 to 200
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10) // Add padding around the rounded panel
            };

            // Create the rounded panel inside
            thumbnailPanelRounded = new RoundedPanel
            {
                BackColor = Color.FromArgb(250, 250, 250),
                BorderColor = Color.FromArgb(200, 200, 200),
                BorderWidth = 1,
                CornerRadius = 8, // VS Code-like rounded corners
                Dock = DockStyle.Fill,
                AutoScroll = false,
                Padding = new Padding(5)
            };

            thumbnailPanel.Controls.Add(thumbnailPanelRounded);
        }

        private void CreateNavigationPanel()
        {
            var navPanel = new Panel
            {
                Height = 60, // Increased from 40 to 60
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };

            previousButton = new RoundedButton
            {
                Text = "◀ Previous",
                Width = 120, // Increased from 80 to 120
                Height = 45,  // Increased from 30 to 45
                Location = new Point(15, 8), // Adjusted position
                BackColor = Color.FromArgb(0, 122, 204), // VS Code blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold), // Increased font size
                CornerRadius = 6 // VS Code-like rounded corners
            };
            previousButton.FlatAppearance.BorderSize = 1;
            previousButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            previousButton.Click += (s, e) => NavigateToScreenshot(currentScreenshotIndex - 1);

            nextButton = new RoundedButton
            {
                Text = "Next ▶",
                Width = 120, // Increased from 80 to 120
                Height = 45,  // Increased from 30 to 45
                Location = new Point(145, 8), // Adjusted position
                BackColor = Color.FromArgb(0, 122, 204), // VS Code blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold), // Increased font size
                CornerRadius = 6 // VS Code-like rounded corners
            };
            nextButton.FlatAppearance.BorderSize = 1;
            nextButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            nextButton.Click += (s, e) => NavigateToScreenshot(currentScreenshotIndex + 1);

            screenshotCountLabel = new Label
            {
                Text = "Screenshot 1 of 1",
                Location = new Point(285, 18), // Adjusted position
                Size = new Size(200, 25), // Increased size
                ForeColor = Color.FromArgb(64, 64, 64),
                Font = new Font("Segoe UI", 11, FontStyle.Regular), // Increased font size
                TextAlign = ContentAlignment.MiddleLeft
            };

            navPanel.Controls.AddRange(new Control[] { previousButton, nextButton, screenshotCountLabel });
            thumbnailPanelRounded.Controls.Add(navPanel);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(0, 122, 204), // VS Code blue
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            statusLabel = new ToolStripStatusLabel("Ready")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White
            };

            sizeLabel = new ToolStripStatusLabel("Size: 0 × 0")
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ForeColor = Color.White
            };

            zoomLabel = new ToolStripStatusLabel("Zoom: 100%")
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ForeColor = Color.White
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, sizeLabel, zoomLabel });
        }

        private void CreateFileExplorerPanel()
        {
            fileExplorerPanel = new Panel
            {
                Width = 375, // Increased from 250 by 50%
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(252, 252, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true // Always visible
            };

            // Create header panel with title and buttons
            var headerPanel = new Panel
            {
                Height = 35, // Reduced height since everything is in one row
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Create header label - positioned to the left
            var headerLabel = new Label
            {
                Text = "File Explorer",
                Location = new Point(10, 8), // Centered vertically in the 35px height
                Size = new Size(85, 20),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(64, 64, 64),
                Font = new Font("Segoe UI", 9, FontStyle.Bold), // Slightly smaller font
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Home button - positioned from the right edge
            var homeButton = new Button
            {
                Text = "🏠 Home",
                Location = new Point(195, 5), // Right-aligned positioning
                Size = new Size(50, 25), // Compact size
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(64, 64, 64),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            homeButton.FlatAppearance.BorderSize = 0; // Remove border
            homeButton.Click += HomeButton_Click;
            homeButton.MouseUp += HomeButton_MouseUp;
            
            // Add hover effects for Home button
            homeButton.MouseEnter += (s, e) => homeButton.BackColor = Color.FromArgb(230, 230, 230);
            homeButton.MouseLeave += (s, e) => homeButton.BackColor = Color.Transparent;

            // New Folder button - positioned next to Home button
            var newFolderButton = new Button
            {
                Text = "📁+ New",
                Location = new Point(250, 5), // Right-aligned positioning
                Size = new Size(50, 25), // Compact size
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(64, 64, 64),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            newFolderButton.FlatAppearance.BorderSize = 0; // Remove border
            newFolderButton.Click += NewFolderButton_Click;
            
            // Add hover effects for New Folder button
            newFolderButton.MouseEnter += (s, e) => newFolderButton.BackColor = Color.FromArgb(230, 230, 230);
            newFolderButton.MouseLeave += (s, e) => newFolderButton.BackColor = Color.Transparent;

            // Refresh button - positioned at the far right edge
            var refreshButton = new Button
            {
                Text = "🔄",
                Location = new Point(305, 5), // Far right positioning
                Size = new Size(25, 25), // Compact size for icon
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(64, 64, 64),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            refreshButton.FlatAppearance.BorderSize = 0; // Remove border
            refreshButton.Click += RefreshButton_Click;
            
            // Add hover effects for Refresh button
            refreshButton.MouseEnter += (s, e) => refreshButton.BackColor = Color.FromArgb(230, 230, 230);
            refreshButton.MouseLeave += (s, e) => refreshButton.BackColor = Color.Transparent;

            // Add all controls directly to header panel
            headerPanel.Controls.AddRange(new Control[] { headerLabel, homeButton, newFolderButton, refreshButton });

            // Create TreeView for file explorer
            fileExplorerTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(252, 252, 252),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                FullRowSelect = true,
                HideSelection = false,
                LabelEdit = true, // Enable inline editing
                ImageList = CreateFileExplorerImageList()
            };

            // Add event handlers
            fileExplorerTreeView.BeforeExpand += FileExplorerTreeView_BeforeExpand;
            fileExplorerTreeView.NodeMouseDoubleClick += FileExplorerTreeView_NodeMouseDoubleClick;
            fileExplorerTreeView.NodeMouseClick += FileExplorerTreeView_NodeMouseClick;
            fileExplorerTreeView.BeforeLabelEdit += FileExplorerTreeView_BeforeLabelEdit;
            fileExplorerTreeView.AfterLabelEdit += FileExplorerTreeView_AfterLabelEdit;
            fileExplorerTreeView.MouseUp += FileExplorerTreeView_MouseUp;

            // Populate with drives
            PopulateFileExplorer();

            fileExplorerPanel.Controls.Add(fileExplorerTreeView);
            fileExplorerPanel.Controls.Add(headerPanel);
        }

        private Splitter CreateFileExplorerSplitter()
        {
            var splitter = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                MinExtra = 200, // Minimum space for the main content area
                MinSize = 150   // Minimum width for file explorer
            };

            // Add visual feedback for resizing
            splitter.SplitterMoved += (sender, e) =>
            {
                // Optional: Save the splitter position to settings
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Blueshot"))
                    {
                        key?.SetValue("FileExplorerWidth", fileExplorerPanel.Width);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to save file explorer width", "SplitterMoved", ex);
                }
            };

            return splitter;
        }

        private void ToggleFileExplorer()
        {
            isFileExplorerVisible = !isFileExplorerVisible;
            fileExplorerPanel.Visible = isFileExplorerVisible;
            
            // Update the file explorer button appearance
            var fileExplorerButton = toolStrip.Items.Cast<ToolStripItem>()
                .FirstOrDefault(item => item.ToolTipText == "Toggle File Explorer") as ToolStripButton;
            
            if (fileExplorerButton != null)
            {
                fileExplorerButton.Checked = isFileExplorerVisible;
            }
            
            // Update save location status
            UpdateSaveLocationStatus();
        }

        private ImageList CreateFileExplorerImageList()
        {
            var imageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // Create simple icons for folder and file
            imageList.Images.Add("folder", CreateFolderIcon16());
            imageList.Images.Add("file", CreateFileIcon16());
            imageList.Images.Add("drive", CreateDriveIcon16());

            return imageList;
        }

        private Bitmap CreateFolderIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    g.FillRectangle(brush, 1, 4, 14, 10);
                    g.FillRectangle(brush, 1, 2, 6, 3);
                }
                using (var pen = new Pen(Color.FromArgb(255, 152, 0), 1))
                {
                    g.DrawRectangle(pen, 1, 4, 14, 10);
                    g.DrawLine(pen, 1, 2, 7, 2);
                    g.DrawLine(pen, 7, 2, 7, 4);
                    g.DrawLine(pen, 1, 2, 1, 4);
                }
            }
            return icon;
        }

        private Bitmap CreateFileIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(96, 125, 139)))
                {
                    g.FillRectangle(brush, 3, 1, 10, 14);
                }
                using (var pen = new Pen(Color.FromArgb(69, 90, 100), 1))
                {
                    g.DrawRectangle(pen, 3, 1, 10, 14);
                }
                using (var pen = new Pen(Color.White, 1))
                {
                    g.DrawLine(pen, 5, 4, 11, 4);
                    g.DrawLine(pen, 5, 6, 9, 6);
                    g.DrawLine(pen, 5, 8, 11, 8);
                    g.DrawLine(pen, 5, 10, 8, 10);
                }
            }
            return icon;
        }

        private Bitmap CreateDriveIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(33, 150, 243)))
                {
                    g.FillRectangle(brush, 2, 4, 12, 8);
                }
                using (var pen = new Pen(Color.FromArgb(25, 118, 210), 1))
                {
                    g.DrawRectangle(pen, 2, 4, 12, 8);
                }
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(brush, 4, 6, 4, 4);
                }
            }
            return icon;
        }

        private void PopulateFileExplorer()
        {
            fileExplorerTreeView.Nodes.Clear();
            
            try
            {
                // Only show content from the home folder
                string homePath = GetCurrentHomePath();
                if (Directory.Exists(homePath))
                {
                    var homeNode = new TreeNode(Path.GetFileName(homePath) ?? "Home")
                    {
                        Tag = homePath,
                        ImageKey = "folder",
                        SelectedImageKey = "folder"
                    };
                    
                    // Add subdirectories and files to the home node
                    PopulateDirectoryNode(homeNode, homePath);
                    homeNode.Expand(); // Expand the home folder by default
                    
                    fileExplorerTreeView.Nodes.Add(homeNode);
                    
                    // Select the home node by default
                    fileExplorerTreeView.SelectedNode = homeNode;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to populate file explorer", "PopulateFileExplorer", ex);
            }
        }

        private void PopulateDirectoryNode(TreeNode node, string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                // Add subdirectories
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    if (!dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        var subNode = new TreeNode(dirInfo.Name)
                        {
                            Tag = directory,
                            ImageKey = "folder",
                            SelectedImageKey = "folder"
                        };
                        
                        // Check if this directory has subdirectories
                        try
                        {
                            if (Directory.GetDirectories(directory).Length > 0)
                            {
                                subNode.Nodes.Add("Loading...");
                            }
                        }
                        catch { /* Ignore access denied */ }
                        
                        node.Nodes.Add(subNode);
                    }
                }

                // Add files (limit to common image files for preview app)
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    if (!fileInfo.Attributes.HasFlag(FileAttributes.Hidden) &&
                        imageExtensions.Contains(fileInfo.Extension.ToLower()))
                    {
                        var fileNode = new TreeNode(fileInfo.Name)
                        {
                            Tag = file,
                            ImageKey = "file",
                            SelectedImageKey = "file"
                        };
                        node.Nodes.Add(fileNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to populate directory node: {path}", "PopulateDirectoryNode", ex);
            }
        }

        private void FileExplorerTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                var node = e.Node;
                var path = node.Tag?.ToString();
                
                // Only allow expansion within the home folder
                string homePath = GetCurrentHomePath();
                if (!string.IsNullOrEmpty(path) && !IsPathWithinHome(path, homePath))
                {
                    e.Cancel = true;
                    return;
                }
                
                // Clear dummy nodes
                if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
                {
                    node.Nodes.Clear();
                    PopulateDirectoryNode(node, path);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to expand file explorer node", "FileExplorerTreeView_BeforeExpand", ex);
            }
        }

        private bool IsPathWithinHome(string path, string homePath)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(homePath))
                    return false;
                    
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                string normalizedHome = Path.GetFullPath(homePath).TrimEnd(Path.DirectorySeparatorChar);
                
                return normalizedPath.StartsWith(normalizedHome, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void FileExplorerTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                var path = e.Node.Tag?.ToString();
                if (File.Exists(path))
                {
                    // Open the image file in the preview
                    LoadImageFromFile(path);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to open file from explorer", "FileExplorerTreeView_NodeMouseDoubleClick", ex);
                MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadImageFromFile(string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var image = new Bitmap(fileStream);
                    
                    // Create a new screenshot item
                    var screenshotItem = new ScreenshotItem(image)
                    {
                        FileName = Path.GetFileName(filePath),
                        CaptureTime = File.GetLastWriteTime(filePath)
                    };
                    
                    // Replace current screenshots with this new one
                    screenshots.Clear();
                    screenshots.Add(screenshotItem);
                    currentScreenshotIndex = 0;
                    
                    // Load the screenshot
                    LoadScreenshot(0);
                    
                    // Update UI
                    UpdateNavigationButtons();
                    UpdateScreenshotCounter();
                    this.Text = $"Screenshot Preview - {screenshotItem.FileName} - Blueshot";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load image from file: {filePath}", "LoadImageFromFile", ex);
                throw;
            }
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Navigate to user's preferred home directory
                string homePath = GetCurrentHomePath();
                NavigateToPath(homePath);
                UpdateSaveLocationStatus();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to navigate to home directory", "HomeButton_Click", ex);
                MessageBox.Show("Failed to navigate to home directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NewFolderButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the currently selected node
                var selectedNode = fileExplorerTreeView.SelectedNode;
                if (selectedNode == null)
                {
                    MessageBox.Show("Please select a directory first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string currentPath = selectedNode.Tag?.ToString();
                if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
                {
                    MessageBox.Show("Please select a valid directory.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Create a unique folder name
                string baseName = "New Folder";
                string newFolderName = baseName;
                int counter = 1;
                string newFolderPath = Path.Combine(currentPath, newFolderName);

                while (Directory.Exists(newFolderPath))
                {
                    newFolderName = $"{baseName} ({counter})";
                    newFolderPath = Path.Combine(currentPath, newFolderName);
                    counter++;
                }

                // Create the directory
                Directory.CreateDirectory(newFolderPath);

                // Expand the parent node if it's not already expanded
                if (!selectedNode.IsExpanded)
                {
                    selectedNode.Expand();
                }
                else
                {
                    // Refresh the node to show the new folder
                    RefreshNodeChildren(selectedNode);
                }

                // Find the newly created folder node and start editing
                foreach (TreeNode childNode in selectedNode.Nodes)
                {
                    if (childNode.Text == newFolderName && childNode.Tag?.ToString() == newFolderPath)
                    {
                        fileExplorerTreeView.SelectedNode = childNode;
                        childNode.BeginEdit();
                        break;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access denied. You don't have permission to create a folder in this location.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create new folder", "NewFolderButton_Click", ex);
                MessageBox.Show($"Failed to create folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            try
            {
                PopulateFileExplorer();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to refresh file explorer", "RefreshButton_Click", ex);
                MessageBox.Show("Failed to refresh file explorer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FileExplorerTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Select the node when clicked (for context menu support in future)
            fileExplorerTreeView.SelectedNode = e.Node;
            
            // Update status bar to show selected save location
            UpdateSaveLocationStatus();
        }

        private void UpdateSaveLocationStatus()
        {
            try
            {
                string saveDirectory = GetSelectedSaveDirectory();
                string directoryName = Path.GetFileName(saveDirectory.TrimEnd(Path.DirectorySeparatorChar));
                
                if (string.IsNullOrEmpty(directoryName))
                {
                    directoryName = saveDirectory;
                }
                
                // Update status to show where files will be saved
                if (isFileExplorerVisible && fileExplorerTreeView?.SelectedNode != null)
                {
                    statusLabel.Text = $"Save location: {directoryName}";
                }
                else
                {
                    statusLabel.Text = "Save location: Desktop (default)";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to update save location status", "UpdateSaveLocationStatus", ex);
                statusLabel.Text = "Ready";
            }
        }

        private void CreateFileExplorerContextMenu()
        {
            fileExplorerContextMenu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(252, 252, 252),
                Font = new Font("Segoe UI", 9)
            };

            // Rename menu item
            var renameMenuItem = new ToolStripMenuItem("Rename")
            {
                Image = CreateRenameIcon16(),
                ShortcutKeyDisplayString = "F2"
            };
            renameMenuItem.Click += RenameMenuItem_Click;

            // Delete menu item
            var deleteMenuItem = new ToolStripMenuItem("Delete")
            {
                Image = CreateDeleteIcon16(),
                ShortcutKeyDisplayString = "Del"
            };
            deleteMenuItem.Click += DeleteMenuItem_Click;

            // Separator
            var separator1 = new ToolStripSeparator();

            // Properties menu item
            var propertiesMenuItem = new ToolStripMenuItem("Properties")
            {
                Image = CreatePropertiesIcon16()
            };
            propertiesMenuItem.Click += PropertiesMenuItem_Click;

            fileExplorerContextMenu.Items.AddRange(new ToolStripItem[] {
                renameMenuItem,
                deleteMenuItem,
                separator1,
                propertiesMenuItem
            });
        }

        private void FileExplorerTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Get the node at the mouse position
                TreeNode clickedNode = fileExplorerTreeView.GetNodeAt(e.Location);
                if (clickedNode != null)
                {
                    // Select the node
                    fileExplorerTreeView.SelectedNode = clickedNode;
                    
                    // Check if it's a valid node for context menu (not a loading node)
                    if (clickedNode.Tag != null && clickedNode.Text != "Loading...")
                    {
                        string path = clickedNode.Tag.ToString();
                        bool isFile = File.Exists(path);
                        bool isDirectory = Directory.Exists(path);
                        bool isDrive = path.Length <= 3 && path.EndsWith("\\");
                        
                        // Only show context menu for files and directories (not drives)
                        if ((isFile || isDirectory) && !isDrive)
                        {
                            // Update menu items visibility based on item type
                            UpdateContextMenuForItem(isFile, isDirectory);
                            fileExplorerContextMenu.Show(fileExplorerTreeView, e.Location);
                        }
                    }
                }
            }
        }

        private void UpdateContextMenuForItem(bool isFile, bool isDirectory)
        {
            // All items are available for both files and directories
            foreach (ToolStripItem item in fileExplorerContextMenu.Items)
            {
                item.Enabled = true;
            }
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = fileExplorerTreeView.SelectedNode;
            if (selectedNode != null && selectedNode.Tag != null)
            {
                selectedNode.BeginEdit();
            }
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = fileExplorerTreeView.SelectedNode;
            if (selectedNode == null || selectedNode.Tag == null)
                return;

            string path = selectedNode.Tag.ToString();
            string itemName = selectedNode.Text;
            bool isFile = File.Exists(path);
            bool isDirectory = Directory.Exists(path);

            if (!isFile && !isDirectory)
                return;

            try
            {
                string itemType = isFile ? "file" : "folder";
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the {itemType} '{itemName}'?{(isDirectory ? "\n\nThis will delete the folder and all its contents." : "")}",
                    $"Delete {itemType}",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (isFile)
                    {
                        // Try to remove read-only attribute if present
                        try
                        {
                            FileAttributes attributes = File.GetAttributes(path);
                            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                            }
                        }
                        catch { /* Ignore attribute errors */ }

                        File.Delete(path);
                    }
                    else if (isDirectory)
                    {
                        // For directories, try to remove read-only attributes recursively
                        try
                        {
                            RemoveReadOnlyAttributes(path);
                        }
                        catch { /* Ignore attribute errors */ }

                        Directory.Delete(path, true); // true = recursive delete
                    }

                    // Remove the node from the tree
                    selectedNode.Remove();
                    
                    statusLabel.Text = $"{itemType} deleted successfully";
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access denied. You don't have permission to delete this item.\n\nThis might be due to:\n• The file/folder is currently in use\n• OneDrive sync is in progress\n• Insufficient permissions", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex) when (ex.Message.Contains("not empty") || ex.Message.Contains("not be deleted"))
            {
                MessageBox.Show("The directory is not empty and cannot be deleted.\n\nPlease ensure:\n• All files in the folder are closed\n• No applications are using files in this folder\n• OneDrive sync is complete", "Directory Not Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex) when (ex.Message.Contains("denied") || ex.Message.Contains("access"))
            {
                MessageBox.Show("Access to the path is denied.\n\nThis might be due to:\n• OneDrive is syncing this folder\n• The folder is open in another application\n• Insufficient permissions\n\nTry closing all programs and wait for OneDrive sync to complete.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Failed to delete item: {ex.Message}\n\nTip: If this is a OneDrive folder, try waiting for sync to complete or temporarily pausing OneDrive sync.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to delete item: {path}", "DeleteMenuItem_Click", ex);
                MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveReadOnlyAttributes(string directoryPath)
        {
            try
            {
                // Remove read-only attribute from the directory itself
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
                if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                }

                // Remove read-only attributes from all files in the directory
                foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileAttributes attributes = File.GetAttributes(file);
                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch { /* Ignore individual file errors */ }
                }

                // Remove read-only attributes from all subdirectories
                foreach (string dir in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        DirectoryInfo subDirInfo = new DirectoryInfo(dir);
                        if ((subDirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            subDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                        }
                    }
                    catch { /* Ignore individual directory errors */ }
                }
            }
            catch { /* Ignore errors in this helper method */ }
        }

        private void PropertiesMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = fileExplorerTreeView.SelectedNode;
            if (selectedNode == null || selectedNode.Tag == null)
                return;

            string path = selectedNode.Tag.ToString();
            
            try
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    ShowFileProperties(path);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show properties for: {path}", "PropertiesMenuItem_Click", ex);
                MessageBox.Show($"Failed to show properties: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFileProperties(string path)
        {
            try
            {
                FileInfo fileInfo = null;
                DirectoryInfo dirInfo = null;
                bool isFile = File.Exists(path);
                
                if (isFile)
                {
                    fileInfo = new FileInfo(path);
                }
                else
                {
                    dirInfo = new DirectoryInfo(path);
                }

                string properties = isFile ? 
                    $"File: {fileInfo.Name}\n" +
                    $"Location: {fileInfo.DirectoryName}\n" +
                    $"Size: {FormatFileSize(fileInfo.Length)}\n" +
                    $"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Attributes: {fileInfo.Attributes}"
                    :
                    $"Folder: {dirInfo.Name}\n" +
                    $"Location: {dirInfo.Parent?.FullName ?? "Root"}\n" +
                    $"Created: {dirInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Modified: {dirInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Attributes: {dirInfo.Attributes}";

                MessageBox.Show(properties, "Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to get properties for: {path}", "ShowFileProperties", ex);
                MessageBox.Show($"Failed to get properties: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        private void CreateHomeButtonContextMenu()
        {
            homeButtonContextMenu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(252, 252, 252),
                Font = new Font("Segoe UI", 9)
            };

            // Default home locations
            var documentsMenuItem = new ToolStripMenuItem("📁 Documents")
            {
                Tag = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            documentsMenuItem.Click += HomeLocationMenuItem_Click;

            var desktopMenuItem = new ToolStripMenuItem("🖥️ Desktop")
            {
                Tag = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            desktopMenuItem.Click += HomeLocationMenuItem_Click;

            var picturesMenuItem = new ToolStripMenuItem("🖼️ Pictures")
            {
                Tag = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            picturesMenuItem.Click += HomeLocationMenuItem_Click;

            var downloadsMenuItem = new ToolStripMenuItem("📥 Downloads")
            {
                Tag = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };
            downloadsMenuItem.Click += HomeLocationMenuItem_Click;

            var separator1 = new ToolStripSeparator();

            // Custom location
            var customMenuItem = new ToolStripMenuItem("📂 Choose Custom Location...")
            {
                Tag = "custom"
            };
            customMenuItem.Click += HomeLocationMenuItem_Click;

            var separator2 = new ToolStripSeparator();

            // Current custom location (if any)
            var currentCustomMenuItem = new ToolStripMenuItem("Current: Default")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            homeButtonContextMenu.Items.AddRange(new ToolStripItem[] {
                documentsMenuItem,
                desktopMenuItem,
                picturesMenuItem,
                downloadsMenuItem,
                separator1,
                customMenuItem,
                separator2,
                currentCustomMenuItem
            });

            // Update the current location display
            UpdateHomeButtonContextMenuStatus();
        }

        private void HomeButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Update the context menu status before showing
                UpdateHomeButtonContextMenuStatus();
                homeButtonContextMenu.Show((Button)sender, e.Location);
            }
        }

        private void HomeLocationMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem?.Tag == null) return;

            try
            {
                string tag = menuItem.Tag.ToString();
                
                if (tag == "custom")
                {
                    // Show folder browser dialog
                    using (var folderDialog = new FolderBrowserDialog())
                    {
                        folderDialog.Description = "Select your preferred home folder:";
                        folderDialog.ShowNewFolderButton = true;
                        
                        // Set initial directory
                        if (!string.IsNullOrEmpty(customHomePath) && Directory.Exists(customHomePath))
                        {
                            folderDialog.SelectedPath = customHomePath;
                        }
                        else
                        {
                            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        }

                        if (folderDialog.ShowDialog() == DialogResult.OK)
                        {
                            customHomePath = folderDialog.SelectedPath;
                            SaveSettings(); // Save the custom path
                            NavigateToPath(customHomePath);
                            UpdateSaveLocationStatus();
                            UpdateHomeButtonContextMenuStatus();
                        }
                    }
                }
                else
                {
                    // Navigate to predefined location
                    string path = tag;
                    if (Directory.Exists(path))
                    {
                        customHomePath = path; // Update custom home path
                        SaveSettings(); // Save the custom path
                        NavigateToPath(path);
                        UpdateSaveLocationStatus();
                        UpdateHomeButtonContextMenuStatus();
                    }
                    else
                    {
                        MessageBox.Show($"The folder '{path}' does not exist.", "Folder Not Found", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to set home location", "HomeLocationMenuItem_Click", ex);
                MessageBox.Show($"Failed to set home location: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateHomeButtonContextMenuStatus()
        {
            try
            {
                // Find the current location status menu item (last item)
                var statusItem = homeButtonContextMenu.Items[homeButtonContextMenu.Items.Count - 1] as ToolStripMenuItem;
                if (statusItem != null)
                {
                    string currentPath = GetCurrentHomePath();
                    string displayName = GetHomePathDisplayName(currentPath);
                    statusItem.Text = $"Current: {displayName}";
                }

                // Update check marks for current selection
                foreach (ToolStripItem item in homeButtonContextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.Tag != null && menuItem.Tag.ToString() != "custom")
                    {
                        string currentPath = GetCurrentHomePath();
                        menuItem.Checked = string.Equals(menuItem.Tag.ToString(), currentPath, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to update home button context menu status", "UpdateHomeButtonContextMenuStatus", ex);
            }
        }

        private string GetCurrentHomePath()
        {
            if (!string.IsNullOrEmpty(customHomePath) && Directory.Exists(customHomePath))
            {
                return customHomePath;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        private string GetHomePathDisplayName(string path)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                if (string.Equals(path, documentsPath, StringComparison.OrdinalIgnoreCase))
                    return "Documents";
                else if (string.Equals(path, desktopPath, StringComparison.OrdinalIgnoreCase))
                    return "Desktop";
                else if (string.Equals(path, picturesPath, StringComparison.OrdinalIgnoreCase))
                    return "Pictures";
                else if (string.Equals(path, downloadsPath, StringComparison.OrdinalIgnoreCase))
                    return "Downloads";
                else
                    return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)) ?? path;
            }
            catch
            {
                return "Unknown";
            }
        }

        private void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Blueshot"))
                {
                    string savedHomePath = key?.GetValue("CustomHomePath") as string;
                    if (!string.IsNullOrEmpty(savedHomePath) && Directory.Exists(savedHomePath))
                    {
                        customHomePath = savedHomePath;
                    }
                    
                    // Load file explorer width
                    object savedWidth = key?.GetValue("FileExplorerWidth");
                    if (savedWidth != null && int.TryParse(savedWidth.ToString(), out int width))
                    {
                        if (width >= 150 && width <= 800) // Reasonable bounds
                        {
                            fileExplorerPanel.Width = width;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load settings", "LoadSettings", ex);
                // Continue with default settings if loading fails
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Blueshot"))
                {
                    if (!string.IsNullOrEmpty(customHomePath))
                    {
                        key?.SetValue("CustomHomePath", customHomePath);
                    }
                    else
                    {
                        key?.DeleteValue("CustomHomePath", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save settings", "SaveSettings", ex);
            }
        }

        private void InitializeFileExplorerToHome()
        {
            try
            {
                string homePath = GetCurrentHomePath();
                if (Directory.Exists(homePath))
                {
                    NavigateToPath(homePath);
                    UpdateSaveLocationStatus();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize file explorer to home", "InitializeFileExplorerToHome", ex);
            }
        }

        private void GoToHomeFolder()
        {
            try
            {
                string homePath = GetCurrentHomePath();
                NavigateToPath(homePath);
                UpdateSaveLocationStatus();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to navigate to home folder", "GoToHomeFolder", ex);
                MessageBox.Show("Failed to navigate to home folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap CreateRenameIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Pencil body
                using (var brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    g.FillRectangle(brush, 2, 10, 8, 2);
                }
                
                // Pencil tip
                using (var brush = new SolidBrush(Color.FromArgb(96, 125, 139)))
                {
                    var points = new Point[] { new Point(10, 10), new Point(13, 7), new Point(10, 12) };
                    g.FillPolygon(brush, points);
                }
                
                // Paper
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(brush, 4, 2, 8, 10);
                }
                
                using (var pen = new Pen(Color.FromArgb(96, 96, 96), 1))
                {
                    g.DrawRectangle(pen, 4, 2, 8, 10);
                    g.DrawLine(pen, 6, 4, 10, 4);
                    g.DrawLine(pen, 6, 6, 10, 6);
                    g.DrawLine(pen, 6, 8, 8, 8);
                }
            }
            return icon;
        }

        private Bitmap CreateDeleteIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Trash can body
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillRectangle(brush, 3, 5, 10, 9);
                }
                
                // Trash can lid
                using (var brush = new SolidBrush(Color.FromArgb(198, 40, 40)))
                {
                    g.FillRectangle(brush, 2, 3, 12, 2);
                }
                
                // Handle
                using (var pen = new Pen(Color.FromArgb(198, 40, 40), 2))
                {
                    g.DrawLine(pen, 6, 1, 6, 3);
                    g.DrawLine(pen, 10, 1, 10, 3);
                    g.DrawLine(pen, 6, 1, 10, 1);
                }
                
                // Vertical lines
                using (var pen = new Pen(Color.White, 1))
                {
                    g.DrawLine(pen, 6, 7, 6, 12);
                    g.DrawLine(pen, 8, 7, 8, 12);
                    g.DrawLine(pen, 10, 7, 10, 12);
                }
            }
            return icon;
        }

        private Bitmap CreatePropertiesIcon16()
        {
            var icon = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Document background
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(brush, 3, 1, 10, 14);
                }
                
                // Document border
                using (var pen = new Pen(Color.FromArgb(96, 125, 139), 1))
                {
                    g.DrawRectangle(pen, 3, 1, 10, 14);
                }
                
                // Info icon (i)
                using (var brush = new SolidBrush(Color.FromArgb(33, 150, 243)))
                {
                    g.FillEllipse(brush, 7, 3, 2, 2);
                    g.FillRectangle(brush, 7, 6, 2, 6);
                }
            }
            return icon;
        }

        private string GetCurrentSelectedPath()
        {
            var selectedNode = fileExplorerTreeView.SelectedNode;
            if (selectedNode != null)
            {
                string path = selectedNode.Tag?.ToString();
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }
            }
            
            // Default to user's Documents folder if nothing selected
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void NavigateToPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    MessageBox.Show("The specified path does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Find and expand the node for this path
                TreeNode nodeToSelect = FindNodeByPath(path);
                if (nodeToSelect != null)
                {
                    fileExplorerTreeView.SelectedNode = nodeToSelect;
                    nodeToSelect.EnsureVisible();
                    nodeToSelect.Expand();
                }
                else
                {
                    // If not found, refresh and try to navigate to the parent directory
                    PopulateFileExplorer();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to navigate to path: {path}", "NavigateToPath", ex);
                MessageBox.Show($"Failed to navigate to path: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TreeNode FindNodeByPath(string path)
        {
            foreach (TreeNode rootNode in fileExplorerTreeView.Nodes)
            {
                TreeNode result = FindNodeByPathRecursive(rootNode, path);
                if (result != null)
                    return result;
            }
            return null;
        }

        private TreeNode FindNodeByPathRecursive(TreeNode node, string path)
        {
            string nodePath = node.Tag?.ToString();
            if (string.Equals(nodePath, path, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            // If the path starts with this node's path, search in children
            if (!string.IsNullOrEmpty(nodePath) && path.StartsWith(nodePath, StringComparison.OrdinalIgnoreCase))
            {
                // Expand node if needed to load children
                if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
                {
                    var cancelArgs = new TreeViewCancelEventArgs(node, false, TreeViewAction.Expand);
                    FileExplorerTreeView_BeforeExpand(fileExplorerTreeView, cancelArgs);
                }

                foreach (TreeNode childNode in node.Nodes)
                {
                    TreeNode result = FindNodeByPathRecursive(childNode, path);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private void RefreshCurrentNode()
        {
            try
            {
                var selectedNode = fileExplorerTreeView.SelectedNode;
                if (selectedNode != null)
                {
                    RefreshNodeChildren(selectedNode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to refresh current node", "RefreshCurrentNode", ex);
            }
        }

        private void RefreshNodeChildren(TreeNode node)
        {
            try
            {
                // Store the expanded state
                bool wasExpanded = node.IsExpanded;
                
                // Clear and reload the node
                node.Nodes.Clear();
                node.Nodes.Add("Loading...");
                
                // Trigger expansion to reload
                var cancelArgs = new TreeViewCancelEventArgs(node, false, TreeViewAction.Expand);
                FileExplorerTreeView_BeforeExpand(fileExplorerTreeView, cancelArgs);
                
                if (wasExpanded)
                {
                    node.Expand();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to refresh node children", "RefreshNodeChildren", ex);
            }
        }

        private void FileExplorerTreeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                // Only allow editing of folder nodes (not drives)
                string path = e.Node.Tag?.ToString();
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    e.CancelEdit = true;
                    return;
                }

                // Don't allow editing of drive nodes
                if (path.Length <= 3 && path.EndsWith("\\"))
                {
                    e.CancelEdit = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in BeforeLabelEdit", "FileExplorerTreeView_BeforeLabelEdit", ex);
                e.CancelEdit = true;
            }
        }

        private void FileExplorerTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                // If the edit was cancelled or the label is null/empty, don't proceed
                if (e.CancelEdit || string.IsNullOrWhiteSpace(e.Label))
                {
                    return;
                }

                string oldPath = e.Node.Tag?.ToString();
                if (string.IsNullOrEmpty(oldPath) || !Directory.Exists(oldPath))
                {
                    e.CancelEdit = true;
                    return;
                }

                string parentPath = Directory.GetParent(oldPath)?.FullName;
                if (string.IsNullOrEmpty(parentPath))
                {
                    e.CancelEdit = true;
                    return;
                }

                string newName = e.Label.Trim();
                string newPath = Path.Combine(parentPath, newName);

                // Check if the new name is valid
                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    MessageBox.Show("The folder name contains invalid characters.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.CancelEdit = true;
                    return;
                }

                // Check if a folder with the new name already exists
                if (Directory.Exists(newPath) && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("A folder with this name already exists.", "Name Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.CancelEdit = true;
                    return;
                }

                // Rename the directory
                try
                {
                    Directory.Move(oldPath, newPath);
                    
                    // Update the node's tag with the new path
                    e.Node.Tag = newPath;
                    
                    // Update any child nodes that might have this as their parent path
                    UpdateChildNodePaths(e.Node, oldPath, newPath);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Access denied. You don't have permission to rename this folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.CancelEdit = true;
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Failed to rename folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.CancelEdit = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in AfterLabelEdit", "FileExplorerTreeView_AfterLabelEdit", ex);
                e.CancelEdit = true;
            }
        }

        private void UpdateChildNodePaths(TreeNode parentNode, string oldParentPath, string newParentPath)
        {
            try
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    string childPath = childNode.Tag?.ToString();
                    if (!string.IsNullOrEmpty(childPath) && childPath.StartsWith(oldParentPath))
                    {
                        string newChildPath = newParentPath + childPath.Substring(oldParentPath.Length);
                        childNode.Tag = newChildPath;
                        
                        // Recursively update grandchildren
                        UpdateChildNodePaths(childNode, childPath, newChildPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to update child node paths", "UpdateChildNodePaths", ex);
            }
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
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Modern save icon background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(32, 32),
                    Color.FromArgb(0, 120, 215), Color.FromArgb(0, 96, 172)))
                {
                    g.FillRectangle(brush, 4, 4, 24, 24);
                }
                
                // Top corner cutout
                using (var brush = new SolidBrush(Color.Transparent))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    var points = new Point[] { new Point(20, 4), new Point(28, 4), new Point(28, 12) };
                    g.FillPolygon(brush, points);
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
                
                // Disk slot
                using (var brush = new SolidBrush(Color.FromArgb(0, 80, 150)))
                {
                    g.FillRectangle(brush, 11, 22, 10, 4);
                }
                
                // Label area with gradient
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(brush, 8, 9, 16, 11);
                }
                
                // Highlight for depth
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 5, 5, 5, 27);
                    g.DrawLine(pen, 5, 5, 19, 5);
                }
                
                // Border
                using (var pen = new Pen(Color.FromArgb(0, 64, 128), 1))
                {
                    g.DrawRectangle(pen, 4, 4, 24, 24);
                }
            }
            return icon;
        }

        private Bitmap CreateCopyIcon()
        {
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Back document (shadow)
                using (var brush = new SolidBrush(Color.FromArgb(120, 120, 120)))
                {
                    g.FillRectangle(brush, 4, 4, 19, 21);
                }
                
                // Front document
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(32, 32),
                    Color.FromArgb(33, 150, 243), Color.FromArgb(21, 101, 192)))
                {
                    g.FillRectangle(brush, 8, 7, 19, 21);
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

        private Bitmap CreateMinimizeIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Circle background
                using (var brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    g.FillEllipse(brush, 2, 2, 20, 20);
                }
                
                // Minimize line
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 7, 12, 17, 12);
                }
            }
            return icon;
        }

        private Bitmap CreateMaximizeIcon()
        {
            var icon = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Circle background
                using (var brush = new SolidBrush(Color.FromArgb(76, 175, 80)))
                {
                    g.FillEllipse(brush, 2, 2, 20, 20);
                }
                
                // Check if window is maximized (either via WindowState or custom bounds)
                bool isMaximized = this.WindowState == FormWindowState.Maximized || 
                                 this.Bounds.Equals(Screen.FromControl(this).WorkingArea);
                
                if (isMaximized)
                {
                    // Restore icon - two overlapping squares
                    using (var pen = new Pen(Color.White, 1.5f))
                    {
                        // Back square
                        g.DrawRectangle(pen, 9, 7, 6, 6);
                        // Front square
                        g.DrawRectangle(pen, 7, 9, 6, 6);
                    }
                }
                else
                {
                    // Maximize icon - single square
                    using (var pen = new Pen(Color.White, 2))
                    {
                        g.DrawRectangle(pen, 8, 8, 8, 8);
                    }
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
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Main rectangle
                using (var pen = new Pen(Color.FromArgb(156, 39, 176), 3f))
                {
                    g.DrawRectangle(pen, 6, 6, 20, 16);
                }
                
                // Inner shadow effect
                using (var pen = new Pen(Color.FromArgb(50, 0, 0, 0), 1))
                {
                    g.DrawRectangle(pen, 7, 7, 18, 14);
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
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Arrow shaft - diagonal from bottom-left to top-right
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 5))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLine(pen, 6, 25, 24, 7);
                }
                
                // Large, clearly visible arrow head pointing up-right at 45 degrees
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    var points = new Point[] {
                        new Point(24, 7),   // tip of arrow (top-right)
                        new Point(16, 7),   // horizontal wing (left from tip)
                        new Point(20, 11),  // connection point (slightly down and left)
                        new Point(24, 15),  // vertical wing (down from tip)
                        new Point(28, 11),  // outer tip extension
                        new Point(24, 7)    // back to tip
                    };
                    g.FillPolygon(brush, points);
                }
                
                // Arrow head outline for extra definition
                using (var pen = new Pen(Color.FromArgb(244, 67, 54), 3))
                {
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    var points = new Point[] {
                        new Point(24, 7),   // tip
                        new Point(16, 7),   // horizontal wing
                        new Point(20, 11),  // connection
                        new Point(24, 15),  // vertical wing
                        new Point(28, 11),  // outer extension
                        new Point(24, 7)    // back to tip
                    };
                    g.DrawLines(pen, points);
                }
                
                // Add contrast shadow behind arrow head
                using (var brush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    var shadowPoints = new Point[] {
                        new Point(25, 8),   // tip shadow
                        new Point(17, 8),   // horizontal wing shadow
                        new Point(21, 12),  // connection shadow
                        new Point(25, 16),  // vertical wing shadow
                        new Point(29, 12),  // outer extension shadow
                        new Point(25, 8)    // back to tip shadow
                    };
                    g.FillPolygon(brush, shadowPoints);
                }
                
                // Starting point circle
                using (var brush = new SolidBrush(Color.FromArgb(244, 67, 54)))
                {
                    g.FillEllipse(brush, 3, 22, 7, 7);
                }
                
                // White highlight on starting circle
                using (var brush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 4, 23, 3, 3);
                }
            }
            return icon;
        }

        private Bitmap CreateTextIcon()
        {
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                // Letter "A" centered
                using (var brush = new SolidBrush(Color.FromArgb(63, 81, 181)))
                using (var font = new Font("Segoe UI", 18, FontStyle.Bold))
                {
                    var textSize = g.MeasureString("A", font);
                    var x = (32 - textSize.Width) / 2;
                    var y = (32 - textSize.Height) / 2 - 3; // Slightly higher to leave room for underline
                    g.DrawString("A", font, brush, x, y);
                }
                
                // Underline
                using (var pen = new Pen(Color.FromArgb(63, 81, 181), 3))
                {
                    g.DrawLine(pen, 8, 25, 24, 25);
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

        private Bitmap CreateFileExplorerIcon()
        {
            var icon = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(icon))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Folder background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0), new Point(32, 32),
                    Color.FromArgb(255, 193, 7), Color.FromArgb(255, 152, 0)))
                {
                    // Main folder body
                    g.FillRectangle(brush, 3, 10, 26, 18);
                    
                    // Folder tab
                    g.FillRectangle(brush, 3, 6, 12, 6);
                }
                
                // Folder shadow/depth
                using (var brush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.FillRectangle(brush, 4, 27, 25, 2);
                }
                
                // Files inside folder
                using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    g.FillRectangle(brush, 8, 14, 16, 2);
                    g.FillRectangle(brush, 8, 17, 12, 2);
                    g.FillRectangle(brush, 8, 20, 14, 2);
                    g.FillRectangle(brush, 8, 23, 10, 2);
                }
                
                // Folder outline
                using (var pen = new Pen(Color.FromArgb(255, 111, 0), 1.5f))
                {
                    // Main folder outline
                    g.DrawRectangle(pen, 3, 10, 26, 18);
                    // Tab outline
                    g.DrawLine(pen, 3, 6, 15, 6);
                    g.DrawLine(pen, 15, 6, 15, 10);
                    g.DrawLine(pen, 3, 6, 3, 10);
                }
                
                // Highlight for 3D effect
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 4, 11, 4, 27);
                    g.DrawLine(pen, 4, 11, 28, 11);
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
            if (thumbnailPanelRounded == null) return;
            
            // Clear existing thumbnails except navigation panel
            var controlsToRemove = thumbnailPanelRounded.Controls.Cast<Control>()
                .Where(c => c is RoundedPictureBox || (c is Panel && c.Controls.Count == 0))
                .ToList();
            
            foreach (var control in controlsToRemove)
            {
                thumbnailPanelRounded.Controls.Remove(control);
                control.Dispose();
            }

            // Add thumbnail images
            int x = 10;
            int y = 45; // Below navigation panel
            
            for (int i = 0; i < screenshots.Count; i++)
            {
                var thumbBox = CreateThumbnailBox(screenshots[i], i);
                thumbBox.Location = new Point(x, y);
                thumbnailPanelRounded.Controls.Add(thumbBox);
                x += thumbBox.Width + 10;
            }
        }

        private RoundedPictureBox CreateThumbnailBox(ScreenshotItem screenshotItem, int index)
        {
            var thumbBox = new RoundedPictureBox
            {
                Image = screenshotItem.Thumbnail,
                Size = new Size(screenshotItem.Thumbnail.Width + 4, screenshotItem.Thumbnail.Height + 4),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BorderColor = index == currentScreenshotIndex ? Color.FromArgb(0, 122, 204) : Color.FromArgb(200, 200, 200),
                BorderWidth = index == currentScreenshotIndex ? 2 : 1,
                BackColor = index == currentScreenshotIndex ? Color.FromArgb(0, 122, 204) : Color.White,
                CornerRadius = 4, // Rounded corners for thumbnails
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
                // Previous button styling
                if (currentScreenshotIndex > 0)
                {
                    // Active state - VS Code blue
                    previousButton.Enabled = true;
                    previousButton.BackColor = Color.FromArgb(0, 122, 204);
                    previousButton.ForeColor = Color.White;
                    previousButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
                    previousButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);
                }
                else
                {
                    // Disabled state - VS Code blue with reduced opacity (muted blue)
                    previousButton.Enabled = false;
                    previousButton.BackColor = Color.FromArgb(0, 80, 134); // Darker blue for disabled state
                    previousButton.ForeColor = Color.FromArgb(200, 200, 200); // Slightly muted white
                    previousButton.FlatAppearance.BorderColor = Color.FromArgb(0, 80, 134);
                    previousButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 80, 134);
                }
                
                // Next button styling
                if (currentScreenshotIndex < screenshots.Count - 1)
                {
                    // Active state - VS Code blue
                    nextButton.Enabled = true;
                    nextButton.BackColor = Color.FromArgb(0, 122, 204);
                    nextButton.ForeColor = Color.White;
                    nextButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
                    nextButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);
                }
                else
                {
                    // Disabled state - VS Code blue with reduced opacity (muted blue)
                    nextButton.Enabled = false;
                    nextButton.BackColor = Color.FromArgb(0, 80, 134); // Darker blue for disabled state
                    nextButton.ForeColor = Color.FromArgb(200, 200, 200); // Slightly muted white
                    nextButton.FlatAppearance.BorderColor = Color.FromArgb(0, 80, 134);
                    nextButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 80, 134);
                }
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
            if (thumbnailPanelRounded != null)
            {
                foreach (Control control in thumbnailPanelRounded.Controls)
                {
                    if (control is RoundedPictureBox thumbBox && thumbBox.Tag is int index)
                    {
                        thumbBox.BorderColor = index == currentScreenshotIndex ? Color.FromArgb(0, 122, 204) : Color.FromArgb(200, 200, 200);
                        thumbBox.BorderWidth = index == currentScreenshotIndex ? 2 : 1;
                        thumbBox.BackColor = index == currentScreenshotIndex ? Color.FromArgb(0, 122, 204) : Color.White;
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
                ActualSize(); // Set 1:1 zoom instead of FitToWindow()
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
            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.Paint += PictureBox_Paint;
            
            // Enable mouse wheel events for the picture box
            mainContainer.MouseWheel += MainContainer_MouseWheel;
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

        private void ShowFillColorPicker(object sender, EventArgs e)
        {
            var colorDialog = new ColorDialog
            {
                Color = Color.FromArgb(255, annotationFillColor.R, annotationFillColor.G, annotationFillColor.B), // Remove alpha for dialog
                AllowFullOpen = true,
                FullOpen = true
            };
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                // Preserve some transparency for fill colors (except for certain tools)
                if (currentTool == AnnotationTool.Highlight)
                {
                    annotationFillColor = Color.FromArgb(100, colorDialog.Color); // Semi-transparent for highlights
                }
                else
                {
                    annotationFillColor = Color.FromArgb(200, colorDialog.Color); // Slightly transparent for other fills
                }
                
                // Update fill color button appearance
                var fillColorButton = toolStrip.Items["FillColorButton"] as ToolStripButton;
                if (fillColorButton != null)
                {
                    fillColorButton.ForeColor = Color.FromArgb(255, annotationFillColor.R, annotationFillColor.G, annotationFillColor.B);
                }
            }
        }

        private void ShowLineColorPicker(object sender, EventArgs e)
        {
            var colorDialog = new ColorDialog
            {
                Color = annotationLineColor,
                AllowFullOpen = true,
                FullOpen = true
            };
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                annotationLineColor = colorDialog.Color; // Line colors are always solid
                
                // Update line color button appearance
                var lineColorButton = toolStrip.Items["LineColorButton"] as ToolStripButton;
                if (lineColorButton != null)
                {
                    lineColorButton.ForeColor = annotationLineColor;
                }
            }
        }

        // Legacy method for backward compatibility
        private void ShowColorPicker(object sender, EventArgs e)
        {
            ShowLineColorPicker(sender, e); // Default to line color picker
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
            var lineColor = annotation.IsSelected ? Color.FromArgb(Math.Min(255, annotation.LineColor.R + 50), 
                Math.Min(255, annotation.LineColor.G + 50), Math.Min(255, annotation.LineColor.B + 50)) : annotation.LineColor;
            var fillColor = annotation.IsSelected ? Color.FromArgb(Math.Min(255, annotation.FillColor.R + 50), 
                Math.Min(255, annotation.FillColor.G + 50), Math.Min(255, annotation.FillColor.B + 50)) : annotation.FillColor;

            switch (annotation.Tool)
            {
                case AnnotationTool.Highlight:
                    using (var brush = new SolidBrush(fillColor))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    break;

                case AnnotationTool.Rectangle:
                    // Fill the rectangle if it has a fill color with alpha > 0
                    if (fillColor.A > 0)
                    {
                        using (var brush = new SolidBrush(fillColor))
                        {
                            g.FillRectangle(brush, rect);
                        }
                    }
                    // Draw the border
                    using (var pen = new Pen(lineColor, thickness))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                    break;

                case AnnotationTool.Line:
                    using (var pen = new Pen(lineColor, thickness))
                    {
                        g.DrawLine(pen, annotation.StartPoint, annotation.EndPoint);
                    }
                    break;

                case AnnotationTool.Arrow:
                    DrawArrow(g, annotation.StartPoint, annotation.EndPoint, lineColor, thickness);
                    break;

                case AnnotationTool.Text:
                    using (var brush = new SolidBrush(lineColor)) // Use line color for text
                    {
                        string textToDisplay = annotation.Text ?? "Text";
                        
                        // If this is the annotation being edited, show current typing text and cursor
                        if (isTypingText && annotation == editingTextAnnotation)
                        {
                            textToDisplay = currentTypingText;
                        }
                        
                        if (annotation.IsTextRegion)
                        {
                            // For region-based text, draw text within the bounds
                            var textRect = new Rectangle(
                                Math.Min(annotation.StartPoint.X, annotation.EndPoint.X),
                                Math.Min(annotation.StartPoint.Y, annotation.EndPoint.Y),
                                Math.Abs(annotation.EndPoint.X - annotation.StartPoint.X),
                                Math.Abs(annotation.EndPoint.Y - annotation.StartPoint.Y)
                            );
                            
                            // Draw background rectangle with fill color
                            if (fillColor.A > 0)
                            {
                                using (var bgBrush = new SolidBrush(fillColor))
                                {
                                    g.FillRectangle(bgBrush, textRect);
                                }
                            }
                            
                            // Draw border
                            using (var pen = new Pen(lineColor, 1))
                            {
                                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawRectangle(pen, textRect);
                            }
                            
                            // Draw text within the rectangle with word wrapping
                            if (!string.IsNullOrEmpty(textToDisplay))
                            {
                                var stringFormat = new StringFormat
                                {
                                    Alignment = StringAlignment.Near,
                                    LineAlignment = StringAlignment.Near,
                                    FormatFlags = StringFormatFlags.LineLimit
                                };
                                
                                // Add some padding
                                textRect.Inflate(-5, -5);
                                
                                g.DrawString(textToDisplay, annotation.Font, brush, textRect, stringFormat);
                            }
                            
                            // Draw cursor if editing
                            if (isTypingText && annotation == editingTextAnnotation)
                            {
                                DrawTextCursor(g, annotation, textToDisplay, lineColor);
                            }
                        }
                        else
                        {
                            // For point-based text, draw at the start point
                            g.DrawString(textToDisplay, annotation.Font, brush, annotation.StartPoint);
                            
                            // Draw cursor if editing
                            if (isTypingText && annotation == editingTextAnnotation)
                            {
                                DrawTextCursor(g, annotation, textToDisplay, lineColor);
                            }
                        }
                    }
                    break;

                case AnnotationTool.Counter:
                    DrawCounter(g, annotation.StartPoint, annotation.CounterNumber, lineColor, thickness);
                    break;
            }
        }

        private void DrawTextCursor(Graphics g, AnnotationObject annotation, string currentText, Color color)
        {
            if (annotation == null || !isTypingText) return;
            
            try
            {
                // Get text up to cursor position
                string textToCursor = textCursorPosition <= currentText.Length ? 
                    currentText.Substring(0, textCursorPosition) : currentText;
                
                PointF cursorPosition;
                
                if (annotation.IsTextRegion)
                {
                    // For region-based text
                    var textRect = new Rectangle(
                        Math.Min(annotation.StartPoint.X, annotation.EndPoint.X),
                        Math.Min(annotation.StartPoint.Y, annotation.EndPoint.Y),
                        Math.Abs(annotation.EndPoint.X - annotation.StartPoint.X),
                        Math.Abs(annotation.EndPoint.Y - annotation.StartPoint.Y)
                    );
                    
                    textRect.Inflate(-5, -5);
                    
                    if (!string.IsNullOrEmpty(textToCursor))
                    {
                        var stringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        
                        var textSize = g.MeasureString(textToCursor, annotation.Font, textRect.Width, stringFormat);
                        cursorPosition = new PointF(textRect.X + textSize.Width, textRect.Y);
                    }
                    else
                    {
                        cursorPosition = new PointF(textRect.X, textRect.Y);
                    }
                }
                else
                {
                    // For point-based text
                    if (!string.IsNullOrEmpty(textToCursor))
                    {
                        var textSize = g.MeasureString(textToCursor, annotation.Font);
                        cursorPosition = new PointF(
                            annotation.StartPoint.X + textSize.Width,
                            annotation.StartPoint.Y
                        );
                    }
                    else
                    {
                        cursorPosition = new PointF(annotation.StartPoint.X, annotation.StartPoint.Y);
                    }
                }
                
                // Draw blinking cursor (vertical line)
                using (var pen = new Pen(color, 1))
                {
                    var cursorHeight = annotation.Font.Height;
                    g.DrawLine(pen, 
                        cursorPosition.X, cursorPosition.Y,
                        cursorPosition.X, cursorPosition.Y + cursorHeight);
                }
            }
            catch (Exception)
            {
                // Fail silently for cursor drawing errors
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
                        
                        // Start direct text input for this text annotation
                        StartDirectTextInput(annotation);
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
                // Convert mouse coordinates to image coordinates
                var imagePoint = new Point(
                    (int)(e.X / zoomFactor),
                    (int)(e.Y / zoomFactor)
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
            // Convert mouse coordinates to image coordinates
            var imagePoint = new Point(
                (int)(e.X / zoomFactor),
                (int)(e.Y / zoomFactor)
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
                // Convert mouse coordinates to image coordinates
                var imagePoint = new Point(
                    (int)(e.X / zoomFactor),
                    (int)(e.Y / zoomFactor)
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

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            // Handle mouse wheel for zooming when Ctrl is held
            if (Control.ModifierKeys == Keys.Control)
            {
                const float zoomStep = 0.1f;
                float newZoomFactor = zoomFactor;

                if (e.Delta > 0)
                {
                    newZoomFactor = Math.Min(zoomFactor + zoomStep, 5.0f);
                }
                else
                {
                    newZoomFactor = Math.Max(zoomFactor - zoomStep, 0.1f);
                }

                if (newZoomFactor != zoomFactor)
                {
                    zoomFactor = newZoomFactor;
                    Logger.LogDebug($"Mouse wheel zoom changed to {zoomFactor:F1}x", "PictureBox_MouseWheel");
                    UpdateImageDisplay();
                }
            }
        }

        private void MainContainer_MouseWheel(object sender, MouseEventArgs e)
        {
            // Handle scrolling when Ctrl is not held (default scroll behavior)
            if (Control.ModifierKeys != Keys.Control)
            {
                // This will handle normal scrolling in the container
                return;
            }
        }

        private void HandleSelectMouseDown(Point imagePoint)
        {
            // If we're currently editing text, finish that first
            if (isTypingText && editingTextAnnotation != null)
            {
                FinishDirectTextEditing();
                return;
            }

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
                
                // Stop the cropping drag operation
                isCropping = false;
                
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

        private void ApplyCrop()
        {
            const string operation = "applying crop";
            Logger.LogInfo($"Starting crop operation. Crop rectangle: {cropRectangle}", "ApplyCrop");
            
            try
            {
                if (cropRectangle.IsEmpty || screenshot == null)
                {
                    var message = "No valid crop area selected.";
                    Logger.LogWarning(message, "ApplyCrop");
                    MessageBox.Show(message, "Crop Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Debug output
                statusLabel.Text = $"Applying crop: {cropRectangle.Width}x{cropRectangle.Height} at ({cropRectangle.X},{cropRectangle.Y})";
                Logger.LogInfo($"Crop area details - Size: {cropRectangle.Width}x{cropRectangle.Height}, Position: ({cropRectangle.X},{cropRectangle.Y})", "ApplyCrop");
                
                // Ensure crop rectangle is within image bounds
                var imageRect = new Rectangle(0, 0, screenshot.Width, screenshot.Height);
                var cropRect = Rectangle.Intersect(cropRectangle, imageRect);
                
                if (cropRect.Width < 10 || cropRect.Height < 10)
                {
                    var message = $"Crop area is too small. Size: {cropRect.Width}x{cropRect.Height}";
                    Logger.LogWarning(message, "ApplyCrop");
                    MessageBox.Show(message, "Crop Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Logger.LogInfo($"Validated crop rectangle: {cropRect}", "ApplyCrop");

                // Create cropped image
                Bitmap croppedImage = null;
                var success = ExceptionHandler.SafeExecute(() =>
                {
                    Logger.LogDebug("Creating cropped bitmap", "ApplyCrop");
                    croppedImage = new Bitmap(cropRect.Width, cropRect.Height);
                    using (var g = Graphics.FromImage(croppedImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.DrawImage(screenshot, 
                            new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                            cropRect, 
                            GraphicsUnit.Pixel);
                    }
                    Logger.LogInfo("Successfully created cropped image", "ApplyCrop");
                }, "creating cropped image", "Failed to create the cropped image. Please try again with a different crop area.");

                if (!success || croppedImage == null)
                {
                    Logger.LogError("Failed to create cropped image", "ApplyCrop");
                    return;
                }

                // Apply any annotations that fall within the crop area
                var croppedAnnotations = new List<AnnotationObject>();
                ExceptionHandler.SafeExecute(() =>
                {
                    if (annotations != null && annotations.Count > 0)
                    {
                        foreach (var annotation in annotations)
                        {
                            ExceptionHandler.SafeExecute(() =>
                            {
                                var annotationBounds = annotation.GetBounds();
                                if (cropRect.IntersectsWith(annotationBounds))
                                {
                                    // Create a copy of the annotation with adjusted coordinates
                                    var croppedAnnotation = new AnnotationObject
                                    {
                                        Tool = annotation.Tool,
                                        Color = annotation.Color,
                                        Thickness = annotation.Thickness,
                                        Text = annotation.Text,
                                        Font = annotation.Font != null ? 
                                            new Font(annotation.Font.FontFamily, annotation.Font.Size, annotation.Font.Style) : 
                                            new Font("Segoe UI", 12),
                                        CounterNumber = annotation.CounterNumber,
                                        StartPoint = new Point(
                                            Math.Max(0, annotation.StartPoint.X - cropRect.X),
                                            Math.Max(0, annotation.StartPoint.Y - cropRect.Y)
                                        ),
                                        EndPoint = new Point(
                                            Math.Max(0, annotation.EndPoint.X - cropRect.X),
                                            Math.Max(0, annotation.EndPoint.Y - cropRect.Y)
                                        )
                                    };
                                    croppedAnnotations.Add(croppedAnnotation);
                                }
                            }, "processing individual annotation for crop"); // Silent failure for individual annotations
                        }
                    }
                }, "processing annotations for crop", "Some annotations could not be included in the crop.");

                // Apply annotations to the cropped image
                if (croppedAnnotations.Count > 0)
                {
                    ExceptionHandler.SafeExecute(() =>
                    {
                        using (var g = Graphics.FromImage(croppedImage))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            foreach (var annotation in croppedAnnotations)
                            {
                                ExceptionHandler.SafeExecute(() =>
                                {
                                    // Validate annotation has valid properties before drawing
                                    if (annotation != null && annotation.Font != null)
                                    {
                                        DrawAnnotation(g, annotation);
                                    }
                                }, "drawing individual annotation"); // Silent failure for individual annotations
                            }
                        }
                    }, "applying annotations to cropped image", "Annotations could not be applied to the cropped image.");
                }

                // Update the current screenshot safely
                ExceptionHandler.SafeExecute(() =>
                {
                    var oldScreenshot = screenshot;
                    var oldWorkingImage = workingImage;
                    
                    screenshot = new Bitmap(croppedImage);
                    workingImage = new Bitmap(croppedImage);
                    annotations = new List<AnnotationObject>(croppedAnnotations);
                    
                    // Dispose old images
                    oldScreenshot?.Dispose();
                    oldWorkingImage?.Dispose();
                    
                    Logger.LogInfo("Successfully updated main images with cropped version", "ApplyCrop");
                }, "updating main images", "Failed to update the main image with the crop.");

                // Update the current screenshot item if available
                if (currentScreenshotIndex >= 0 && currentScreenshotIndex < screenshots.Count)
                {
                    ExceptionHandler.SafeExecute(() =>
                    {
                        var currentItem = screenshots[currentScreenshotIndex];
                        currentItem.OriginalImage?.Dispose();
                        currentItem.WorkingImage?.Dispose();
                        currentItem.Thumbnail?.Dispose();
                        
                        currentItem.OriginalImage = new Bitmap(croppedImage);
                        currentItem.WorkingImage = new Bitmap(croppedImage);
                        currentItem.Annotations = new List<AnnotationObject>(croppedAnnotations);
                        
                        // Create new thumbnail
                        const int thumbnailSize = 120;
                        var aspectRatio = (float)croppedImage.Width / croppedImage.Height;
                        int thumbWidth = aspectRatio > 1 ? thumbnailSize : (int)(thumbnailSize * aspectRatio);
                        int thumbHeight = aspectRatio > 1 ? (int)(thumbnailSize / aspectRatio) : thumbnailSize;
                        
                        currentItem.Thumbnail = new Bitmap(thumbWidth, thumbHeight);
                        using (var g = Graphics.FromImage(currentItem.Thumbnail))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(croppedImage, 0, 0, thumbWidth, thumbHeight);
                        }
                        
                        Logger.LogInfo("Successfully updated screenshot item with cropped version", "ApplyCrop");
                    }, "updating screenshot item", "Failed to update the screenshot item, but the main image was cropped successfully.");
                }

                // Dispose the temporary cropped image
                croppedImage?.Dispose();

                // Reset crop state
                cropRectangle = Rectangle.Empty;
                isCropping = false;
                
                // Update UI safely
                ExceptionHandler.SafeExecute(() =>
                {
                    // Safely dispose old image and set new one
                    var oldImage = pictureBox.Image;
                    pictureBox.Image = new Bitmap(workingImage);
                    oldImage?.Dispose();
                    
                    UpdateImageDisplay();
                    
                    // Update thumbnail panel if method exists and is accessible
                    ExceptionHandler.SafeExecute(() =>
                    {
                        UpdateThumbnailPanel();
                    }, "updating thumbnail panel"); // Silent failure for thumbnail panel
                    
                    statusLabel.Text = "Image cropped successfully";
                    SetAnnotationTool(AnnotationTool.Select);
                    pictureBox.Invalidate();
                    
                    Logger.LogInfo($"Crop operation completed successfully. New image size: {screenshot.Width}x{screenshot.Height}", "ApplyCrop");
                }, "updating UI after crop", "The image was cropped successfully, but there was an issue updating the display.");
            }
            catch (Exception ex)
            {
                // Reset crop state on any error
                cropRectangle = Rectangle.Empty;
                isCropping = false;
                Logger.LogError("Unexpected error during crop operation", "ApplyCrop", ex);
                ExceptionHandler.HandleUnexpectedException(ex, operation);
                statusLabel.Text = "Crop operation failed";
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
                // For text tool, check if we have a region or just a point
                var width = Math.Abs(imagePoint.X - startPoint.X);
                var height = Math.Abs(imagePoint.Y - startPoint.Y);
                
                if (width > 10 || height > 10) // If user drew a region
                {
                    CreateTextRegionEditor(startPoint, imagePoint);
                }
                else // Single click - create at point
                {
                    CreateInlineTextEditor(imagePoint);
                }
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
                    FillColor = annotationFillColor,
                    LineColor = annotationLineColor,
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
                    FillColor = annotationFillColor,
                    LineColor = annotationLineColor,
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
                    FillColor = Color.FromArgb(255, 255, 255, 255), // White background for text
                    LineColor = annotationLineColor, // Use line color for text
                    Thickness = annotationThickness,
                    Text = "",
                    Font = new Font("Segoe UI", 12 + annotationThickness * 2, FontStyle.Regular)
                };
                annotations.Add(editingTextAnnotation);
            }

            StartDirectTextInput(editingTextAnnotation);
        }

        private void StartDirectTextInput(AnnotationObject textAnnotation)
        {
            editingTextAnnotation = textAnnotation;
            isTypingText = true;
            currentTypingText = textAnnotation.Text ?? "";
            textCursorPosition = currentTypingText.Length;

            // Ensure the form can receive key events
            this.Focus();
            
            RedrawImage();
        }

        private void HandleDirectTextInput(KeyEventArgs e)
        {
            if (editingTextAnnotation == null) return;

            bool textChanged = false;

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    // Cancel text editing
                    FinishDirectTextEditing();
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    // Finish text editing
                    FinishDirectTextEditing();
                    e.Handled = true;
                    break;

                case Keys.Back:
                    // Handle backspace
                    if (textCursorPosition > 0 && currentTypingText.Length > 0)
                    {
                        currentTypingText = currentTypingText.Remove(textCursorPosition - 1, 1);
                        textCursorPosition--;
                        textChanged = true;
                    }
                    e.Handled = true;
                    break;

                case Keys.Delete:
                    // Handle delete
                    if (textCursorPosition < currentTypingText.Length)
                    {
                        currentTypingText = currentTypingText.Remove(textCursorPosition, 1);
                        textChanged = true;
                    }
                    e.Handled = true;
                    break;

                case Keys.Left:
                    // Move cursor left
                    if (textCursorPosition > 0)
                    {
                        textCursorPosition--;
                    }
                    e.Handled = true;
                    break;

                case Keys.Right:
                    // Move cursor right
                    if (textCursorPosition < currentTypingText.Length)
                    {
                        textCursorPosition++;
                    }
                    e.Handled = true;
                    break;

                case Keys.Home:
                    // Move to beginning
                    textCursorPosition = 0;
                    e.Handled = true;
                    break;

                case Keys.End:
                    // Move to end
                    textCursorPosition = currentTypingText.Length;
                    e.Handled = true;
                    break;
            }

            if (textChanged)
            {
                editingTextAnnotation.Text = currentTypingText;
                RedrawImage();
            }
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (isTypingText && editingTextAnnotation != null)
            {
                // Handle character input
                if (!char.IsControl(e.KeyChar))
                {
                    currentTypingText = currentTypingText.Insert(textCursorPosition, e.KeyChar.ToString());
                    textCursorPosition++;
                    editingTextAnnotation.Text = currentTypingText;
                    RedrawImage();
                    e.Handled = true;
                }
            }
        }

        private void FinishDirectTextEditing()
        {
            if (editingTextAnnotation != null)
            {
                // If text is empty, remove the annotation
                if (string.IsNullOrWhiteSpace(currentTypingText))
                {
                    annotations.Remove(editingTextAnnotation);
                }

                isTypingText = false;
                editingTextAnnotation = null;
                currentTypingText = "";
                textCursorPosition = 0;
                RedrawImage();
            }
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

            var okButton = new RoundedButton
            {
                Text = "OK",
                Location = new Point(195, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK,
                CornerRadius = 3, // Match VS Code angular style
                BackColor = Color.FromArgb(0, 122, 204), // VS Code blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 1;
            okButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);

            var cancelButton = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(115, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel,
                CornerRadius = 3, // Match VS Code angular style
                BackColor = Color.FromArgb(0, 122, 204), // VS Code blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 1;
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);

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
                    FillColor = Color.FromArgb(255, 255, 255, 255), // White background for text
                    LineColor = annotationLineColor, // Use line color for text
                    Thickness = annotationThickness,
                    Text = textBox.Text,
                    Font = new Font("Segoe UI", 12 + annotationThickness * 2, FontStyle.Regular)
                };

                annotations.Add(annotation);
                RedrawImage();
            }

            inputForm.Dispose();
        }

        private void CreateTextRegionEditor(Point startPoint, Point endPoint)
        {
            // Normalize the rectangle coordinates
            var left = Math.Min(startPoint.X, endPoint.X);
            var top = Math.Min(startPoint.Y, endPoint.Y);
            var right = Math.Max(startPoint.X, endPoint.X);
            var bottom = Math.Max(startPoint.Y, endPoint.Y);
            
            var regionRect = new Rectangle(left, top, right - left, bottom - top);

            // Create a new text annotation with the region boundaries
            editingTextAnnotation = new AnnotationObject
            {
                Tool = AnnotationTool.Text,
                StartPoint = new Point(left, top),
                EndPoint = new Point(right, bottom),
                FillColor = Color.FromArgb(240, 255, 255, 255), // Semi-transparent white background for region text
                LineColor = annotationLineColor, // Use line color for text
                Thickness = annotationThickness,
                Text = "",
                Font = new Font("Segoe UI", 12 + annotationThickness * 2, FontStyle.Regular),
                IsTextRegion = true // Flag to indicate this is a region-based text
            };
            
            annotations.Add(editingTextAnnotation);
            StartDirectTextInput(editingTextAnnotation);
        }

        private void ShowRegionalTextEditor(AnnotationObject textAnnotation, Rectangle imageRect)
        {
            // Remove any existing inline text editor
            HideInlineTextEditor();

            editingTextAnnotation = textAnnotation;

            // Calculate display position and size
            var displayRect = new Rectangle(
                (int)(imageRect.X * zoomFactor) - imagePanel.HorizontalScroll.Value + pictureBox.Left,
                (int)(imageRect.Y * zoomFactor) - imagePanel.VerticalScroll.Value + pictureBox.Top,
                (int)(imageRect.Width * zoomFactor),
                (int)(imageRect.Height * zoomFactor)
            );

            // Ensure minimum size
            if (displayRect.Width < 100) displayRect.Width = 100;
            if (displayRect.Height < 40) displayRect.Height = 40;

            // Create regional text box
            inlineTextBox = new TextBox
            {
                Text = textAnnotation.Text ?? "",
                Font = new Font("Segoe UI", Math.Max(8, (int)(textAnnotation.Font.Size * zoomFactor * 0.6f)), FontStyle.Regular),
                ForeColor = textAnnotation.Color,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = displayRect.Location,
                Size = displayRect.Size,
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical
            };

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
                        using (var brush = new SolidBrush(annotationFillColor))
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
                        using (var pen = new Pen(annotationLineColor, displayThickness))
                        {
                            g.DrawRectangle(pen, rectBounds);
                        }
                        break;

                    case AnnotationTool.Line:
                        using (var pen = new Pen(annotationLineColor, displayThickness))
                        {
                            g.DrawLine(pen, displayStart, displayCurrent);
                        }
                        break;

                    case AnnotationTool.Arrow:
                        using (var pen = new Pen(annotationLineColor, displayThickness))
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

                    case AnnotationTool.Text:
                        // Draw preview rectangle for text region
                        var textRect = new Rectangle(
                            Math.Min(displayStart.X, displayCurrent.X),
                            Math.Min(displayStart.Y, displayCurrent.Y),
                            Math.Abs(displayCurrent.X - displayStart.X),
                            Math.Abs(displayCurrent.Y - displayStart.Y)
                        );
                        
                        // Draw dashed border to indicate text region
                        using (var pen = new Pen(annotationLineColor, Math.Max(1, displayThickness)))
                        {
                            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            g.DrawRectangle(pen, textRect);
                        }
                        
                        // Draw "Text" label in the center
                        if (textRect.Width > 40 && textRect.Height > 20)
                        {
                            using (var brush = new SolidBrush(annotationLineColor))
                            using (var font = new Font("Segoe UI", Math.Max(8, displayThickness + 8), FontStyle.Regular))
                            {
                                var text = "Text";
                                var textSize = g.MeasureString(text, font);
                                var textPos = new PointF(
                                    textRect.X + (textRect.Width - textSize.Width) / 2,
                                    textRect.Y + (textRect.Height - textSize.Height) / 2
                                );
                                g.DrawString(text, font, brush, textPos);
                            }
                        }
                        break;
                }
            }

            // Draw crop rectangle if in crop mode
            if (currentTool == AnnotationTool.Crop && !cropRectangle.IsEmpty)
            {
                // Convert crop rectangle to display coordinates
                var displayCropRect = new Rectangle(
                    (int)(cropRectangle.X * zoomFactor),
                    (int)(cropRectangle.Y * zoomFactor),
                    (int)(cropRectangle.Width * zoomFactor),
                    (int)(cropRectangle.Height * zoomFactor)
                );

                // Draw crop selection rectangle with dashed border
                using (var pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(pen, displayCropRect);
                }

                // Draw crop area overlay (darken outside area)
                var imageRect = new Rectangle(0, 0, pictureBox.Width, pictureBox.Height);
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Black)))
                {
                    // Top area
                    if (displayCropRect.Top > 0)
                    {
                        g.FillRectangle(brush, new Rectangle(0, 0, imageRect.Width, displayCropRect.Top));
                    }
                    // Bottom area
                    if (displayCropRect.Bottom < imageRect.Height)
                    {
                        g.FillRectangle(brush, new Rectangle(0, displayCropRect.Bottom, imageRect.Width, imageRect.Height - displayCropRect.Bottom));
                    }
                    // Left area
                    if (displayCropRect.Left > 0)
                    {
                        g.FillRectangle(brush, new Rectangle(0, displayCropRect.Top, displayCropRect.Left, displayCropRect.Height));
                    }
                    // Right area
                    if (displayCropRect.Right < imageRect.Width)
                    {
                        g.FillRectangle(brush, new Rectangle(displayCropRect.Right, displayCropRect.Top, imageRect.Width - displayCropRect.Right, displayCropRect.Height));
                    }
                }

                // Draw corner handles for crop rectangle
                var handleSize = 8;
                DrawCropHandle(g, displayCropRect.Location, handleSize);
                DrawCropHandle(g, new Point(displayCropRect.Right, displayCropRect.Top), handleSize);
                DrawCropHandle(g, new Point(displayCropRect.Left, displayCropRect.Bottom), handleSize);
                DrawCropHandle(g, new Point(displayCropRect.Right, displayCropRect.Bottom), handleSize);
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

        private void DrawCropHandle(Graphics g, Point center, int size)
        {
            var handleRect = new Rectangle(
                center.X - size / 2,
                center.Y - size / 2,
                size,
                size
            );

            // White fill with red border for crop handles
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, handleRect);
            }
            using (var pen = new Pen(Color.Red, 2))
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
            if (mainContainer == null || pictureBox == null) return;

            // Get the available area (excluding scroll bars if they exist)
            var availableSize = mainContainer.ClientSize;
            
            // If image is smaller than container, center it
            if (availableSize.Width > pictureBox.Width)
            {
                pictureBox.Left = (availableSize.Width - pictureBox.Width) / 2;
            }
            else
            {
                pictureBox.Left = 0;
            }

            if (availableSize.Height > pictureBox.Height)
            {
                pictureBox.Top = (availableSize.Height - pictureBox.Height) / 2;
            }
            else
            {
                pictureBox.Top = 0;
            }

            // Update the scroll bars when the image size changes
            mainContainer.AutoScrollMinSize = pictureBox.Size;
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
            if (IsEffectivelyMaximized())
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Size = new Size(1000, 700);
                this.CenterToScreen();
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                MaximizeRespectingTaskbar();
            }
        }

        private void ToggleMaximize()
        {
            if (IsEffectivelyMaximized())
            {
                // Restore to normal size
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Size = new Size(1000, 700);
                this.CenterToScreen();
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                // Maximize while respecting taskbar
                MaximizeRespectingTaskbar();
            }
            
            // Update the maximize button icon to reflect the new state
            UpdateMaximizeButtonIcon();
        }

        private void UpdateMaximizeButtonIcon()
        {
            // Find the maximize button in the toolbar and update its icon
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton button && button.ToolTipText == "Maximize/Restore Window")
                {
                    button.Image?.Dispose();
                    button.Image = CreateMaximizeIcon();
                    break;
                }
            }
        }

        // Action methods
        private async void QuickSave()
        {
            try
            {
                statusLabel.Text = "Saving...";
                this.Cursor = Cursors.WaitCursor;

                // Get the save directory from file explorer selection or fallback to desktop
                string saveDirectory = GetSelectedSaveDirectory();

                await Task.Run(() =>
                {
                    var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var filePath = Path.Combine(saveDirectory, fileName);
                    
                    // Ensure we have a valid image to save
                    Bitmap imageToSave = null;
                    
                    // Create image with annotations if any exist
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
                        
                        imageToSave.Save(filePath, ImageFormat.Png);
                        
                        // Clean up the temporary image
                        if (imageToSave != workingImage && imageToSave != screenshot)
                        {
                            imageToSave.Dispose();
                        }
                    }
                    else
                    {
                        // Fallback to screenshot or working image
                        (screenshot ?? workingImage)?.Save(filePath, ImageFormat.Png);
                    }
                });

                string directoryName = Path.GetFileName(saveDirectory.TrimEnd(Path.DirectorySeparatorChar));
                if (string.IsNullOrEmpty(directoryName))
                {
                    directoryName = saveDirectory;
                }
                
                statusLabel.Text = $"Screenshot saved to {directoryName}";
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

        private string GetSelectedSaveDirectory()
        {
            try
            {
                // If file explorer is visible and has a selection, use that
                if (isFileExplorerVisible && fileExplorerTreeView?.SelectedNode != null)
                {
                    string selectedPath = fileExplorerTreeView.SelectedNode.Tag?.ToString();
                    
                    // If it's a directory, use it
                    if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                    {
                        return selectedPath;
                    }
                    
                    // If it's a file, use its parent directory
                    if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
                    {
                        string parentDirectory = Path.GetDirectoryName(selectedPath);
                        if (!string.IsNullOrEmpty(parentDirectory) && Directory.Exists(parentDirectory))
                        {
                            return parentDirectory;
                        }
                    }
                }
                
                // Fallback to Desktop
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to get selected save directory", "GetSelectedSaveDirectory", ex);
                // Fallback to Desktop on any error
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
            // Handle direct text input when typing text
            if (isTypingText && editingTextAnnotation != null)
            {
                HandleDirectTextInput(e);
                return;
            }

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
                    if (currentTool == AnnotationTool.Crop && !cropRectangle.IsEmpty)
                    {
                        // Cancel crop mode
                        cropRectangle = Rectangle.Empty;
                        isCropping = false;
                        statusLabel.Text = "Crop mode - Click and drag to select area to crop, press Enter to apply";
                        pictureBox.Invalidate();
                        e.Handled = true;
                    }
                    else if (selectedAnnotation != null)
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
                    else if (isFileExplorerVisible && fileExplorerTreeView.SelectedNode != null)
                    {
                        // Delete in file explorer
                        DeleteMenuItem_Click(sender, EventArgs.Empty);
                        e.Handled = true;
                    }
                    break;
                case Keys.Enter:
                    if (currentTool == AnnotationTool.Crop && !cropRectangle.IsEmpty)
                    {
                        try
                        {
                            // Apply crop
                            ApplyCrop();
                            e.Handled = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error during crop operation: {ex.Message}", "Crop Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            // Reset crop state on error
                            cropRectangle = Rectangle.Empty;
                            isCropping = false;
                            statusLabel.Text = "Crop operation failed";
                            e.Handled = true;
                        }
                    }
                    else if (!e.Control && !e.Shift)
                    {
                        QuickSave();
                    }
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
                case Keys.F2:
                    // Rename in file explorer
                    if (isFileExplorerVisible && fileExplorerTreeView.SelectedNode != null)
                    {
                        RenameMenuItem_Click(sender, EventArgs.Empty);
                        e.Handled = true;
                    }
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

        private void OnSizeChanged(object sender, EventArgs e)
        {
            // Ensure taskbar icon remains visible during window state changes
            this.ShowInTaskbar = true;
            
            // Update maximize button icon when window state changes
            UpdateMaximizeButtonIcon();
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
