using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Win32.UI.Notifications;

namespace DrLab.Desktop.Services
{
    public class NotificationService
    {
        private static readonly Lazy<NotificationService> _instance = new(() => new NotificationService());
        public static NotificationService Instance => _instance.Value;

        private static Window? _mainWindow;
        private readonly DispatcherQueue _dispatcher;
        private readonly Queue<NotificationItem> _pendingNotifications = new();

        public event EventHandler<NotificationEventArgs>? NotificationReceived;

        private NotificationService()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public static void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(message, title ?? "Success", NotificationType.Success);
        }

        public async Task ShowErrorAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(message, title ?? "Error", NotificationType.Error);
        }

        public async Task ShowInfoAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(message, title ?? "Information", NotificationType.Info);
        }

        public async Task ShowWarningAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(message, title ?? "Warning", NotificationType.Warning);
        }

        public async Task ShowNotificationAsync(string message, string title, NotificationType type)
        {
            var notification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            // Dispatch to UI thread if needed
            if (_dispatcher.HasThreadAccess)
            {
                await DisplayNotificationAsync(notification);
            }
            else
            {
                _dispatcher.TryEnqueue(async () => await DisplayNotificationAsync(notification));
            }

            // Raise event
            NotificationReceived?.Invoke(this, new NotificationEventArgs(notification));
        }

        private async Task DisplayNotificationAsync(NotificationItem notification)
        {
            try
            {
                // Show in-app notification if main window is available
                if (_mainWindow != null)
                {
                    await ShowInAppNotificationAsync(notification);
                }

                // Show Windows toast notification
                ShowToastNotification(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to display notification: {ex.Message}");
                // Fallback to queue for later display
                _pendingNotifications.Enqueue(notification);
            }
        }

        private async Task ShowInAppNotificationAsync(NotificationItem notification)
        {
            if (_mainWindow?.Content is not FrameworkElement content)
                return;

            try
            {
                // Create info bar for in-app notification
                var infoBar = new InfoBar
                {
                    Title = notification.Title,
                    Message = notification.Message,
                    Severity = notification.Type switch
                    {
                        NotificationType.Success => InfoBarSeverity.Success,
                        NotificationType.Error => InfoBarSeverity.Error,
                        NotificationType.Warning => InfoBarSeverity.Warning,
                        _ => InfoBarSeverity.Informational
                    },
                    IsOpen = true,
                    Margin = new Thickness(20, 20, 20, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // Auto-close after 5 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, e) =>
                {
                    infoBar.IsOpen = false;
                    timer.Stop();
                };
                timer.Start();

                // Find a suitable parent to add the InfoBar
                if (content is Grid grid)
                {
                    grid.Children.Add(infoBar);
                    Grid.SetZIndex(infoBar, 1000); // Ensure it's on top
                }
                else if (content is Panel panel)
                {
                    panel.Children.Add(infoBar);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show in-app notification: {ex.Message}");
            }
        }

        private void ShowToastNotification(NotificationItem notification)
        {
            try
            {
                // Create toast notification
                var toastContent = new ToastContentBuilder()
                    .AddText(notification.Title)
                    .AddText(notification.Message)
                    .SetToastScenario(ToastScenario.Default);

                // Add icon based on type
                var iconPath = notification.Type switch
                {
                    NotificationType.Success => "ms-appx:///Assets/success.png",
                    NotificationType.Error => "ms-appx:///Assets/error.png",
                    NotificationType.Warning => "ms-appx:///Assets/warning.png",
                    _ => "ms-appx:///Assets/info.png"
                };

                var toast = new ToastNotification(toastContent.GetXml())
                {
                    Tag = notification.Id,
                    ExpirationTime = DateTime.Now.AddMinutes(5)
                };

                ToastNotificationManager.CreateToastNotifier("DrLab.Desktop").Show(toast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show toast notification: {ex.Message}");
            }
        }

        public void ShowPendingNotifications()
        {
            while (_pendingNotifications.Count > 0)
            {
                var notification = _pendingNotifications.Dequeue();
                _ = DisplayNotificationAsync(notification);
            }
        }

        public async Task ShowMessageNotificationAsync(string senderName, string message, string conversationId)
        {
            var title = $"New message from {senderName}";
            var truncatedMessage = message.Length > 100 ? message.Substring(0, 100) + "..." : message;

            // Create actionable toast notification for messages
            try
            {
                var toastContent = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(truncatedMessage)
                    .AddButton(new ToastButton()
                        .SetContent("Reply")
                        .AddArgument("action", "reply")
                        .AddArgument("conversationId", conversationId))
                    .AddButton(new ToastButton()
                        .SetContent("View")
                        .AddArgument("action", "view")
                        .AddArgument("conversationId", conversationId))
                    .SetToastScenario(ToastScenario.IncomingCall);

                var toast = new ToastNotification(toastContent.GetXml())
                {
                    Tag = $"message_{conversationId}_{Guid.NewGuid()}",
                    ExpirationTime = DateTime.Now.AddHours(1)
                };

                ToastNotificationManager.CreateToastNotifier("DrLab.Desktop").Show(toast);
            }
            catch (Exception ex)
            {
                // Fallback to regular notification
                await ShowInfoAsync(truncatedMessage, title);
                System.Diagnostics.Debug.WriteLine($"Failed to show message toast notification: {ex.Message}");
            }
        }

        public void ClearNotifications()
        {
            try
            {
                ToastNotificationManager.CreateToastNotifier("DrLab.Desktop").RemoveFromSchedule();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear notifications: {ex.Message}");
            }
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class NotificationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string? ActionData { get; set; }
    }

    public class NotificationEventArgs : EventArgs
    {
        public NotificationItem Notification { get; }

        public NotificationEventArgs(NotificationItem notification)
        {
            Notification = notification;
        }
    }
}