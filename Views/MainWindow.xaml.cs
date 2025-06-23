using DrLab.Desktop.Services;
using DrLab.Desktop.Views.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DrLab.Desktop.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly UserSessionManager _sessionManager;
        private readonly NotificationService _notificationService;
        private readonly ApiService _apiService;
        private readonly Dictionary<string, Type> _pageTypes;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Laboratory Information Management System";

            _sessionManager = UserSessionManager.Instance;
            _notificationService = NotificationService.Instance;
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService))!;

            // Set this window as the main window for notifications
            NotificationService.SetMainWindow(this);

            // Initialize page mappings - only include pages we've created
            _pageTypes = new Dictionary<string, Type>
            {
                { "Dashboard", typeof(DashboardPage) },
                { "Samples", typeof(SamplesPage) },
                { "Messaging", typeof(MessagingPage) }
                // TODO: Add other pages as we create them:
                // { "Results", typeof(ResultsPage) },
                // { "Clients", typeof(ClientsPage) },
                // { "Finance", typeof(FinancePage) },
                // { "QualityControl", typeof(QualityControlPage) },
                // { "Reviews", typeof(ReviewsPage) },
                // { "Reports", typeof(ReportsPage) },
                // { "Settings", typeof(SettingsPage) }
            };

            LoadUserInfo();
            NavigateToDefaultPage();

            this.Closed += MainWindow_Closed;
        }

        private void LoadUserInfo()
        {
            // Load user information from session
            var userDisplayName = _sessionManager.GetUserDisplayName();
            var userDepartment = _sessionManager.GetUserDepartment();

            UserNameText.Text = userDisplayName ?? "Unknown User";
            UserDepartmentText.Text = userDepartment ?? "Unknown Department";

            // Set user initials
            UserInitials.Text = GetInitials(userDisplayName ?? "User");
        }

        private string GetInitials(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "U";

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length > 0)
            {
                return parts[0][0].ToString().ToUpper();
            }
            return "U";
        }

        private void NavigateToDefaultPage()
        {
            // Navigate to dashboard by default
            ContentFrame.Navigate(typeof(DashboardPage));
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0]; // Dashboard
            UpdatePageTitle("Dashboard", "Overview and key metrics");
        }

        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString() ?? "";

                if (_pageTypes.ContainsKey(tag))
                {
                    ContentFrame.Navigate(_pageTypes[tag]);
                    UpdatePageTitle(selectedItem.Content?.ToString() ?? tag, GetPageSubtitle(tag));
                }
                else
                {
                    // Page not implemented yet
                    _ = _notificationService.ShowInfoAsync($"{selectedItem.Content} page is coming soon!");

                    // Revert selection to current page
                    if (ContentFrame.Content != null)
                    {
                        var currentPageType = ContentFrame.Content.GetType();
                        var currentTag = GetTagForPageType(currentPageType);
                        if (!string.IsNullOrEmpty(currentTag))
                        {
                            foreach (var item in MainNavigationView.MenuItems)
                            {
                                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == currentTag)
                                {
                                    MainNavigationView.SelectedItem = navItem;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdatePageTitle(string title, string subtitle = "")
        {
            PageTitleText.Text = title;
            PageSubtitleText.Text = string.IsNullOrEmpty(subtitle) ? "" : $" - {subtitle}";
        }

        private string GetPageSubtitle(string tag)
        {
            return tag switch
            {
                "Dashboard" => "Overview and key metrics",
                "Samples" => "Track and manage laboratory samples",
                "Results" => "Enter and validate test results",
                "Clients" => "Manage client information and projects",
                "Finance" => "Billing and financial management",
                "QualityControl" => "Quality control testing and validation",
                "Reviews" => "Result validation and approval",
                "Reports" => "Generate and manage reports",
                "Messaging" => "Secure laboratory communications",
                "Settings" => "System configuration and preferences",
                _ => ""
            };
        }

        private string? GetTagForPageType(Type pageType)
        {
            foreach (var kvp in _pageTypes)
            {
                if (kvp.Value == pageType)
                    return kvp.Key;
            }
            return null;
        }

        // User profile menu handlers
        private void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            UserProfileFlyout.ShowAt(UserProfileButton);
        }

        private async void ProfileSettings_Click(object sender, RoutedEventArgs e)
        {
            UserProfileFlyout.Hide();
            await _notificationService.ShowInfoAsync("Profile settings feature coming soon!");
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            UserProfileFlyout.Hide();
            await _notificationService.ShowInfoAsync("Change password feature coming soon!");
        }

        private async void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            UserProfileFlyout.Hide();
            await _notificationService.ShowInfoAsync("System information feature coming soon!");
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            UserProfileFlyout.Hide();
            await SignOutAsync();
        }

        private async Task SignOutAsync()
        {
            try
            {
                // Show confirmation dialog
                var dialog = new ContentDialog
                {
                    Title = "Sign Out",
                    Content = "Are you sure you want to sign out?",
                    PrimaryButtonText = "Sign Out",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Logout from API
                    await _apiService.LogoutAsync();

                    // Clear session
                    _sessionManager.ClearSession();

                    // Show login window
                    var loginWindow = new LoginWindow();
                    loginWindow.Activate();

                    // Close this window
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to sign out: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Sign out error: {ex}");
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // Cleanup resources
            try
            {
                // If this is the last window, clean up the session
                // In a real app, you might want to save the session for next time

                System.Diagnostics.Debug.WriteLine("Main window closing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window cleanup: {ex.Message}");
            }
        }

        // Method to handle real-time updates (can be called from WebSocket service)
        public void HandleRealTimeUpdate(string updateType, object data)
        {
            try
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    // Forward updates to current page if it supports real-time updates
                    if (ContentFrame.Content is DashboardPage dashboardPage)
                    {
                        dashboardPage.HandleRealTimeUpdate(updateType, data);
                    }
                    else if (ContentFrame.Content is SamplesPage samplesPage)
                    {
                        samplesPage.HandleRealTimeUpdate(updateType, data);
                    }
                    else if (ContentFrame.Content is MessagingPage messagingPage)
                    {
                        messagingPage.HandleRealTimeUpdate(updateType, data);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle real-time update: {ex.Message}");
            }
        }

        // Method to refresh current page data
        public async Task RefreshCurrentPageAsync()
        {
            try
            {
                if (ContentFrame.Content is DashboardPage dashboardPage)
                {
                    await dashboardPage.RefreshAsync();
                }
                else if (ContentFrame.Content is SamplesPage samplesPage)
                {
                    await samplesPage.RefreshAsync();
                }
                else if (ContentFrame.Content is MessagingPage messagingPage)
                {
                    await messagingPage.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to refresh page: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Page refresh error: {ex}");
            }
        }

        // Method to navigate to a specific page programmatically
        public void NavigateToPage(string pageTag)
        {
            try
            {
                if (_pageTypes.ContainsKey(pageTag))
                {
                    ContentFrame.Navigate(_pageTypes[pageTag]);

                    // Update navigation selection
                    foreach (var item in MainNavigationView.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageTag)
                        {
                            MainNavigationView.SelectedItem = navItem;
                            UpdatePageTitle(navItem.Content?.ToString() ?? pageTag, GetPageSubtitle(pageTag));
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _notificationService.ShowErrorAsync($"Failed to navigate to page: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
            }
        }
    }
}