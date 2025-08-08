using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Blueshot
{
    public partial class MainForm : Form
    {
        private RoundedButton captureButton;
        private RoundedButton currentScreenButton;
        private RoundedButton settingsButton;
        private Label statusLabel;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private GlobalHotkey globalHotkey;
        private PreviewForm previewForm; // Single instance for all screenshots
        private IntPtr lastActiveWindow = IntPtr.Zero; // Store the last active window for tray menu capture

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
            this.ShowInTaskbar = true;  // Ensure it shows in taskbar by default
            this.Icon = LoadCustomIcon();
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.KeyPreview = true; // Enable form to receive key events
            
            // Add rounded corners for modern look
            this.Region = CreateRoundedRegion(this.Width, this.Height, 15);

            // Add close button since there's no title bar
            var closeButton = new RoundedButton
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(this.Width - 45, 10),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                CornerRadius = 5
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
            closeButton.Click += (s, e) => 
            {
                this.Hide();
                // Don't set ShowInTaskbar = false - let it stay available for when window is shown again
            };
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = Color.White;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = Color.FromArgb(200, 200, 200);
            this.Controls.Add(closeButton);

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
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
            captureButton = new RoundedButton
            {
                Text = "&Start Region Capture (F1)",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None,
                CornerRadius = 5
            };
            captureButton.FlatAppearance.BorderSize = 0;
            captureButton.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 200);
            captureButton.Click += CaptureButton_Click;

            // Current screen capture button
            currentScreenButton = new RoundedButton
            {
                Text = "&Capture Window (F3)",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None,
                CornerRadius = 5
            };
            currentScreenButton.FlatAppearance.BorderSize = 0;
            currentScreenButton.FlatAppearance.BorderColor = Color.FromArgb(0, 130, 80);
            currentScreenButton.Click += CurrentScreenButton_Click;

            // Settings button
            settingsButton = new RoundedButton
            {
                Text = "&Settings (F2)",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None,
                CornerRadius = 5
            };
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            settingsButton.Click += SettingsButton_Click;

            // Status label
            statusLabel = new Label
            {
                Text = "Ready • Print Screen: Region • Alt+Print Screen: Current Screen • F1: Region • F2: Settings • F3: Current Screen • Esc: Hide",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Add controls to panel
            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.Controls.Add(captureButton, 0, 1);
            mainPanel.Controls.Add(currentScreenButton, 0, 2);
            mainPanel.Controls.Add(settingsButton, 0, 3);
            mainPanel.Controls.Add(statusLabel, 0, 4);

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
            trayMenu.Opening += (s, e) => {
                // Don't capture window here as it will be the tray/taskbar
                // Instead, we'll use a smarter approach in the capture method
                Logger.LogInfo("Tray menu opening");
            };
            trayMenu.Items.Add("Capture Region", null, (s, e) => StartRegionCapture());
            trayMenu.Items.Add("Capture Window", null, (s, e) => StartCurrentScreenCapture());
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
                
                if (globalHotkey != null && !string.IsNullOrEmpty(globalHotkey.CurrentScreenHotkeyDescription))
                {
                    trayMenu.Items[1].Text = $"Capture Window ({globalHotkey.CurrentScreenHotkeyDescription})";
                }
                else
                {
                    trayMenu.Items[1].Text = "Capture Window";
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
            
            // Ensure the icon is properly displayed in the system tray
            // This helps with notification icon display issues
            RefreshTrayIcon();
            
            // Don't show initial balloon tip here - it will be shown after hotkey registration
        }

        private void InitializeGlobalHotkey()
        {
            try
            {
                globalHotkey = new GlobalHotkey(this.Handle);
                globalHotkey.HotkeyPressed += (s, e) => 
                {
                    // Run region capture on UI thread
                    this.BeginInvoke(new Action(() => StartRegionCapture()));
                };
                
                globalHotkey.CurrentScreenHotkeyPressed += (s, e) => 
                {
                    // Run current screen capture on UI thread
                    this.BeginInvoke(new Action(() => StartCurrentScreenCapture()));
                };
                
                bool hotkeyRegistered = globalHotkey.RegisterHotkey();
                
                if (hotkeyRegistered)
                {
                    // Show success message with the actual hotkeys that were registered
                    string hotkeyDesc = globalHotkey.RegisteredHotkeyDescription;
                    string currentScreenDesc = globalHotkey.CurrentScreenHotkeyDescription;
                    
                    string message = "Screenshot hotkeys registered:";
                    if (!string.IsNullOrEmpty(hotkeyDesc))
                    {
                        message += $"\n• {hotkeyDesc} - Region capture";
                    }
                    if (!string.IsNullOrEmpty(currentScreenDesc))
                    {
                        message += $"\n• {currentScreenDesc} - Capture window";
                    }
                    
                    trayIcon.ShowBalloonTip(3000, "Blueshot Ready", message, ToolTipIcon.Info);
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
                Logger.LogError("Error initializing global hotkey", "InitializeGlobalHotkey", ex);
                ExceptionHandler.HandleExpectedException(ex, "initializing global hotkey", 
                    "Error initializing global hotkey. You can still use the system tray to capture screenshots.");
                    
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

        private void CurrentScreenButton_Click(object sender, EventArgs e)
        {
            StartCurrentScreenCapture();
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

        private void StartCurrentScreenCapture()
        {
            statusLabel.Text = "Capturing window...";
            this.Hide();

            try
            {
                var captureManager = new ScreenCaptureManager();
                Bitmap screenshot = null;

                // Add a longer delay to allow tray menu to close and user to focus on target window
                Logger.LogInfo("Waiting for menu to close and getting active window...");
                System.Threading.Thread.Sleep(500);
                
                // Get the currently active window after the delay
                var activeWindow = GetForegroundWindow();
                Logger.LogInfo($"Active window after delay: {activeWindow}");

                if (activeWindow != IntPtr.Zero && IsWindowValid(activeWindow))
                {
                    // Check if it's not the desktop or taskbar
                    string windowTitle = GetWindowTitle(activeWindow);
                    Logger.LogInfo($"Window title: '{windowTitle}'");
                    
                    if (!string.IsNullOrEmpty(windowTitle) && 
                        !windowTitle.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) &&
                        !windowTitle.Contains("Taskbar", StringComparison.OrdinalIgnoreCase))
                    {
                        screenshot = captureManager.CaptureWindow(activeWindow);
                        Logger.LogInfo("Successfully captured active window");
                    }
                    else
                    {
                        Logger.LogInfo("Active window is desktop/taskbar, falling back to screen capture");
                    }
                }

                // If we don't have a valid window screenshot, fall back to active window capture
                if (screenshot == null)
                {
                    Logger.LogInfo("Falling back to active window capture method");
                    screenshot = captureManager.CaptureActiveWindow();
                }

                if (screenshot != null && screenshot.Width > 0 && screenshot.Height > 0)
                {
                    if (previewForm == null || previewForm.IsDisposed)
                    {
                        previewForm = new PreviewForm();
                        previewForm.FormClosed += (s, e) => previewForm = null;
                    }

                    previewForm.AddScreenshot(screenshot);

                    if (!previewForm.Visible)
                    {
                        previewForm.Show();
                    }
                    else
                    {
                        previewForm.BringToFront();
                    }

                    statusLabel.Text = "Current screen captured successfully";
                }
                else
                {
                    Logger.LogError("Window capture returned null or invalid image");
                    MessageBox.Show("Failed to capture window. Please try again.", "Capture Failed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "Window capture failed";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during current screen capture: {ex.Message}");
                MessageBox.Show($"Error during capture: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Capture failed";
            }
            finally
            {
                this.Show();
            }
        }

        private void OnRegionSelected(object sender, RegionSelectedEventArgs e)
        {
            if (e.SelectedRegion.IsEmpty)
            {
                statusLabel.Text = "Capture cancelled";
                return;
            }

            if (e.SelectedRegion.Width <= 0 || e.SelectedRegion.Height <= 0)
            {
                Logger.LogError($"Invalid region selected: {e.SelectedRegion}");
                MessageBox.Show($"Invalid capture region selected: {e.SelectedRegion.Width}x{e.SelectedRegion.Height}", "Invalid Region", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                statusLabel.Text = "Invalid region selected";
                return;
            }

            statusLabel.Text = "Processing capture...";

            try
            {
                var captureManager = new ScreenCaptureManager();
                var screenshot = captureManager.CaptureRegion(e.SelectedRegion);
                
                if (screenshot != null && screenshot.Width > 0 && screenshot.Height > 0)
                {
                    ShowPreview(screenshot);
                }
                else
                {
                    Logger.LogError("Region capture returned null or invalid image");
                    MessageBox.Show("Failed to capture the selected region. Please try again.", "Capture Failed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "Failed to capture screenshot";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error capturing region: {ex.Message}");
                MessageBox.Show($"Error saving screenshot: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Capture failed";
            }
        }

        private void ShowPreview(Bitmap screenshot)
        {
            try
            {
                if (screenshot == null)
                {
                    Logger.LogError("ShowPreview called with null screenshot");
                    MessageBox.Show("Cannot show preview: Screenshot is null", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Preview failed - no image";
                    return;
                }
                
                if (screenshot.Width <= 0 || screenshot.Height <= 0)
                {
                    Logger.LogError($"ShowPreview called with invalid dimensions: {screenshot.Width}x{screenshot.Height}");
                    MessageBox.Show($"Cannot show preview: Invalid image dimensions ({screenshot.Width}x{screenshot.Height})", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Preview failed - invalid dimensions";
                    return;
                }

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
                Logger.LogError($"Error showing preview: {ex.Message}");
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
            this.ShowInTaskbar = true;  // Ensure taskbar icon is shown
            this.Show();
            this.WindowState = FormWindowState.Normal;
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
                // Don't set ShowInTaskbar = false here - let it stay available for when window is shown
                
                // Show balloon tip to inform user
                trayIcon.BalloonTipTitle = "Blueshot";
                trayIcon.BalloonTipText = "Blueshot is still running. Right-click the tray icon to show window or exit.";
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
                case Keys.F3:
                    StartCurrentScreenCapture();
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    this.Hide();
                    // Don't set ShowInTaskbar = false - let it stay available for when window is shown again
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
                    // Load and validate the icon
                    var customIcon = new Icon(iconPath);
                    
                    // Ensure the icon is valid for notifications
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
                Console.WriteLine($"Failed to load custom icon: {ex.Message}");
            }
            
            // Fallback to system icon if custom icon fails to load
            return SystemIcons.Application;
        }

        private void RefreshTrayIcon()
        {
            try
            {
                if (trayIcon != null)
                {
                    // Force refresh the tray icon to ensure proper display in notifications
                    // This is especially important for balloon tip icons
                    trayIcon.Visible = false;
                    
                    // Small delay to ensure the icon is removed from tray
                    Application.DoEvents();
                    
                    // Reload the icon
                    var currentIcon = trayIcon.Icon;
                    trayIcon.Icon = LoadCustomIcon();
                    
                    // Make visible again
                    trayIcon.Visible = true;
                    
                    // Dispose old icon if it was different
                    if (currentIcon != null && currentIcon != trayIcon.Icon)
                    {
                        currentIcon.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh tray icon: {ex.Message}");
            }
        }

        #region Windows API
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        private bool IsWindowValid(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && 
                   IsWindow(windowHandle) && 
                   IsWindowVisible(windowHandle);
        }

        private string GetWindowTitle(IntPtr windowHandle)
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(windowHandle, title, title.Capacity);
            return title.ToString();
        }
        #endregion
    }
}
