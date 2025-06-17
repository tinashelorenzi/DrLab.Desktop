using System;
using System.IO;
using System.Text.Json;
using DrLab.Desktop.Models;

namespace DrLab.Desktop.Services
{
    public class UserSessionManager
    {
        private static UserSessionManager _instance;
        private static readonly object _lock = new object();

        // Current session data
        public LoginResponse CurrentSession { get; private set; }
        public bool IsLoggedIn => CurrentSession != null && !string.IsNullOrEmpty(CurrentSession.tokens?.access);

        // Session file path (stored in app data folder)
        private readonly string _sessionFilePath;

        private UserSessionManager()
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrLab", "LIMS");
            Directory.CreateDirectory(appDataFolder);
            _sessionFilePath = Path.Combine(appDataFolder, "session.json");
        }

        public static UserSessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new UserSessionManager();
                    }
                }
                return _instance;
            }
        }

        public void SaveSession(LoginResponse loginResponse)
        {
            CurrentSession = loginResponse;

            // Save to file for persistence
            try
            {
                var json = JsonSerializer.Serialize(loginResponse, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_sessionFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public bool LoadSavedSession()
        {
            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    var json = File.ReadAllText(_sessionFilePath);
                    CurrentSession = JsonSerializer.Deserialize<LoginResponse>(json);

                    // TODO: Validate token is still valid
                    return IsLoggedIn;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
            }

            return false;
        }

        public void ClearSession()
        {
            CurrentSession = null;

            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    File.Delete(_sessionFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session file: {ex.Message}");
            }
        }

        public string GetAccessToken()
        {
            return CurrentSession?.tokens?.access;
        }

        public string GetRefreshToken()
        {
            return CurrentSession?.tokens?.refresh;
        }

        public UserProfile GetCurrentUser()
        {
            return CurrentSession?.user;
        }

        public string GetUserDisplayName()
        {
            var user = GetCurrentUser();
            return user?.user?.full_name ?? user?.user?.username ?? "Unknown User";
        }

        public string GetUserDepartment()
        {
            return GetCurrentUser()?.department?.name ?? "Unknown Department";
        }
    }
}