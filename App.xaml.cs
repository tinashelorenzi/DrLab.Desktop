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
        private IHost _host;
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            this.InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var builder = Host.CreateDefaultBuilder();

            // Add configuration with proper path resolution for WinUI 3
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Get the application's base directory instead of current directory
                var appDirectory = AppContext.BaseDirectory;

                // Alternative: Use the directory where the executable is located
                // var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                config.SetBasePath(appDirectory);

                // Try to load appsettings.json, but make it optional and provide fallback
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                // Add in-memory configuration as fallback
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ApiSettings:BaseUrl"] = "http://localhost:8000",
                    ["ApiSettings:LoginEndpoint"] = "/api/auth/login/",
                    ["ApiSettings:Timeout"] = "30",
                    ["AppSettings:AppName"] = "DrLab LIMS Desktop",
                    ["AppSettings:Version"] = "1.0.0"
                });
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
                var mainWindow = new Views.MainWindow();
                mainWindow.Activate();
            }
            else
            {
                // Show login window
                var loginWindow = new Views.LoginWindow();
                loginWindow.Activate();
            }
        }
    }
}