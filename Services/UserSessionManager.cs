using System;
using System.IO;
using System.Text.Json;
using DrLab.Desktop.Models;
using Windows.Storage;

namespace DrLab.Desktop.Services
{
    public class UserSessionManager
    {
        private static readonly Lazy<UserSessionManager> _instance = new(() => new UserSessionManager());
        public static UserSessionManager Instance => _instance.Value;

        private const string SessionFileName = "user_session.json";

        private User? _currentUser;
        private string? _authToken;
        private string? _refreshToken;
        private DateTime? _tokenExpiry;

        public event EventHandler<User?>? UserChanged;
        public event EventHandler<bool>? LoginStatusChanged;

        public User? CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    UserChanged?.Invoke(this, value);
                    LoginStatusChanged?.Invoke(this, value != null);
                }
            }
        }

        public string? AuthToken => _authToken;
        public string? RefreshToken => _refreshToken;
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_authToken);

        private UserSessionManager() { }

        public void SetSession(User user, string authToken, string? refreshToken = null, DateTime? tokenExpiry = null)
        {
            CurrentUser = user;
            _authToken = authToken;
            _refreshToken = refreshToken;
            _tokenExpiry = tokenExpiry;

            SaveSessionToStorage();
        }

        public void ClearSession()
        {
            CurrentUser = null;
            _authToken = null;
            _refreshToken = null;
            _tokenExpiry = null;

            ClearSessionFromStorage();
        }

        public bool LoadSavedSession()
        {
            try
            {
                var sessionData = LoadSessionFromStorage();
                if (sessionData != null)
                {
                    CurrentUser = sessionData.User;
                    _authToken = sessionData.AuthToken;
                    _refreshToken = sessionData.RefreshToken;
                    _tokenExpiry = sessionData.TokenExpiry;

                    // Check if token is expired
                    if (_tokenExpiry.HasValue && DateTime.UtcNow >= _tokenExpiry.Value)
                    {
                        ClearSession();
                        return false;
                    }

                    return IsLoggedIn;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load saved session: {ex.Message}");
            }

            return false;
        }

        public string? GetUserDisplayName()
        {
            return CurrentUser?.DisplayName ?? CurrentUser?.Username;
        }

        public string? GetUserDepartment()
        {
            return CurrentUser?.Department;
        }

        public string? GetUserId()
        {
            return CurrentUser?.Id;
        }

        private void SaveSessionToStorage()
        {
            try
            {
                var sessionData = new SessionData
                {
                    User = CurrentUser,
                    AuthToken = _authToken,
                    RefreshToken = _refreshToken,
                    TokenExpiry = _tokenExpiry,
                    SavedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, SessionFileName);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        private SessionData? LoadSessionFromStorage()
        {
            try
            {
                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, SessionFileName);

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var sessionData = JsonSerializer.Deserialize<SessionData>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return sessionData;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
            }

            return null;
        }

        private void ClearSessionFromStorage()
        {
            try
            {
                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, SessionFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session storage: {ex.Message}");
            }
        }

        public bool IsTokenExpired()
        {
            return _tokenExpiry.HasValue && DateTime.UtcNow >= _tokenExpiry.Value;
        }

        public void UpdateUser(User user)
        {
            if (IsLoggedIn)
            {
                CurrentUser = user;
                SaveSessionToStorage();
            }
        }

        // Internal class for serialization
        private class SessionData
        {
            public User? User { get; set; }
            public string? AuthToken { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime? TokenExpiry { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}