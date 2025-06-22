using DrLab.Desktop.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace DrLab.Desktop.Views.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private readonly UserSessionManager _sessionManager;

        public DashboardPage()
        {
            this.InitializeComponent();
            _sessionManager = UserSessionManager.Instance;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            // Set personalized welcome message
            var user = _sessionManager.GetCurrentUser();
            var firstName = user?.user?.first_name ?? "User";
            var timeOfDay = GetTimeOfDay();

            WelcomeText.Text = $"Good {timeOfDay}, {firstName}! Here's what's happening in the lab today.";

            // TODO: Load real dashboard data from API
            // For now using dummy data as requested
        }

        private string GetTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 12 => "morning",
                >= 12 and < 17 => "afternoon",
                _ => "evening"
            };
        }
    }
}