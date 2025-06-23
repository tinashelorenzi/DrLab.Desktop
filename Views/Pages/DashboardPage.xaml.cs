using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DrLab.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace DrLab.Desktop.Views.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private readonly UserSessionManager _sessionManager;
        private readonly NotificationService _notificationService;

        public DashboardPage()
        {
            this.InitializeComponent();
            _sessionManager = UserSessionManager.Instance;
            _notificationService = NotificationService.Instance;

            this.Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Refresh data when navigating to dashboard
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // TODO: Replace with actual API calls to get dashboard data
                await LoadSampleStatisticsAsync();
                await LoadRecentActivityAsync();
                await LoadSystemStatusAsync();
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to load dashboard data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex}");
            }
        }

        private async Task LoadSampleStatisticsAsync()
        {
            try
            {
                // TODO: Replace with actual API calls
                // For now, simulate loading data
                await Task.Delay(100);

                // These would come from your API
                ActiveSamplesCount.Text = "127";
                PendingTestsCount.Text = "43";
                QCAlerts.Text = "2";
                CompletedTodayCount.Text = "18";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load sample statistics: {ex.Message}");
                // Set default values on error
                ActiveSamplesCount.Text = "--";
                PendingTestsCount.Text = "--";
                QCAlerts.Text = "--";
                CompletedTodayCount.Text = "--";
            }
        }

        private async Task LoadRecentActivityAsync()
        {
            try
            {
                // TODO: Replace with actual API call to get recent activity
                await Task.Delay(100);

                // The activity items are currently hardcoded in XAML
                // In a real implementation, you would:
                // 1. Call your API to get recent activities
                // 2. Create a data source/collection for the activity list
                // 3. Bind the ScrollViewer content to that collection
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent activity: {ex.Message}");
            }
        }

        private async Task LoadSystemStatusAsync()
        {
            try
            {
                // TODO: Replace with actual system health checks
                await Task.Delay(100);

                // In a real implementation, you would check:
                // 1. Database connectivity
                // 2. Messaging service status
                // 3. Backup system status
                // 4. Any connected instruments
                // And update the status indicators accordingly
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load system status: {ex.Message}");
            }
        }

        // Event handlers for quick action buttons
        private void AddNewSample_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to sample creation page or show dialog
            _ = _notificationService.ShowInfoAsync("Add New Sample feature coming soon!");
        }

        private void EnterResults_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to results entry page
            _ = _notificationService.ShowInfoAsync("Enter Results feature coming soon!");
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to report generation page
            _ = _notificationService.ShowInfoAsync("Generate Report feature coming soon!");
        }

        private void QCReview_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to QC review page
            _ = _notificationService.ShowInfoAsync("QC Review feature coming soon!");
        }

        // Method to refresh dashboard data (can be called from parent window)
        public async Task RefreshAsync()
        {
            await LoadDashboardDataAsync();
        }

        // Method to handle real-time updates from WebSocket
        public void HandleRealTimeUpdate(string updateType, object data)
        {
            try
            {
                switch (updateType.ToLower())
                {
                    case "sample_status_update":
                        // Update sample counts
                        _ = LoadSampleStatisticsAsync();
                        break;
                    case "qc_alert":
                        // Update QC alert count
                        _ = LoadSampleStatisticsAsync();
                        break;
                    case "test_completed":
                        // Update completed count
                        _ = LoadSampleStatisticsAsync();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle real-time update: {ex.Message}");
            }
        }
    }
}