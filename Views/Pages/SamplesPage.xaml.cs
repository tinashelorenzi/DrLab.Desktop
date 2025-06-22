using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DrLab.Desktop.Views.Pages
{
    public sealed partial class SamplesPage : Page
    {
        public SamplesPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadSamples();
        }

        private void LoadSamples()
        {
            // TODO: Load samples from API
            // For now using dummy data as shown in XAML
        }

        private void NewSampleButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open new sample dialog or navigate to new sample page
            // For now just show a message
            ShowMessage("New sample functionality will be implemented here");
        }

        private async void ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Information",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}