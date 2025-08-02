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
            // Initialize logging first
            Logger.LogApplicationStart();
            
            // Initialize global exception handling
            ExceptionHandler.Initialize();
            
            try
            {
                // Enable visual styles for modern Windows appearance
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                Logger.LogInfo("Application initialization completed", "Program");

                // Start the main application
                var mainForm = new MainForm();
                
                // Show the form briefly to trigger OnShown, then it will hide itself
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.ShowInTaskbar = false;
                mainForm.Show();
                
                Logger.LogInfo("Starting application message loop", "Program");
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Logger.LogError("Critical error in main application", "Program", ex);
                ExceptionHandler.HandleExpectedException(ex, "starting the application",
                    "A critical error occurred while starting Blueshot. Please check the log files for details.");
            }
            finally
            {
                Logger.LogApplicationEnd();
            }
        }
    }
}
