using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Blueshot
{
    public class GlobalHotkey
    {
        // Windows API constants and functions
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_PRIMARY = 9000;
        private const int HOTKEY_ID_FALLBACK = 9001;
        private const int HOTKEY_ID_CURRENT_SCREEN = 9002;
        
        // Modifiers
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;
        
        // Virtual key codes
        private const uint VK_SNAPSHOT = 0x2C; // Print Screen key
        private const uint VK_F12 = 0x7B;      // F12 key

        private IntPtr windowHandle;
        private bool primaryHotkeyRegistered = false;
        private bool fallbackHotkeyRegistered = false;
        private bool currentScreenHotkeyRegistered = false;
        private string registeredHotkeyDescription = "";
        private string currentScreenHotkeyDescription = "";

        public event EventHandler HotkeyPressed;
        public event EventHandler CurrentScreenHotkeyPressed;
        public string RegisteredHotkeyDescription => registeredHotkeyDescription;
        public string CurrentScreenHotkeyDescription => currentScreenHotkeyDescription;

        public GlobalHotkey(IntPtr handle)
        {
            windowHandle = handle;
        }

        public bool RegisterHotkey()
        {
            // First try to register Print Screen for region capture
            try
            {
                primaryHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID_PRIMARY, MOD_NONE, VK_SNAPSHOT);
                if (primaryHotkeyRegistered)
                {
                    registeredHotkeyDescription = "Print Screen";
                }
            }
            catch (Exception)
            {
                primaryHotkeyRegistered = false;
            }

            // Try to register Alt+Print Screen for current screen capture (without taskbar)
            try
            {
                currentScreenHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID_CURRENT_SCREEN, MOD_ALT, VK_SNAPSHOT);
                if (currentScreenHotkeyRegistered)
                {
                    currentScreenHotkeyDescription = "Alt+Print Screen";
                }
            }
            catch (Exception)
            {
                currentScreenHotkeyRegistered = false;
            }

            // If Print Screen failed, try Ctrl+Shift+F12 as fallback for region capture
            if (!primaryHotkeyRegistered)
            {
                try
                {
                    fallbackHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID_FALLBACK, MOD_CTRL | MOD_SHIFT, VK_F12);
                    if (fallbackHotkeyRegistered)
                    {
                        registeredHotkeyDescription = "Ctrl+Shift+F12";
                    }
                }
                catch (Exception)
                {
                    fallbackHotkeyRegistered = false;
                }
            }

            // Return true if at least one hotkey was registered
            bool anyRegistered = primaryHotkeyRegistered || fallbackHotkeyRegistered || currentScreenHotkeyRegistered;
            
            if (!anyRegistered)
            {
                registeredHotkeyDescription = "";
                currentScreenHotkeyDescription = "";
            }
            
            return anyRegistered;
        }

        public void UnregisterHotkey()
        {
            if (primaryHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, HOTKEY_ID_PRIMARY);
                primaryHotkeyRegistered = false;
            }
            
            if (fallbackHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, HOTKEY_ID_FALLBACK);
                fallbackHotkeyRegistered = false;
            }
            
            if (currentScreenHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, HOTKEY_ID_CURRENT_SCREEN);
                currentScreenHotkeyRegistered = false;
            }
            
            registeredHotkeyDescription = "";
            currentScreenHotkeyDescription = "";
        }

        public bool ProcessHotkey(Message m)
        {
            const int WM_HOTKEY = 0x0312;
            
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == HOTKEY_ID_PRIMARY || hotkeyId == HOTKEY_ID_FALLBACK)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                else if (hotkeyId == HOTKEY_ID_CURRENT_SCREEN)
                {
                    CurrentScreenHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            return false;
        }
    }
}
