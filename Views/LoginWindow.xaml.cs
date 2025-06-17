using System;
using System.Net.Http;
using System.Threading.Tasks;
using DrLab.Desktop.Services;
using DrLab.Desktop.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinRT.Interop;

namespace DrLab.Desktop.Views
{
    public sealed partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        public LoginWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Login";

            // Get API service from dependency injection
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService));

            // Make window maximized on startup - WinUI 3 way
            MaximizeWindow();

            // Start animations when window loads
            this.Activated += LoginWindow_Activated;
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

        private void LoginWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            // Only run animations on first activation
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                // Remove the event handler so it only runs once
                this.Activated -= LoginWindow_Activated;

                // Start animations
                AnimateLoginContainer();
                AnimateParticles();
                AnimateLogo();
            }
        }

        private void AnimateLoginContainer()
        {
            // Fade in and slide up animation
            var fadeIn = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation()
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(fadeIn, LoginContainer);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            Storyboard.SetTarget(slideUp, LoginContainer);
            Storyboard.SetTargetProperty(slideUp, "(UIElement.Translation).Y");

            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(slideUp);
            storyboard.Begin();
        }

        private void AnimateLogo()
        {
            // Gentle pulse animation for the logo
            var scaleAnimation = new DoubleAnimation()
            {
                From = 1.0,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new SineEase() { EasingMode = EasingMode.EaseInOut },
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(scaleAnimation, LogoIcon);
            Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");

            var scaleAnimationY = new DoubleAnimation()
            {
                From = 1.0,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new SineEase() { EasingMode = EasingMode.EaseInOut },
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(scaleAnimationY, LogoIcon);
            Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

            // Set render transform
            LogoIcon.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();

            storyboard.Children.Add(scaleAnimation);
            storyboard.Children.Add(scaleAnimationY);
            storyboard.Begin();
        }

        private async void AnimateParticles()
        {
            // Simple floating animation for particles
            var random = new Random();

            while (true)
            {
                foreach (var child in ParticlesCanvas.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        var moveX = random.NextDouble() * 4 - 2; // -2 to 2
                        var moveY = random.NextDouble() * 4 - 2; // -2 to 2

                        var currentX = Canvas.GetLeft(element);
                        var currentY = Canvas.GetTop(element);

                        Canvas.SetLeft(element, currentX + moveX);
                        Canvas.SetTop(element, currentY + moveY);

                        // Keep particles within bounds
                        if (Canvas.GetLeft(element) < 0) Canvas.SetLeft(element, 0);
                        if (Canvas.GetTop(element) < 0) Canvas.SetTop(element, 0);
                        if (Canvas.GetLeft(element) > 800) Canvas.SetLeft(element, 800);
                        if (Canvas.GetTop(element) > 600) Canvas.SetTop(element, 600);
                    }
                }

                await Task.Delay(100); // Update every 100ms
            }
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

            // Show loading state with animation
            AnimateButtonLoading(true);

            try
            {
                // Call the real Django API
                var loginResponse = await _apiService.LoginAsync(username, password);

                if (loginResponse != null)
                {
                    // Success! Show success message briefly, then open main window
                    StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                    ShowError("Login successful! Opening main application...");

                    // Wait a moment to show success message
                    await Task.Delay(1000);

                    // Open main window and close login window
                    ShowMainWindow();
                }
            }
            catch (TimeoutException)
            {
                ShowError("Connection timeout. Please check your internet connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("401") || ex.Message.Contains("Invalid"))
                {
                    ShowError("Invalid username or password.");
                }
                else if (ex.Message.Contains("Connection"))
                {
                    ShowError("Cannot connect to server. Please check if the LIMS server is running.");
                }
                else
                {
                    ShowError($"Login failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"An unexpected error occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            }
            finally
            {
                // Reset button state
                AnimateButtonLoading(false);
            }
        }

        private void AnimateButtonLoading(bool isLoading)
        {
            if (isLoading)
            {
                LoginButton.Content = "Signing in...";
                LoginButton.IsEnabled = false;

                // Add subtle opacity animation
                var opacityAnimation = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.7,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                var storyboard = new Storyboard();
                Storyboard.SetTarget(opacityAnimation, LoginButton);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                storyboard.Children.Add(opacityAnimation);
                storyboard.Begin();
            }
            else
            {
                LoginButton.Content = "Sign In";
                LoginButton.IsEnabled = true;

                // Restore opacity
                var opacityAnimation = new DoubleAnimation()
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                var storyboard = new Storyboard();
                Storyboard.SetTarget(opacityAnimation, LoginButton);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                storyboard.Children.Add(opacityAnimation);
                storyboard.Begin();
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Visibility = Visibility.Visible;

            // Animate error message
            var fadeIn = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, StatusTextBlock);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        private void ShowMainWindow()
        {
            // Create and show main dashboard window
            var mainWindow = new MainWindow();
            mainWindow.Activate();

            // Close this login window
            this.Close();
        }
    }
}

private void AnimateLoginContainer()
{
    // Fade in and slide up animation
    var fadeIn = new DoubleAnimation()
    {
        From = 0,
        To = 1,
        Duration = TimeSpan.FromMilliseconds(800),
        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
    };

    var slideUp = new DoubleAnimation()
    {
        From = 50,
        To = 0,
        Duration = TimeSpan.FromMilliseconds(800),
        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
    };

    var storyboard = new Storyboard();

    Storyboard.SetTarget(fadeIn, LoginContainer);
    Storyboard.SetTargetProperty(fadeIn, "Opacity");

    Storyboard.SetTarget(slideUp, LoginContainer);
    Storyboard.SetTargetProperty(slideUp, "(UIElement.Translation).Y");

    storyboard.Children.Add(fadeIn);
    storyboard.Children.Add(slideUp);
    storyboard.Begin();
}

private void AnimateLogo()
{
    // Gentle pulse animation for the logo
    var scaleAnimation = new DoubleAnimation()
    {
        From = 1.0,
        To = 1.1,
        Duration = TimeSpan.FromSeconds(2),
        EasingFunction = new SineEase() { EasingMode = EasingMode.EaseInOut },
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever
    };

    var storyboard = new Storyboard();
    Storyboard.SetTarget(scaleAnimation, LogoIcon);
    Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");

    var scaleAnimationY = new DoubleAnimation()
    {
        From = 1.0,
        To = 1.1,
        Duration = TimeSpan.FromSeconds(2),
        EasingFunction = new SineEase() { EasingMode = EasingMode.EaseInOut },
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever
    };

    Storyboard.SetTarget(scaleAnimationY, LogoIcon);
    Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

    // Set render transform
    LogoIcon.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();

    storyboard.Children.Add(scaleAnimation);
    storyboard.Children.Add(scaleAnimationY);
    storyboard.Begin();
}

private async void AnimateParticles()
{
    // Simple floating animation for particles
    var random = new Random();

    while (true)
    {
        foreach (var child in ParticlesCanvas.Children)
        {
            if (child is FrameworkElement element)
            {
                var moveX = random.NextDouble() * 4 - 2; // -2 to 2
                var moveY = random.NextDouble() * 4 - 2; // -2 to 2

                var currentX = Canvas.GetLeft(element);
                var currentY = Canvas.GetTop(element);

                Canvas.SetLeft(element, currentX + moveX);
                Canvas.SetTop(element, currentY + moveY);

                // Keep particles within bounds
                if (Canvas.GetLeft(element) < 0) Canvas.SetLeft(element, 0);
                if (Canvas.GetTop(element) < 0) Canvas.SetTop(element, 0);
                if (Canvas.GetLeft(element) > 800) Canvas.SetLeft(element, 800);
                if (Canvas.GetTop(element) > 600) Canvas.SetTop(element, 600);
            }
        }

        await Task.Delay(100); // Update every 100ms
    }
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

    // Show loading state with animation
    AnimateButtonLoading(true);

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
        AnimateButtonLoading(false);
    }
}

private void AnimateButtonLoading(bool isLoading)
{
    if (isLoading)
    {
        LoginButton.Content = "Signing in...";
        LoginButton.IsEnabled = false;

        // Add subtle opacity animation
        var opacityAnimation = new DoubleAnimation()
        {
            From = 1.0,
            To = 0.7,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        var storyboard = new Storyboard();
        Storyboard.SetTarget(opacityAnimation, LoginButton);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        storyboard.Children.Add(opacityAnimation);
        storyboard.Begin();
    }
    else
    {
        LoginButton.Content = "Sign In";
        LoginButton.IsEnabled = true;

        // Restore opacity
        var opacityAnimation = new DoubleAnimation()
        {
            From = 0.7,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        var storyboard = new Storyboard();
        Storyboard.SetTarget(opacityAnimation, LoginButton);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        storyboard.Children.Add(opacityAnimation);
        storyboard.Begin();
    }
}

private void ShowError(string message)
{
    StatusTextBlock.Text = message;
    StatusTextBlock.Visibility = Visibility.Visible;

    // Animate error message
    var fadeIn = new DoubleAnimation()
    {
        From = 0,
        To = 1,
        Duration = TimeSpan.FromMilliseconds(300)
    };

    var storyboard = new Storyboard();
    Storyboard.SetTarget(fadeIn, StatusTextBlock);
    Storyboard.SetTargetProperty(fadeIn, "Opacity");
    storyboard.Children.Add(fadeIn);
    storyboard.Begin();
}

private void ShowMainWindow()
{
    // TODO: Create and show main window
    // For now, just show a success message with animation
    StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);
    ShowError("Login successful! Opening main application...");

    // TODO: After 2 seconds, close this window and open main window
    Task.Delay(2000).ContinueWith(_ =>
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            // This is where we'll open the main window
            ShowError("Main window not implemented yet - but login works!");
        });
    });
}
    }
}