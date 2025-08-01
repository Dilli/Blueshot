using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Blueshot
{
    public partial class MainForm : Form
    {
        private Button captureButton;
        private Button settingsButton;
        private Label statusLabel;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private GlobalHotkey globalHotkey;
        private PreviewForm previewForm; // Single instance for all screenshots

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            // Hide immediately but register hotkey after form is shown
            this.Hide();
            
            // Now register the hotkey when the form handle is available
            InitializeGlobalHotkey();
        }

        private void InitializeComponent()
        {
            this.Text = "Blueshot - Screen Capture Tool";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Icon = LoadCustomIcon();
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.KeyPreview = true; // Enable form to receive key events
            
            // Add rounded corners for modern look
            this.Region = CreateRoundedRegion(this.Width, this.Height, 15);

            // Add close button since there's no title bar
            var closeButton = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(this.Width - 45, 10),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
            closeButton.Click += (s, e) => 
            {
                this.Hide();
                this.ShowInTaskbar = false;
            };
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = Color.White;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = Color.FromArgb(200, 200, 200);
            this.Controls.Add(closeButton);

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };

            // Title label
            var titleLabel = new Label
            {
                Text = "Blueshot",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Capture button
            captureButton = new Button
            {
                Text = "&Start Region Capture (F1)",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            captureButton.FlatAppearance.BorderSize = 0;
            captureButton.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 200);
            captureButton.Click += CaptureButton_Click;

            // Settings button
            settingsButton = new Button
            {
                Text = "&Settings (F2)",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            settingsButton.Click += SettingsButton_Click;

            // Status label
            statusLabel = new Label
            {
                Text = "Ready • Print Screen: Capture • F1: Capture • F2: Settings • Esc: Hide",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Add controls to panel
            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.Controls.Add(captureButton, 0, 1);
            mainPanel.Controls.Add(settingsButton, 0, 2);
            mainPanel.Controls.Add(statusLabel, 0, 3);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            this.Controls.Add(mainPanel);
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Capture Region", null, (s, e) => StartRegionCapture());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Show Window", null, (s, e) => ShowMainWindow());
            trayMenu.Items.Add("Settings", null, (s, e) => SettingsButton_Click(this, EventArgs.Empty));
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            // Update the first menu item after hotkey registration
            trayMenu.Opening += (s, e) =>
            {
                if (globalHotkey != null && !string.IsNullOrEmpty(globalHotkey.RegisteredHotkeyDescription))
                {
                    trayMenu.Items[0].Text = $"Capture Region ({globalHotkey.RegisteredHotkeyDescription})";
                }
                else
                {
                    trayMenu.Items[0].Text = "Capture Region";
                }
            };

            trayIcon = new NotifyIcon
            {
                Text = "Blueshot - Press Print Screen to capture",
                Icon = LoadCustomIcon(),
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => StartRegionCapture();
            trayIcon.BalloonTipTitle = "Blueshot";
            // Don't show initial balloon tip here - it will be shown after hotkey registration
        }

        private void InitializeGlobalHotkey()
        {
            try
            {
                globalHotkey = new GlobalHotkey(this.Handle);
                globalHotkey.HotkeyPressed += (s, e) => 
                {
                    // Run capture on UI thread
                    this.BeginInvoke(new Action(() => StartRegionCapture()));
                };
                
                bool hotkeyRegistered = globalHotkey.RegisterHotkey();
                
                if (hotkeyRegistered)
                {
                    // Show success message with the actual hotkey that was registered
                    string hotkeyDesc = globalHotkey.RegisteredHotkeyDescription;
                    trayIcon.ShowBalloonTip(3000, "Blueshot Ready", 
                        $"Screenshot hotkey registered: {hotkeyDesc}", 
                        ToolTipIcon.Info);
                }
                else
                {
                    string errorMessage = "Warning: Could not register any screenshot hotkeys. ";
                    errorMessage += "This might be because other applications (OneDrive, Teams, etc.) are using these keys.";
                    errorMessage += "\n\nYou can still capture using:\n• Double-click system tray icon\n• Right-click tray → Capture Region";
                    
                    MessageBox.Show(errorMessage, "Hotkey Registration", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                    // Show fallback notification
                    trayIcon.ShowBalloonTip(5000, "Blueshot Ready", 
                        "Screenshot hotkeys unavailable. Use system tray to capture.", 
                        ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing global hotkey: {ex.Message}", 
                    "Hotkey Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                // Show fallback notification
                trayIcon.ShowBalloonTip(5000, "Blueshot Ready", 
                    "Hotkey unavailable. Use system tray to capture.", 
                    ToolTipIcon.Warning);
            }
        }

        private void ExitApplication()
        {
            globalHotkey?.UnregisterHotkey();
            trayIcon?.Dispose();
            Application.Exit();
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            StartRegionCapture();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Settings panel will be implemented soon!", "Settings", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartRegionCapture()
        {
            // Show notification to confirm hotkey is working
            trayIcon.ShowBalloonTip(1000, "Blueshot", "Print Screen detected! Starting capture...", ToolTipIcon.Info);
            
            statusLabel.Text = "Starting region capture...";
            this.Hide();

            try
            {
                var regionSelector = new RegionSelectorOverlay();
                regionSelector.RegionSelected += OnRegionSelected;
                regionSelector.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during capture: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Show();
                statusLabel.Text = "Ready to capture";
            }
        }

        private void OnRegionSelected(object sender, RegionSelectedEventArgs e)
        {
            if (e.SelectedRegion.IsEmpty)
            {
                statusLabel.Text = "Capture cancelled";
                return;
            }

            statusLabel.Text = "Processing capture...";

            try
            {
                var captureManager = new ScreenCaptureManager();
                var screenshot = captureManager.CaptureRegion(e.SelectedRegion);
                
                if (screenshot != null)
                {
                    ShowPreview(screenshot);
                }
                else
                {
                    statusLabel.Text = "Failed to capture screenshot";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving screenshot: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Capture failed";
            }
        }

        private void ShowPreview(Bitmap screenshot)
        {
            try
            {
                if (previewForm == null || previewForm.IsDisposed)
                {
                    // Create new preview form if it doesn't exist
                    previewForm = new PreviewForm(screenshot);
                    previewForm.FormClosed += (s, e) =>
                    {
                        previewForm = null;
                        statusLabel.Text = "Preview closed";
                    };
                    previewForm.Show();
                    statusLabel.Text = "Preview opened";
                }
                else
                {
                    // Add screenshot to existing preview form
                    previewForm.AddScreenshot(screenshot);
                    
                    // Bring the existing form to front
                    if (previewForm.WindowState == FormWindowState.Minimized)
                    {
                        previewForm.WindowState = FormWindowState.Normal;
                    }
                    previewForm.BringToFront();
                    previewForm.Activate();
                    
                    statusLabel.Text = "Screenshot added to preview";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing preview: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Preview failed";
            }
        }

        private void SaveScreenshot(Bitmap screenshot)
        {
            // Legacy method - now handled by PreviewForm
            var saveDialog = new SaveFileDialog
            {
                Filter = "PNG files (*.png)|*.png|JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var format = saveDialog.FilterIndex == 2 ? 
                    System.Drawing.Imaging.ImageFormat.Jpeg : 
                    System.Drawing.Imaging.ImageFormat.Png;
                
                screenshot.Save(saveDialog.FileName, format);
            }
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
        }

        protected override void WndProc(ref Message m)
        {
            if (globalHotkey?.ProcessHotkey(m) == true)
            {
                return; // Hotkey was processed
            }
            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (!value)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Instead of asking, just minimize to tray
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
                
                // Show balloon tip to inform user
                trayIcon.BalloonTipTitle = "Blueshot";
                trayIcon.BalloonTipText = "Blueshot is still running. Right-click the tray icon to exit.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(2000);
            }
            else
            {
                globalHotkey?.UnregisterHotkey();
                trayIcon?.Dispose();
                base.OnFormClosing(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    StartRegionCapture();
                    e.Handled = true;
                    break;
                case Keys.F2:
                    SettingsButton_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    this.Hide();
                    this.ShowInTaskbar = false;
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    if (e.Control)
                    {
                        StartRegionCapture();
                        e.Handled = true;
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        private Region CreateRoundedRegion(int width, int height, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(width - radius, 0, radius, radius, 270, 90);
            path.AddArc(width - radius, height - radius, radius, radius, 0, 90);
            path.AddArc(0, height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return new Region(path);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                this.Region = CreateRoundedRegion(this.Width, this.Height, 15);
            }
        }

        // Add drag functionality for borderless form
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        private Icon LoadCustomIcon()
        {
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load custom icon: {ex.Message}");
            }
            
            // Fallback to system icon if custom icon fails to load
            return SystemIcons.Application;
        }
    }
}
