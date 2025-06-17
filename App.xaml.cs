using Microsoft.UI.Xaml;
using DrLab.Desktop.Views;

namespace DrLab.Desktop
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Show login window instead of main window
            var loginWindow = new LoginWindow();
            loginWindow.Activate();
        }
    }
}