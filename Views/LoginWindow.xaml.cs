using System;
using System.Threading.Tasks;
using DrLab.Desktop.Services;
using DrLab.Desktop.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using WinRT.Interop;
using Windows.UI;

namespace DrLab.Desktop.Views
{
    public sealed partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserSessionManager _sessionManager;
        private readonly NotificationService _notificationService;
        private bool _isLoggingIn = false;

        public LoginWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Login";

            // Get services from dependency injection
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService))!;
            _sessionManager = UserSessionManager.Instance;
            _notificationService = NotificationService.Instance;

            // Configure window
            ConfigureWindow();

            // Start entrance animations
            this.Activated += LoginWindow_Activated;

            // Handle Enter key for login
            this.Content.KeyDown += LoginWindow_KeyDown;

            // Focus username box when loaded
            this.Loaded += (s, e) => UsernameTextBox.Focus(FocusState.Programmatic);

            // Check connection status
            CheckConnectionStatus();
        }

        private void ConfigureWindow()
        {
            try
            {
                // Get the window handle
                var hWnd = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                // Set window size and center it
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var centerX = (displayArea.WorkArea.Width - 1200) / 2;
                var centerY = (displayArea.WorkArea.Height - 800) / 2;
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(centerX, centerY, 1200, 800));

                // Set window properties
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to configure window: {ex.Message}");
            }
        }

        private void LoginWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                this.Activated -= LoginWindow_Activated;
                StartEntranceAnimations();
            }
        }

        private void LoginWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !_isLoggingIn)
            {
                e.Handled = true;
                _ = LoginAsync();
            }
        }

        private void StartEntranceAnimations()
        {
            try
            {
                // Animate login container entrance
                var containerTransform = new CompositeTransform
                {
                    TranslateY = 50,
                    ScaleX = 0.95,
                    ScaleY = 0.95
                };
                LoginContainer.RenderTransform = containerTransform;
                LoginContainer.Opacity = 0;

                // Create animation
                var storyboard = new Storyboard();

                // Opacity animation
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacityAnimation, LoginContainer);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

                // Transform animation
                var translateAnimation = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(translateAnimation, containerTransform);
                Storyboard.SetTargetProperty(translateAnimation, "TranslateY");

                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleXAnimation, containerTransform);
                Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleYAnimation, containerTransform);
                Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");

                storyboard.Children.Add(opacityAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // If animations fail, just show the container
                LoginContainer.Opacity = 1;
                System.Diagnostics.Debug.WriteLine($"Animation failed: {ex.Message}");
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            if (_isLoggingIn) return;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            try
            {
                _isLoggingIn = true;
                SetLoginState(true);

                var response = await _apiService.LoginAsync(username, password);

                if (response.Success && response.Data != null)
                {
                    // Save session
                    _sessionManager.SetSession(
                        response.Data.User,
                        response.Data.Token,
                        response.Data.RefreshToken
                    );

                    // Show success briefly
                    await _notificationService.ShowSuccessAsync("Login successful!");

                    // Navigate to main window
                    var mainWindow = new MainWindow();
                    mainWindow.Activate();
                    this.Close();
                }
                else
                {
                    ShowError(response.Message ?? "Login failed. Please check your credentials.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            }
            finally
            {
                _isLoggingIn = false;
                SetLoginState(false);
            }
        }

        private void SetLoginState(bool isLogging)
        {
            LoginButton.IsEnabled = !isLogging;
            UsernameTextBox.IsEnabled = !isLogging;
            PasswordBox.IsEnabled = !isLogging;
            LoadingPanel.Visibility = isLogging ? Visibility.Visible : Visibility.Collapsed;

            if (isLogging)
            {
                ErrorInfoBar.IsOpen = false;
            }
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }

        private async void CheckConnectionStatus()
        {
            try
            {
                // Simple connectivity check - you might want to ping your API endpoint
                ConnectionStatusText.Text = "Connected";
                ConnectionIndicator.Fill = new SolidColorBrush(Colors.Green);

                // You could add an actual API health check here
                // var healthCheck = await _apiService.CheckHealthAsync();
            }
            catch
            {
                ConnectionStatusText.Text = "Connection Error";
                ConnectionIndicator.Fill = new SolidColorBrush(Colors.Red);
            }
        }
    }
}