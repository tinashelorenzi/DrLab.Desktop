using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DrLab.Desktop.Services;

namespace LIMS.Services
{
    public class EncryptionService
    {
        private readonly Dictionary<string, byte[]> _conversationKeys = new();
        private readonly Dictionary<string, RSA> _userPublicKeys = new();
        private RSA _userPrivateKey;
        private readonly ApiService _apiService;

        public EncryptionService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task InitializeUserKeysAsync(string userId, string password)
        {
            try
            {
                // Get user's encrypted private key from server
                var keyPair = await _apiService.GetUserKeyPairAsync(userId);

                // Decrypt private key with user's password
                _userPrivateKey = DecryptPrivateKey(keyPair.EncryptedPrivateKey, password, keyPair.Salt);

                // Store user's public key
                var publicKeyRsa = RSA.Create();
                publicKeyRsa.ImportRSAPublicKey(Convert.FromBase64String(keyPair.PublicKey), out _);
                _userPublicKeys[userId] = publicKeyRsa;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize user keys: {ex.Message}", ex);
            }
        }

        public async Task<string> EncryptMessageAsync(string content, string conversationId)
        {
            try
            {
                // Get or generate conversation key
                var conversationKey = await GetConversationKeyAsync(conversationId);

                // Generate random IV
                var iv = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                // Encrypt message content with AES
                using var aes = Aes.Create();
                aes.Key = conversationKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var encryptedContent = encryptor.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

                // Combine IV + encrypted content
                var result = new byte[iv.Length + encryptedContent.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to encrypt message: {ex.Message}", ex);
            }
        }

        public async Task<string> DecryptMessageAsync(string encryptedContent, string conversationId)
        {
            try
            {
                // Get conversation key
                var conversationKey = await GetConversationKeyAsync(conversationId);

                // Decode base64
                var encryptedData = Convert.FromBase64String(encryptedContent);

                // Extract IV and encrypted content
                var iv = new byte[16];
                var encrypted = new byte[encryptedData.Length - 16];
                Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
                Buffer.BlockCopy(encryptedData, 16, encrypted, 0, encrypted.Length);

                // Decrypt with AES
                using var aes = Aes.Create();
                aes.Key = conversationKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                return "Failed to decrypt message";
            }
        }

        private async Task<byte[]> GetConversationKeyAsync(string conversationId)
        {
            // Check if we already have the key in memory
            if (_conversationKeys.TryGetValue(conversationId, out var existingKey))
            {
                return existingKey;
            }

            try
            {
                // Try to get encrypted key from server
                var encryptedKey = await _apiService.GetConversationKeyAsync(conversationId);

                if (!string.IsNullOrEmpty(encryptedKey))
                {
                    // Decrypt conversation key with our private key
                    var keyBytes = _userPrivateKey.Decrypt(Convert.FromBase64String(encryptedKey), RSAEncryptionPadding.OaepSHA256);
                    _conversationKeys[conversationId] = keyBytes;
                    return keyBytes;
                }
            }
            catch
            {
                // If we can't get the key from server, generate a new one
            }

            // Generate new conversation key
            var newKey = new byte[32]; // 256-bit key
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKey);
            }

            // Store locally
            _conversationKeys[conversationId] = newKey;

            // Encrypt and send to server for other participants
            await ShareConversationKeyAsync(conversationId, newKey);

            return newKey;
        }

        private async Task ShareConversationKeyAsync(string conversationId, byte[] conversationKey)
        {
            try
            {
                // Get conversation participants
                var participants = await _apiService.GetConversationParticipantsAsync(conversationId);

                var encryptedKeys = new Dictionary<string, string>();

                foreach (var participant in participants)
                {
                    // Get participant's public key
                    if (!_userPublicKeys.TryGetValue(participant.UserId, out var publicKey))
                    {
                        var userKeyPair = await _apiService.GetUserKeyPairAsync(participant.UserId);
                        publicKey = RSA.Create();
                        publicKey.ImportRSAPublicKey(Convert.FromBase64String(userKeyPair.PublicKey), out _);
                        _userPublicKeys[participant.UserId] = publicKey;
                    }

                    // Encrypt conversation key with participant's public key
                    var encryptedKey = publicKey.Encrypt(conversationKey, RSAEncryptionPadding.OaepSHA256);
                    encryptedKeys[participant.UserId] = Convert.ToBase64String(encryptedKey);
                }

                // Send encrypted keys to server
                await _apiService.UpdateConversationKeysAsync(conversationId, encryptedKeys);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to share conversation key: {ex.Message}");
            }
        }

        private RSA DecryptPrivateKey(string encryptedPrivateKey, string password, string salt)
        {
            try
            {
                var encryptedData = Convert.FromBase64String(encryptedPrivateKey);
                var saltBytes = Convert.FromBase64String(salt);

                // Derive key from password
                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                var key = pbkdf2.GetBytes(32);

                // Extract IV and encrypted private key
                var iv = new byte[16];
                var encrypted = new byte[encryptedData.Length - 16];
                Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
                Buffer.BlockCopy(encryptedData, 16, encrypted, 0, encrypted.Length);

                // Decrypt private key
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                // Remove padding
                var paddingLength = decryptedBytes[decryptedBytes.Length - 1];
                var privateKeyBytes = new byte[decryptedBytes.Length - paddingLength];
                Buffer.BlockCopy(decryptedBytes, 0, privateKeyBytes, 0, privateKeyBytes.Length);

                // Import RSA private key
                var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                return rsa;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decrypt private key: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _userPrivateKey?.Dispose();
            foreach (var key in _userPublicKeys.Values)
            {
                key?.Dispose();
            }
            _conversationKeys.Clear();
            _userPublicKeys.Clear();
        }
    }

    // Supporting models for encryption
    public class UserKeyPair
    {
        public string UserId { get; set; }
        public string PublicKey { get; set; }
        public string EncryptedPrivateKey { get; set; }
        public string Salt { get; set; }
    }

    public class ConversationParticipant
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public bool IsAdmin { get; set; }
    }
}