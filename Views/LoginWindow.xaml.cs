using System; // Add this namespace for Exception
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing; // Add this namespace for AppWindowPresenterKind

namespace DrLab.Desktop.Views
{
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Login";

            // Make window maximized on startup
            this.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide any previous error messages
            StatusTextBlock.Visibility = Visibility.Collapsed;

            // Get username and password
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Basic validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            // Show loading state
            LoginButton.Content = "Signing in...";
            LoginButton.IsEnabled = false;

            try
            {
                // TODO: This is where we'll call your Django API
                // For now, let's simulate a login
                await Task.Delay(1000); // Simulate network call

                // Temporary mock login - replace with real API call
                if (username == "admin" && password == "admin")
                {
                    // Success! Close login window and open main window
                    ShowMainWindow();
                }
                else
                {
                    ShowError("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
            }
            finally
            {
                // Reset button state
                LoginButton.Content = "Sign In";
                LoginButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowMainWindow()
        {
            // TODO: Create and show main window
            // For now, just show a message
            ShowError("Login successful! (Main window not implemented yet)");
        }
    }
}