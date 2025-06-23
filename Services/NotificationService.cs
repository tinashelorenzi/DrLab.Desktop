using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml;

namespace DrLab.Desktop.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        private static readonly object _lock = new object();
        private static Window? _mainWindow;

        // Notification types for easy templating
        public enum NotificationType
        {
            Security,
            Success,
            Warning,
            Error,
            Info,
            Message,
            QualityControl,
            SampleAlert
        }

        private NotificationService()
        {
            // Initialize notification system
            EnsureNotificationPermissions();
        }

        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new NotificationService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Set the main window reference for notification click handling
        /// </summary>
        public static void SetMainWindow(Window mainWindow)
        {
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// Show security login notification
        /// </summary>
        public void ShowLoginNotification(string username, string department, DateTime loginTime)
        {
            var title = "🔐 DrLab LIMS - Secure Login";
            var message = $"Welcome back, {username}!\n" +
                         $"Department: {department}\n" +
                         $"Login: {loginTime:yyyy-MM-dd HH:mm:ss}";

            ShowNotification(title, message, NotificationType.Security);
        }

        /// <summary>
        /// Show sample related notifications
        /// </summary>
        public void ShowSampleNotification(string sampleId, string status, string message)
        {
            var title = $"🧪 Sample Update - {sampleId}";
            var fullMessage = $"Status: {status}\n{message}";

            ShowNotification(title, fullMessage, NotificationType.SampleAlert);
        }

        /// <summary>
        /// Show QC notifications
        /// </summary>
        public void ShowQCNotification(string batchId, string issue, bool isUrgent = false)
        {
            var title = isUrgent ? "🚨 URGENT QC Issue" : "⚠️ QC Alert";
            var message = $"Batch: {batchId}\nIssue: {issue}";

            ShowNotification(title, message, NotificationType.QualityControl);
        }

        /// <summary>
        /// Show messaging notifications for future messaging system
        /// </summary>
        public void ShowMessageNotification(string sender, string preview, string conversationId)
        {
            var title = $"💬 New Message from {sender}";
            var message = preview.Length > 50 ? preview.Substring(0, 50) + "..." : preview;

            ShowNotification(title, message, NotificationType.Message);
        }

        /// <summary>
        /// Generic success notification
        /// </summary>
        public void ShowSuccess(string title, string message)
        {
            ShowNotification($"✅ {title}", message, NotificationType.Success);
        }

        /// <summary>
        /// Generic error notification  
        /// </summary>
        public void ShowError(string title, string message)
        {
            ShowNotification($"❌ {title}", message, NotificationType.Error);
        }

        /// <summary>
        /// Generic warning notification
        /// </summary>
        public void ShowWarning(string title, string message)
        {
            ShowNotification($"⚠️ {title}", message, NotificationType.Warning);
        }

        /// <summary>
        /// Core notification method that handles all notification display
        /// </summary>
        private void ShowNotification(string title, string message, NotificationType type)
        {
            try
            {
                // Create the toast content
                var toastXml = CreateSimpleToastContent(title, message);

                // Create the toast notification
                var toast = new ToastNotification(toastXml);

                // Set notification properties based on type
                ConfigureToastProperties(toast, type);

                // Show the notification
                ToastNotificationManager.CreateToastNotifier("DrLab.Desktop").Show(toast);

                // Log for debugging
                System.Diagnostics.Debug.WriteLine($"Notification shown: {type} - {title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Create simple toast XML content
        /// </summary>
        private XmlDocument CreateSimpleToastContent(string title, string message)
        {
            // Use simple text-only template for better compatibility
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // Set the text content
            var textNodes = toastXml.GetElementsByTagName("text");
            textNodes[0].AppendChild(toastXml.CreateTextNode(title));
            textNodes[1].AppendChild(toastXml.CreateTextNode(message));

            return toastXml;
        }

        /// <summary>
        /// Configure toast properties such as duration and sound
        /// </summary>
        private void ConfigureToastProperties(ToastNotification toast, NotificationType type)
        {
            // Set duration based on importance
            toast.ExpirationTime = type switch
            {
                NotificationType.Security => DateTimeOffset.Now.AddMinutes(10),
                NotificationType.QualityControl => DateTimeOffset.Now.AddMinutes(30),
                NotificationType.Error => DateTimeOffset.Now.AddMinutes(15),
                _ => DateTimeOffset.Now.AddMinutes(5)
            };

            // Set priority for urgent notifications
            if (type == NotificationType.QualityControl || type == NotificationType.Error)
            {
                toast.Priority = ToastNotificationPriority.High;
            }

            // Handle notification activation (when user clicks)
            toast.Activated += (sender, args) =>
            {
                HandleNotificationClick(type);
            };
        }

        /// <summary>
        /// Handle notification click events
        /// </summary>
        private void HandleNotificationClick(NotificationType type)
        {
            try
            {
                // Get the main window for dispatcher access
                var mainWindow = _mainWindow;
                if (mainWindow != null)
                {
                    mainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            // Handle different notification types
                            switch (type)
                            {
                                case NotificationType.Message:
                                    System.Diagnostics.Debug.WriteLine("Message notification clicked - navigate to messaging");
                                    // TODO: Navigate to messaging page when implemented
                                    break;
                                case NotificationType.QualityControl:
                                    System.Diagnostics.Debug.WriteLine("QC notification clicked - navigate to QC page");
                                    // TODO: Navigate to QC page when implemented
                                    break;
                                case NotificationType.Security:
                                    System.Diagnostics.Debug.WriteLine("Security notification clicked");
                                    // Could open security/audit log
                                    break;
                                case NotificationType.SampleAlert:
                                    System.Diagnostics.Debug.WriteLine("Sample notification clicked - navigate to samples");
                                    // TODO: Navigate to samples page
                                    break;
                                default:
                                    System.Diagnostics.Debug.WriteLine($"{type} notification clicked");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error handling notification click: {ex.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{type} notification clicked (no main window reference)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dispatching notification click: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure notification permissions are granted
        /// </summary>
        private void EnsureNotificationPermissions()
        {
            try
            {
                // Note: For WinUI 3 apps, toast notifications should work by default
                // If you need explicit permission handling, implement it here
                System.Diagnostics.Debug.WriteLine("Notification system initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize notifications: {ex.Message}");
            }
        }
    }
}