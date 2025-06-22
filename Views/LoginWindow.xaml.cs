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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;
using Windows.UI;

namespace DrLab.Desktop.Views
{
    public sealed partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;
        private bool _isLoggingIn = false;

        public LoginWindow()
        {
            this.InitializeComponent();
            this.Title = "DrLab LIMS - Login";

            // Get API service from dependency injection
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService))!;

            // Configure window
            ConfigureWindow();

            // Start entrance animations
            this.Activated += LoginWindow_Activated;

            // Handle Enter key for login using PreviewKeyDown on the content
            this.Content.PreviewKeyDown += LoginWindow_PreviewKeyDown;
        }

        private void ConfigureWindow()
        {
            // Get the window handle and maximize
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            // Center the window if not maximized
            if (appWindow.Size.Width < 1920)
            {
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var centerX = (displayArea.WorkArea.Width - 1200) / 2;
                var centerY = (displayArea.WorkArea.Height - 800) / 2;
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(centerX, centerY, 1200, 800));
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

        private void LoginWindow_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !_isLoggingIn)
            {
                e.Handled = true;
                _ = LoginAsync();
            }
        }

        private void StartEntranceAnimations()
        {
            // Animate login container entrance
            var containerTransform = new CompositeTransform
            {
                TranslateY = 30,
                ScaleX = 0.95,
                ScaleY = 0.95
            };
            LoginContainer.RenderTransform = containerTransform;
            LoginContainer.Opacity = 0;

            var storyboard = new Storyboard();

            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, LoginContainer);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            // Slide up animation
            var slideUp = new DoubleAnimation
            {
                From = 30,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideUp, containerTransform);
            Storyboard.SetTargetProperty(slideUp, "TranslateY");

            // Scale animation
            var scaleX = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleX, containerTransform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");

            var scaleY = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleY, containerTransform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");

            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(slideUp);
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);

            storyboard.Begin();

            // Animate logo with subtle breathing effect
            StartLogoAnimation();

            // Start subtle particle floating animation
            StartParticleAnimation();

            // Focus username field after animation
            storyboard.Completed += (s, e) => UsernameTextBox.Focus(FocusState.Programmatic);
        }

        private void StartLogoAnimation()
        {
            var logoTransform = new CompositeTransform();
            LogoIcon.RenderTransform = logoTransform;

            var breatheStoryboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            var breatheAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(3),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                AutoReverse = true
            };

            Storyboard.SetTarget(breatheAnimation, logoTransform);
            Storyboard.SetTargetProperty(breatheAnimation, "ScaleX");

            var breatheAnimationY = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(3),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                AutoReverse = true
            };

            Storyboard.SetTarget(breatheAnimationY, logoTransform);
            Storyboard.SetTargetProperty(breatheAnimationY, "ScaleY");

            breatheStoryboard.Children.Add(breatheAnimation);
            breatheStoryboard.Children.Add(breatheAnimationY);
            breatheStoryboard.Begin();
        }

        private async void StartParticleAnimation()
        {
            var random = new Random();

            while (true)
            {
                await Task.Delay(2000); // Update every 2 seconds for subtle movement

                try
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        foreach (var child in ParticlesCanvas.Children)
                        {
                            if (child is FrameworkElement element)
                            {
                                var currentX = Canvas.GetLeft(element);
                                var currentY = Canvas.GetTop(element);

                                var newX = currentX + (random.NextDouble() * 20 - 10); // -10 to 10
                                var newY = currentY + (random.NextDouble() * 20 - 10); // -10 to 10

                                // Keep within reasonable bounds
                                newX = Math.Max(0, Math.Min(newX, 1000));
                                newY = Math.Max(0, Math.Min(newY, 700));

                                Canvas.SetLeft(element, newX);
                                Canvas.SetTop(element, newY);
                            }
                        }
                    });
                }
                catch
                {
                    // Window might be closed, exit the loop
                    break;
                }
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            if (_isLoggingIn) return;

            // Hide any previous error messages
            HideError();

            // Get username and password
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Basic validation
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Please enter your username.");
                UsernameTextBox.Focus(FocusState.Programmatic);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password.");
                PasswordBox.Focus(FocusState.Programmatic);
                return;
            }

            _isLoggingIn = true;
            ShowLoading(true);

            try
            {
                var loginResponse = await _apiService.LoginAsync(username, password);

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.message) &&
                    loginResponse.message.Contains("successful", StringComparison.OrdinalIgnoreCase))
                {
                    // Login successful - show success message briefly
                    ShowSuccess("Login successful! Opening application...");

                    // Small delay for user feedback
                    await Task.Delay(1000);

                    // Open main window
                    var mainWindow = new MainWindow();
                    mainWindow.Activate();

                    // Close login window
                    this.Close();
                }
                else
                {
                    ShowError(loginResponse?.message ?? "Login failed. Please check your credentials.");
                }
            }
            catch (TimeoutException)
            {
                ShowError("Connection timeout. Please check your internet connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("No connection could be made"))
                {
                    ShowError("Cannot connect to server. Please check your connection and try again.");
                }
                else
                {
                    ShowError("Network error occurred. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                _isLoggingIn = false;
                ShowLoading(false);
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 205, 92, 92));
            StatusContainer.Background = new SolidColorBrush(Color.FromArgb(32, 255, 68, 68));
            StatusContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(96, 255, 68, 68));
            StatusContainer.Visibility = Visibility.Visible;

            // Animate error appearance
            AnimateStatusMessage();
        }

        private void ShowSuccess(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 144, 238, 144));
            StatusContainer.Background = new SolidColorBrush(Color.FromArgb(32, 76, 175, 80));
            StatusContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(96, 76, 175, 80));
            StatusContainer.Visibility = Visibility.Visible;

            AnimateStatusMessage();
        }

        private void HideError()
        {
            StatusContainer.Visibility = Visibility.Collapsed;
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            LoginButton.IsEnabled = !show;
            UsernameTextBox.IsEnabled = !show;
            PasswordBox.IsEnabled = !show;
        }

        private void AnimateStatusMessage()
        {
            var transform = new CompositeTransform
            {
                ScaleX = 0.8,
                ScaleY = 0.8
            };
            StatusContainer.RenderTransform = transform;
            StatusContainer.Opacity = 0;

            var storyboard = new Storyboard();

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            Storyboard.SetTarget(fadeIn, StatusContainer);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var scaleX = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleX, transform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");

            var scaleY = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleY, transform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");

            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Begin();
        }
    }
}