using DrLab.Desktop.Services;
using DrLab.Desktop.Views.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace DrLab.Desktop.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly UserSessionManager _sessionManager;
        private readonly Dictionary<string, Type> _pageTypes;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Laboratory Information Management System";

            _sessionManager = UserSessionManager.Instance;

            // Set this window as the main window for notifications
            NotificationService.SetMainWindow(this);

            // Initialize page mappings - only include pages we've created
            _pageTypes = new Dictionary<string, Type>
            {
                { "Dashboard", typeof(DashboardPage) },
                { "Samples", typeof(SamplesPage) }
                // TODO: Add other pages as we create them:
                // { "Results", typeof(ResultsPage) },
                // { "Clients", typeof(ClientsPage) },
                // { "Finance", typeof(FinancePage) },
                // { "QualityControl", typeof(QualityControlPage) },
                // { "Reviews", typeof(ReviewsPage) },
                // { "Reports", typeof(ReportsPage) },
                // { "Messaging", typeof(MessagingPage) },
                // { "Settings", typeof(SettingsPage) }
            };

            LoadUserInfo();
            NavigateToDefaultPage();
        }

        private void LoadUserInfo()
        {
            // Load user information from session
            var userDisplayName = _sessionManager.GetUserDisplayName();
            var userDepartment = _sessionManager.GetUserDepartment();

            UserNameText.Text = userDisplayName ?? "Unknown User";
            UserDepartmentText.Text = userDepartment ?? "Unknown Department";
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
                    // Show "coming soon" page for unimplemented features
                    ShowComingSoonPage(selectedItem.Content?.ToString() ?? tag);
                }
            }
        }

        private void ShowComingSoonPage(string featureName)
        {
            // Create a temporary "coming soon" page content
            var comingSoonContent = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20
            };

            var icon = new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uE946", // Construction icon
                FontSize = 48,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 165, 0)) // Orange
            };

            var title = new TextBlock
            {
                Text = featureName,
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), // White
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var subtitle = new TextBlock
            {
                Text = "This feature is coming soon...",
                FontSize = 14,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 176, 255, 255)), // Light blue
                HorizontalAlignment = HorizontalAlignment.Center
            };

            comingSoonContent.Children.Add(icon);
            comingSoonContent.Children.Add(title);
            comingSoonContent.Children.Add(subtitle);

            ContentFrame.Content = comingSoonContent;
            UpdatePageTitle(featureName, "Feature in development");
        }

        private void UpdatePageTitle(string title, string subtitle)
        {
            PageTitleText.Text = title;
            PageSubtitleText.Text = subtitle;
        }

        private string GetPageSubtitle(string pageTag)
        {
            return pageTag switch
            {
                "Dashboard" => "Overview and key metrics",
                "Samples" => "Sample management and tracking",
                "Results" => "Test results and data entry",
                "Clients" => "Customer and client management",
                "Finance" => "Billing and financial records",
                "QualityControl" => "QC testing and validation",
                "Reviews" => "Result validation and approval",
                "Reports" => "Generate and manage reports",
                "Messaging" => "Secure laboratory communications",
                "Settings" => "System configuration and preferences",
                _ => "Laboratory Information Management System"
            };
        }

        private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Call logout API
            try
            {
                var apiService = App.ServiceProvider.GetService(typeof(ApiService)) as ApiService;
                if (apiService != null)
                {
                    await apiService.LogoutAsync();
                }
            }
            catch
            {
                // Ignore API logout errors, proceed with local logout
            }

            // Clear the session
            _sessionManager.ClearSession();

            // Close this window
            this.Close();

            // Show login window again
            var loginWindow = new LoginWindow();
            loginWindow.Activate();
        }
    }
}