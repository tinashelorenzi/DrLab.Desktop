using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DrLab.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace DrLab.Desktop.Views.Pages
{
    public sealed partial class SamplesPage : Page
    {
        private readonly UserSessionManager _sessionManager;
        private readonly NotificationService _notificationService;
        private readonly ApiService _apiService;

        public SamplesPage()
        {
            this.InitializeComponent();
            _sessionManager = UserSessionManager.Instance;
            _notificationService = NotificationService.Instance;
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService))!;

            this.Loaded += SamplesPage_Loaded;

            // Wire up search functionality
            SearchBox.TextChanged += SearchBox_TextChanged;
            StatusFilterComboBox.SelectionChanged += StatusFilter_SelectionChanged;
        }

        private async void SamplesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSamplesAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Refresh data when navigating to samples page
            _ = LoadSamplesAsync();
        }

        private async Task LoadSamplesAsync()
        {
            try
            {
                ShowLoadingState();

                // TODO: Replace with actual API call to get samples
                // For now, simulate loading
                await Task.Delay(1000);

                // In a real implementation:
                // var response = await _apiService.GetSamplesAsync();
                // if (response.Success && response.Data != null)
                // {
                //     PopulateSamplesList(response.Data);
                // }

                HideLoadingState();

                // For now, the sample data is hardcoded in XAML
                // In a real implementation, you would clear SamplesContainer
                // and dynamically create sample rows based on API data
            }
            catch (Exception ex)
            {
                HideLoadingState();
                await _notificationService.ShowErrorAsync($"Failed to load samples: {ex.Message}");
                ShowEmptyState();
                System.Diagnostics.Debug.WriteLine($"Samples load error: {ex}");
            }
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            // Hide actual sample data while loading
            foreach (var child in SamplesContainer.Children)
            {
                if (child is Border border && border != LoadingPanel && border != EmptyStatePanel)
                {
                    border.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void HideLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            // Show actual sample data
            foreach (var child in SamplesContainer.Children)
            {
                if (child is Border border && border != LoadingPanel && border != EmptyStatePanel)
                {
                    border.Visibility = Visibility.Visible;
                }
            }
        }

        private void ShowEmptyState()
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            // Hide sample data
            foreach (var child in SamplesContainer.Children)
            {
                if (child is Border border && border != EmptyStatePanel && border != LoadingPanel)
                {
                    border.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce search to avoid too many API calls
            await Task.Delay(300);

            var searchText = SearchBox.Text.Trim();
            if (searchText != SearchBox.Text.Trim()) return; // User is still typing

            await FilterSamplesAsync(searchText);
        }

        private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedStatus = selectedItem.Content.ToString();
                await FilterSamplesAsync(SearchBox.Text.Trim(), selectedStatus);
            }
        }

        private async Task FilterSamplesAsync(string searchText = "", string statusFilter = "")
        {
            try
            {
                // TODO: Implement filtering logic
                // In a real implementation, you would:
                // 1. Call API with search and filter parameters
                // 2. Update the samples list based on results

                await Task.Delay(100); // Simulate API call

                // For now, just show a notification
                if (!string.IsNullOrEmpty(searchText) || (statusFilter != "All Statuses" && !string.IsNullOrEmpty(statusFilter)))
                {
                    System.Diagnostics.Debug.WriteLine($"Filtering samples: search='{searchText}', status='{statusFilter}'");
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to filter samples: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Filter error: {ex}");
            }
        }

        private async void AddSampleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Show add sample dialog or navigate to add sample page
                await _notificationService.ShowInfoAsync("Add Sample dialog will be implemented soon!");

                // In a real implementation:
                // var dialog = new AddSampleDialog();
                // var result = await dialog.ShowAsync();
                // if (result == ContentDialogResult.Primary)
                // {
                //     await LoadSamplesAsync(); // Refresh the list
                // }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to open add sample dialog: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Add sample error: {ex}");
            }
        }

        // Event handlers for sample action buttons
        private async void ViewSample_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Get sample ID from button context and show sample details
            await _notificationService.ShowInfoAsync("View sample details feature coming soon!");
        }

        private async void EditSample_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Get sample ID from button context and show edit sample dialog
            await _notificationService.ShowInfoAsync("Edit sample feature coming soon!");
        }

        private async void PrintBarcode_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Get sample ID from button context and print barcode
            await _notificationService.ShowInfoAsync("Print barcode feature coming soon!");
        }

        // Method to refresh samples data (can be called from parent window)
        public async Task RefreshAsync()
        {
            await LoadSamplesAsync();
        }

        // Method to handle real-time updates from WebSocket
        public void HandleRealTimeUpdate(string updateType, object data)
        {
            try
            {
                switch (updateType.ToLower())
                {
                    case "sample_status_update":
                        // Refresh the samples list to show updated status
                        _ = LoadSamplesAsync();
                        break;
                    case "new_sample":
                        // Add new sample to the list or refresh entire list
                        _ = LoadSamplesAsync();
                        break;
                    case "sample_deleted":
                        // Remove sample from list or refresh entire list
                        _ = LoadSamplesAsync();
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

        // Helper method to create sample row programmatically (for dynamic data)
        private Border CreateSampleRow(dynamic sample)
        {
            // TODO: Implement dynamic sample row creation
            // This would be used when you have actual sample data from API
            // Return a Border with Grid containing all sample information

            var border = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 16)
            };

            // Add Grid with sample data here...

            return border;
        }
    }
}