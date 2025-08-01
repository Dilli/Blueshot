using System;
using System.Windows.Forms;

namespace Blueshot
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Enable visual styles for modern Windows appearance
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Start the main application
                var mainForm = new MainForm();
                
                // Show the form briefly to trigger OnShown, then it will hide itself
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.ShowInTaskbar = false;
                mainForm.Show();
                
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Blueshot Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
