using System;
using System.Threading;
using System.Windows.Forms;

namespace Blueshot
{
    public static class ExceptionHandler
    {
        private static bool _isHandlingException = false;

        public static void Initialize()
        {
            // Handle unhandled exceptions in UI thread
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += HandleUIThreadException;
            
            // Handle unhandled exceptions in background threads
            AppDomain.CurrentDomain.UnhandledException += HandleNonUIThreadException;
            
            Logger.LogInfo("Exception handling initialized", "ExceptionHandler");
        }

        private static void HandleUIThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, "UI Thread Exception");
        }

        private static void HandleNonUIThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var isTerminating = e.IsTerminating ? " (Application Terminating)" : "";
            HandleException(exception, $"Background Thread Exception{isTerminating}");
        }

        private static void HandleException(Exception exception, string context)
        {
            if (_isHandlingException)
            {
                // Prevent recursive exception handling
                return;
            }

            try
            {
                _isHandlingException = true;
                
                Logger.LogError($"Unhandled exception in {context}", "ExceptionHandler", exception);

                var errorMessage = $"An unexpected error occurred in Blueshot.\n\n" +
                                  $"Error: {exception?.Message ?? "Unknown error"}\n\n" +
                                  $"The error has been logged. You can find the log file at:\n" +
                                  $"{Logger.GetLogFilePath()}\n\n" +
                                  $"Would you like to continue using the application?";

                var result = MessageBox.Show(errorMessage, "Blueshot - Unexpected Error", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                if (result == DialogResult.No)
                {
                    Logger.LogInfo("User chose to exit application after unhandled exception", "ExceptionHandler");
                    Application.Exit();
                }
                else
                {
                    Logger.LogInfo("User chose to continue after unhandled exception", "ExceptionHandler");
                }
            }
            catch (Exception loggingException)
            {
                // Last resort: show a simple error without logging
                try
                {
                    MessageBox.Show($"Critical error in exception handler: {loggingException.Message}", 
                        "Blueshot - Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                catch
                {
                    // If even MessageBox fails, we can't do much more
                }
            }
            finally
            {
                _isHandlingException = false;
            }
        }

        public static void HandleExpectedException(Exception exception, string operation, 
            string userMessage = null, bool showToUser = true)
        {
            Logger.LogError($"Expected exception during {operation}", operation, exception);

            if (showToUser)
            {
                var message = userMessage ?? $"An error occurred while {operation.ToLower()}.\n\n" +
                                            $"Error: {exception.Message}";
                
                MessageBox.Show(message, "Blueshot - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void HandleUnexpectedException(Exception exception, string operation)
        {
            Logger.LogError($"Unexpected exception during {operation}", operation, exception);
            
            var message = $"An unexpected error occurred while {operation.ToLower()}.\n\n" +
                         $"Error: {exception.Message}\n\n" +
                         $"Please try again. If the problem persists, check the log file at:\n" +
                         $"{Logger.GetLogFilePath()}";

            MessageBox.Show(message, "Blueshot - Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static T SafeExecute<T>(Func<T> operation, string operationName, T defaultValue = default(T))
        {
            try
            {
                Logger.LogDebug($"Starting operation: {operationName}", "SafeExecute");
                var result = operation();
                Logger.LogDebug($"Completed operation: {operationName}", "SafeExecute");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in operation: {operationName}", "SafeExecute", ex);
                return defaultValue;
            }
        }

        public static bool SafeExecute(Action operation, string operationName, string userMessage = null)
        {
            try
            {
                Logger.LogDebug($"Starting operation: {operationName}", "SafeExecute");
                operation();
                Logger.LogDebug($"Completed operation: {operationName}", "SafeExecute");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in operation: {operationName}", "SafeExecute", ex);
                
                if (!string.IsNullOrEmpty(userMessage))
                {
                    MessageBox.Show(userMessage, "Blueshot - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                return false;
            }
        }
    }
}
