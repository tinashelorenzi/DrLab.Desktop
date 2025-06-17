using System;
using System.Collections.Generic;
using System.Linq;
using DrLab.Desktop.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace DrLab.Desktop.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly UserSessionManager _sessionManager;
        private string _currentSection = "Dashboard";

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Laboratory Management System";

            _sessionManager = UserSessionManager.Instance;

            // Maximize window on startup
            MaximizeWindow();

            // Load user information
            LoadUserInformation();

            // Show dashboard by default
            ShowSection("Dashboard");
        }

        private void MaximizeWindow()
        {
            // Get the window handle
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Maximize the window
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }

        private void LoadUserInformation()
        {
            var currentUser = _sessionManager.GetCurrentUser();

            if (currentUser != null)
            {
                // Update user display
                UserDisplayName.Text = currentUser.user?.full_name ?? currentUser.user?.username ?? "Unknown User";
                UserRole.Text = $"({FormatJobTitle(currentUser.job_title)})";

                // You could also show department, employee ID, etc.
            }
            else
            {
                // Fallback for testing
                UserDisplayName.Text = "Test User";
                UserRole.Text = "(Administrator)";
            }
        }

        private string FormatJobTitle(string jobTitle)
        {
            if (string.IsNullOrEmpty(jobTitle)) return "User";

            // Convert snake_case to Title Case
            return jobTitle.Replace("_", " ")
                          .Split(' ')
                          .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower())
                          .Aggregate((a, b) => a + " " + b);
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string section)
            {
                ShowSection(section);
                UpdateNavigationUI(button);
            }
        }

        private void ShowSection(string sectionName)
        {
            _currentSection = sectionName;

            // Update page title and subtitle
            switch (sectionName)
            {
                case "Dashboard":
                    PageTitle.Text = "Dashboard";
                    PageSubtitle.Text = "Laboratory overview and key metrics";
                    DashboardContent.Visibility = Visibility.Visible;
                    OtherContent.Visibility = Visibility.Collapsed;
                    break;

                case "SampleManagement":
                    PageTitle.Text = "Sample Management";
                    PageSubtitle.Text = "Manage laboratory samples and tracking";
                    ShowPlaceholderSection("Sample Management", "Manage sample registration, tracking, and processing");
                    break;

                case "TestingQueue":
                    PageTitle.Text = "Testing Queue";
                    PageSubtitle.Text = "View and manage pending tests";
                    ShowPlaceholderSection("Testing Queue", "Monitor and prioritize laboratory testing workflow");
                    break;

                case "ResultsEntry":
                    PageTitle.Text = "Results Entry";
                    PageSubtitle.Text = "Enter and validate test results";
                    ShowPlaceholderSection("Results Entry", "Input and verify laboratory test results");
                    break;

                case "QualityControl":
                    PageTitle.Text = "Quality Control";
                    PageSubtitle.Text = "Monitor QC samples and standards";
                    ShowPlaceholderSection("Quality Control", "Manage quality control procedures and monitoring");
                    break;

                case "Reports":
                    PageTitle.Text = "Reports";
                    PageSubtitle.Text = "Generate and manage laboratory reports";
                    ShowPlaceholderSection("Reports", "Create, review, and distribute test reports");
                    break;

                case "ClientManagement":
                    PageTitle.Text = "Client Management";
                    PageSubtitle.Text = "Manage client information and relationships";
                    ShowPlaceholderSection("Client Management", "Handle client registration, contacts, and projects");
                    break;

                case "Quotations":
                    PageTitle.Text = "Quotations";
                    PageSubtitle.Text = "Create and manage price quotations";
                    ShowPlaceholderSection("Quotations", "Generate quotes and pricing for laboratory services");
                    break;

                case "Analytics":
                    PageTitle.Text = "Analytics";
                    PageSubtitle.Text = "Laboratory performance and insights";
                    ShowPlaceholderSection("Analytics", "View trends, statistics, and performance metrics");
                    break;

                case "Settings":
                    PageTitle.Text = "Settings";
                    PageSubtitle.Text = "System configuration and preferences";
                    ShowPlaceholderSection("Settings", "Configure system settings and user preferences");
                    break;

                default:
                    PageTitle.Text = "Dashboard";
                    PageSubtitle.Text = "Laboratory overview and key metrics";
                    DashboardContent.Visibility = Visibility.Visible;
                    OtherContent.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void ShowPlaceholderSection(string title, string description)
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            OtherContent.Visibility = Visibility.Visible;
            PlaceholderTitle.Text = title;
            PlaceholderDescription.Text = description;
        }

        private void UpdateNavigationUI(Button selectedButton)
        {
            // Reset all navigation buttons to default state
            var navButtons = new[] {
                DashboardBtn, SampleMgmtBtn, TestingQueueBtn, ResultsEntryBtn,
                QualityControlBtn, ReportsBtn, ClientMgmtBtn, QuotationsBtn,
                AnalyticsBtn, SettingsBtn
            };

            foreach (var btn in navButtons)
            {
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 173, 181, 189)); // #FFADB5BD
            }

            // Highlight selected button
            selectedButton.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 59, 130, 246)); // #FF3B82F6
            selectedButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show confirmation dialog
                var dialog = new ContentDialog()
                {
                    Title = "Confirm Logout",
                    Content = "Are you sure you want to log out?",
                    PrimaryButtonText = "Yes, Logout",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Clear session data
                    _sessionManager.ClearSession();

                    // Close this window
                    this.Close();

                    // Show login window again
                    var loginWindow = new LoginWindow();
                    loginWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");

                // Even if there's an error, still logout locally
                _sessionManager.ClearSession();
                this.Close();

                var loginWindow = new LoginWindow();
                loginWindow.Activate();
            }
        }

        // Method to refresh dashboard data (can be called periodically)
        public void RefreshDashboard()
        {
            if (_currentSection == "Dashboard")
            {
                // TODO: Refresh dashboard metrics from API
                // For now, just reload user info
                LoadUserInformation();
            }
        }

        // Method to navigate to specific section (can be called from other parts of the app)
        public void NavigateToSection(string sectionName)
        {
            ShowSection(sectionName);

            // Update navigation UI
            var buttonMap = new Dictionary<string, Button>
            {
                ["Dashboard"] = DashboardBtn,
                ["SampleManagement"] = SampleMgmtBtn,
                ["TestingQueue"] = TestingQueueBtn,
                ["ResultsEntry"] = ResultsEntryBtn,
                ["QualityControl"] = QualityControlBtn,
                ["Reports"] = ReportsBtn,
                ["ClientManagement"] = ClientMgmtBtn,
                ["Quotations"] = QuotationsBtn,
                ["Analytics"] = AnalyticsBtn,
                ["Settings"] = SettingsBtn
            };

            if (buttonMap.ContainsKey(sectionName))
            {
                UpdateNavigationUI(buttonMap[sectionName]);
            }
        }
    }
}