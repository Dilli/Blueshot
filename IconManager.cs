using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Blueshot
{
    /// <summary>
    /// Manages loading of toolbar icons from files with fallback to programmatic generation
    /// </summary>
    public static class IconManager
    {
        private static readonly string IconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
        
        /// <summary>
        /// Loads an icon from file or generates it programmatically as fallback
        /// </summary>
        /// <param name="iconName">Name of the icon file (without extension)</param>
        /// <param name="fallbackGenerator">Function to generate icon if file not found</param>
        /// <param name="size">Desired icon size (24 for normal, 48 for high DPI)</param>
        /// <returns>Bitmap of the icon</returns>
        public static Bitmap LoadIcon(string iconName, Func<Bitmap> fallbackGenerator, int size = 24)
        {
            try
            {
                // Try to load high DPI version first if requested
                if (size == 48)
                {
                    string highDpiPath = Path.Combine(IconsPath, $"{iconName}@2x.png");
                    if (File.Exists(highDpiPath))
                    {
                        return new Bitmap(highDpiPath);
                    }
                }
                
                // Try to load normal version
                string normalPath = Path.Combine(IconsPath, $"{iconName}.png");
                if (File.Exists(normalPath))
                {
                    var bitmap = new Bitmap(normalPath);
                    
                    // Resize if needed
                    if (bitmap.Width != size || bitmap.Height != size)
                    {
                        var resized = new Bitmap(size, size);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(bitmap, 0, 0, size, size);
                        }
                        bitmap.Dispose();
                        return resized;
                    }
                    
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with fallback
                System.Diagnostics.Debug.WriteLine($"Failed to load icon {iconName}: {ex.Message}");
            }
            
            // Fallback to programmatic generation
            return fallbackGenerator();
        }
        
        /// <summary>
        /// Checks if the icons directory exists and has any PNG files
        /// </summary>
        public static bool HasCustomIcons()
        {
            return Directory.Exists(IconsPath) && 
                   Directory.GetFiles(IconsPath, "*.png").Length > 0;
        }
        
        /// <summary>
        /// Gets the path to the icons directory
        /// </summary>
        public static string GetIconsPath()
        {
            return IconsPath;
        }
        
        /// <summary>
        /// Ensures the icons directory exists
        /// </summary>
        public static void EnsureIconsDirectory()
        {
            if (!Directory.Exists(IconsPath))
            {
                Directory.CreateDirectory(IconsPath);
            }
        }
    }
}
