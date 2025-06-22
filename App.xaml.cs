using System;
using System.Collections.Generic;
using System.IO;
using DrLab.Desktop.Services;
using DrLab.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace DrLab.Desktop
{
    public partial class App : Application
    {
        private IHost? _host;
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public App()
        {
            this.InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var builder = Host.CreateDefaultBuilder();

            // Add configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Get the application's directory instead of current directory
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                config.SetBasePath(appDirectory);

                // Make appsettings.json optional since we provide defaults
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                // Add default configuration
                var defaultConfig = new Dictionary<string, string?>
                {
                    ["ApiSettings:BaseUrl"] = "http://localhost:8000",
                    ["ApiSettings:Timeout"] = "30"
                };
                config.AddInMemoryCollection(defaultConfig);
            });

            // Add services
            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<ApiService>();
                services.AddSingleton(UserSessionManager.Instance);
            });

            _host = builder.Build();
            ServiceProvider = _host.Services;
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Check if user is already logged in
            var sessionManager = UserSessionManager.Instance;

            if (sessionManager.LoadSavedSession() && sessionManager.IsLoggedIn)
            {
                // User has a valid saved session, go directly to main window
                var mainWindow = new MainWindow();
                mainWindow.Activate();
            }
            else
            {
                // Show login window
                var loginWindow = new LoginWindow();
                loginWindow.Activate();
            }
        }
    }
}