using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace DrLab.Desktop.Services
{
    public class UserSessionManager
    {
        private static UserSessionManager? _instance;
        private static readonly object _lock = new object();

        private readonly string _sessionFilePath;
        private const string SessionFileName = "user_session.dat";

        // Current user session data
        public string? UserId { get; private set; }
        public string? Username { get; private set; }
        public string? Email { get; private set; }
        public string? Department { get; private set; }
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTime? TokenExpiry { get; private set; }
        public bool RememberMe { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(AccessToken);
        public bool IsTokenValid => TokenExpiry.HasValue && DateTime.UtcNow < TokenExpiry.Value;

        private UserSessionManager()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            _sessionFilePath = Path.Combine(localFolder.Path, SessionFileName);
        }

        public static UserSessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new UserSessionManager();
                    }
                }
                return _instance;
            }
        }

        public void SetUserSession(string userId, string username, string email, string department,
            string accessToken, string refreshToken, DateTime tokenExpiry, bool rememberMe = false)
        {
            UserId = userId;
            Username = username;
            Email = email;
            Department = department;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenExpiry = tokenExpiry;
            RememberMe = rememberMe;

            if (rememberMe)
            {
                SaveSessionToFile();
            }
        }

        public void UpdateTokens(string accessToken, string refreshToken, DateTime tokenExpiry)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenExpiry = tokenExpiry;

            if (RememberMe && IsLoggedIn)
            {
                SaveSessionToFile();
            }
        }

        public bool LoadSavedSession()
        {
            try
            {
                if (!File.Exists(_sessionFilePath))
                    return false;

                var encryptedData = File.ReadAllBytes(_sessionFilePath);
                var sessionJson = DecryptData(encryptedData);

                if (string.IsNullOrEmpty(sessionJson))
                    return false;

                var session = JsonSerializer.Deserialize<UserSession>(sessionJson);
                if (session == null)
                    return false;

                // Check if session is still valid
                if (session.TokenExpiry <= DateTime.UtcNow)
                {
                    ClearSession();
                    return false;
                }

                UserId = session.UserId;
                Username = session.Username;
                Email = session.Email;
                Department = session.Department;
                AccessToken = session.AccessToken;
                RefreshToken = session.RefreshToken;
                TokenExpiry = session.TokenExpiry;
                RememberMe = true;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved session: {ex.Message}");
                ClearSession();
                return false;
            }
        }

        private void SaveSessionToFile()
        {
            try
            {
                var session = new UserSession
                {
                    UserId = UserId ?? string.Empty,
                    Username = Username ?? string.Empty,
                    Email = Email ?? string.Empty,
                    Department = Department ?? string.Empty,
                    AccessToken = AccessToken ?? string.Empty,
                    RefreshToken = RefreshToken ?? string.Empty,
                    TokenExpiry = TokenExpiry ?? DateTime.UtcNow,
                    SavedAt = DateTime.UtcNow
                };

                var sessionJson = JsonSerializer.Serialize(session);
                var encryptedData = EncryptData(sessionJson);

                File.WriteAllBytes(_sessionFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }

        public void ClearSession()
        {
            UserId = null;
            Username = null;
            Email = null;
            Department = null;
            AccessToken = null;
            RefreshToken = null;
            TokenExpiry = null;
            RememberMe = false;

            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    File.Delete(_sessionFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing session file: {ex.Message}");
            }
        }

        public void Logout()
        {
            ClearSession();
        }

        private byte[] EncryptData(string data)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var entropy = GetMachineEntropy();

                // Use Windows DPAPI for encryption (user-specific)
                return ProtectedData.Protect(dataBytes, entropy, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error encrypting data: {ex.Message}");
                throw;
            }
        }

        private string DecryptData(byte[] encryptedData)
        {
            try
            {
                var entropy = GetMachineEntropy();
                var decryptedBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting data: {ex.Message}");
                return string.Empty;
            }
        }

        private byte[] GetMachineEntropy()
        {
            // Create entropy based on machine characteristics
            var machineId = Environment.MachineName + Environment.UserName;
            return SHA256.HashData(Encoding.UTF8.GetBytes(machineId + "DrLab_LIMS_Session"));
        }

        // Session validation
        public bool RequiresTokenRefresh()
        {
            if (!TokenExpiry.HasValue)
                return true;

            // Refresh if token expires within the next 5 minutes
            return DateTime.UtcNow.AddMinutes(5) >= TokenExpiry.Value;
        }

        public UserInfo GetCurrentUserInfo()
        {
            return new UserInfo
            {
                UserId = UserId ?? string.Empty,
                Username = Username ?? string.Empty,
                Email = Email ?? string.Empty,
                Department = Department ?? string.Empty,
                IsLoggedIn = IsLoggedIn
            };
        }
    }

    // Data transfer objects
    internal class UserSession
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public DateTime SavedAt { get; set; }
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; }
    }
}