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
        
        // Modifiers
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        
        // Virtual key codes
        private const uint VK_SNAPSHOT = 0x2C; // Print Screen key
        private const uint VK_F12 = 0x7B;      // F12 key

        private IntPtr windowHandle;
        private bool primaryHotkeyRegistered = false;
        private bool fallbackHotkeyRegistered = false;
        private string registeredHotkeyDescription = "";

        public event EventHandler HotkeyPressed;
        public string RegisteredHotkeyDescription => registeredHotkeyDescription;

        public GlobalHotkey(IntPtr handle)
        {
            windowHandle = handle;
        }

        public bool RegisterHotkey()
        {
            // First try to register Print Screen
            try
            {
                primaryHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID_PRIMARY, MOD_NONE, VK_SNAPSHOT);
                if (primaryHotkeyRegistered)
                {
                    registeredHotkeyDescription = "Print Screen";
                    return true;
                }
            }
            catch (Exception)
            {
                primaryHotkeyRegistered = false;
            }

            // If Print Screen failed, try Ctrl+Shift+F12 as fallback
            try
            {
                fallbackHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID_FALLBACK, MOD_CTRL | MOD_SHIFT, VK_F12);
                if (fallbackHotkeyRegistered)
                {
                    registeredHotkeyDescription = "Ctrl+Shift+F12";
                    return true;
                }
            }
            catch (Exception)
            {
                fallbackHotkeyRegistered = false;
            }

            registeredHotkeyDescription = "";
            return false;
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
            
            registeredHotkeyDescription = "";
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
            }
            return false;
        }
    }
}
