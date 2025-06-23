using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using DrLab.Desktop.Models;
using DrLab.Desktop.Models.Messaging;
using DrLab.Desktop.Services;
using LIMS.Models.Messaging;
using LIMS.Services;
using Microsoft.UI.Dispatching;

namespace DrLab.Desktop.ViewModels
{
    public class MessagingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly MessagingWebSocketService _webSocketService;
        private readonly NotificationService _notificationService;
        private readonly ApiService _apiService;
        private readonly EncryptionService _encryptionService;
        private readonly DispatcherQueue _dispatcher;
        private ConversationModel? _activeConversation;
        private bool _isConnected;
        private bool _isCurrentPageActive = true;
        private bool _isInitialized = false;
        private string _searchQuery = string.Empty;

        public ObservableCollection<ConversationModel> Conversations { get; }
        public ObservableCollection<MessageModel> Messages { get; }
        public ObservableCollection<UserModel> TypingUsers { get; }

        public ConversationModel? ActiveConversation
        {
            get => _activeConversation;
            set
            {
                if (_activeConversation != value)
                {
                    if (_activeConversation != null)
                        _activeConversation.IsActive = false;

                    _activeConversation = value;

                    if (_activeConversation != null)
                        _activeConversation.IsActive = true;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasActiveConversation));
                    _ = LoadConversationMessagesAsync();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                _ = FilterConversationsAsync();
            }
        }

        public bool HasActiveConversation => ActiveConversation != null;
        public bool HasConversations => Conversations.Count > 0;

        public MessagingViewModel(
            MessagingWebSocketService webSocketService,
            NotificationService notificationService,
            ApiService apiService,
            EncryptionService encryptionService)
        {
            _webSocketService = webSocketService;
            _notificationService = notificationService;
            _apiService = apiService;
            _encryptionService = encryptionService;
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            Conversations = new ObservableCollection<ConversationModel>();
            Messages = new ObservableCollection<MessageModel>();
            TypingUsers = new ObservableCollection<UserModel>();

            // Subscribe to WebSocket events
            _webSocketService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _webSocketService.MessageReceived += OnMessageReceived;
            _webSocketService.ConversationUpdated += OnConversationUpdated;
            _webSocketService.NotificationReceived += OnNotificationReceived;
            _webSocketService.ReadStatusUpdated += OnReadStatusUpdated;
            _webSocketService.TypingIndicatorReceived += OnTypingIndicatorReceived;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Get auth token from user session or login
                var authToken = UserSessionManager.Instance.AccessToken;
                if (string.IsNullOrEmpty(authToken))
                {
                    throw new Exception("No authentication token available");
                }

                // Connect to WebSocket
                await _webSocketService.ConnectAsync(authToken);

                // Load initial data
                await LoadConversationsAsync();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing messaging: {ex.Message}");
                throw;
            }
        }

        public async Task CleanupAsync()
        {
            try
            {
                await _webSocketService.DisconnectAsync();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        public void SetPageActive(bool isActive)
        {
            _isCurrentPageActive = isActive;
        }

        public async Task SelectConversation(string conversationId)
        {
            try
            {
                var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conversation != null)
                {
                    ActiveConversation = conversation;

                    // Join the conversation for real-time updates
                    if (_webSocketService.IsConnected)
                    {
                        await _webSocketService.JoinConversationAsync(conversationId);
                    }

                    // Mark messages as read
                    await MarkConversationAsReadAsync(conversationId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting conversation: {ex.Message}");
            }
        }

        public async Task SendMessage(string content, string messageType = "text", string? replyToId = null)
        {
            if (ActiveConversation == null || string.IsNullOrWhiteSpace(content)) return;

            try
            {
                // Encrypt message content
                var encryptedContent = await _encryptionService.EncryptMessageAsync(content, ActiveConversation.Id);

                // Send via WebSocket
                await _webSocketService.SendMessageAsync(ActiveConversation.Id, encryptedContent, messageType, replyToId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
                // You could show an error notification here
            }
        }

        public async Task SendTypingIndicator(bool isTyping)
        {
            if (ActiveConversation == null) return;

            try
            {
                await _webSocketService.SendTypingIndicatorAsync(ActiveConversation.Id, isTyping);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending typing indicator: {ex.Message}");
            }
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                var conversations = await _apiService.GetConversationsAsync();

                _dispatcher.TryEnqueue(() =>
                {
                    Conversations.Clear();
                    foreach (var conversation in conversations)
                    {
                        Conversations.Add(ConvertToConversationModel(conversation));
                    }
                    OnPropertyChanged(nameof(HasConversations));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading conversations: {ex.Message}");
            }
        }

        private async Task LoadConversationMessagesAsync()
        {
            if (ActiveConversation == null) return;

            try
            {
                var messages = await _apiService.GetMessagesAsync(ActiveConversation.Id);

                _dispatcher.TryEnqueue(() =>
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        var messageModel = ConvertToMessageModel(message);
                        messageModel.IsSentByCurrentUser = message.SenderId == UserSessionManager.Instance.UserId;
                        Messages.Add(messageModel);
                    }
                });

                // Decrypt messages
                await DecryptMessagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
        }

        private async Task DecryptMessagesAsync()
        {
            foreach (var message in Messages.Where(m => !m.IsDecrypted))
            {
                try
                {
                    var decryptedContent = await _encryptionService.DecryptMessageAsync(
                        message.EncryptedContent,
                        message.ConversationId
                    );

                    _dispatcher.TryEnqueue(() =>
                    {
                        message.Content = decryptedContent;
                        message.IsDecrypted = true;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to decrypt message {message.Id}: {ex.Message}");
                    _dispatcher.TryEnqueue(() =>
                    {
                        message.Content = "Failed to decrypt message";
                        message.IsDecrypted = true;
                    });
                }
            }
        }

        private async Task MarkConversationAsReadAsync(string conversationId)
        {
            try
            {
                var unreadMessages = Messages.Where(m => !m.IsRead && !m.IsSentByCurrentUser);
                foreach (var message in unreadMessages)
                {
                    await _apiService.MarkMessageAsReadAsync(message.Id);
                    message.IsRead = true;
                }

                // Update conversation unread count
                var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conversation != null)
                {
                    conversation.UnreadCount = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking conversation as read: {ex.Message}");
            }
        }

        private async Task FilterConversationsAsync()
        {
            // Simple client-side filtering for now
            // In a production app, you might want to implement server-side search
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                await LoadConversationsAsync();
                return;
            }

            var query = SearchQuery.ToLower();
            var filtered = Conversations.Where(c =>
                c.Name.ToLower().Contains(query) ||
                c.LastMessagePreview.ToLower().Contains(query) ||
                c.Participants.Any(p => p.DisplayName.ToLower().Contains(query))
            ).ToList();

            _dispatcher.TryEnqueue(() =>
            {
                Conversations.Clear();
                foreach (var conversation in filtered)
                {
                    Conversations.Add(conversation);
                }
            });
        }

        // Event handlers
        private void OnConnectionStatusChanged(ConnectionStatusEventArgs e)
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsConnected = e.IsConnected;
            });
        }

        private async void OnMessageReceived(MessageReceivedEventArgs e)
        {
            try
            {
                // Decrypt message content
                var decryptedContent = await _encryptionService.DecryptMessageAsync(e.EncryptedContent, e.ConversationId);

                var message = new MessageModel
                {
                    Id = e.MessageId,
                    ConversationId = e.ConversationId,
                    SenderId = e.SenderId,
                    SenderName = e.SenderName,
                    Content = decryptedContent,
                    EncryptedContent = e.EncryptedContent,
                    MessageType = e.MessageType,
                    Timestamp = e.Timestamp,
                    ReplyToId = e.ReplyToId,
                    IsDecrypted = true,
                    IsSentByCurrentUser = e.SenderId == UserSessionManager.Instance.UserId
                };

                _dispatcher.TryEnqueue(() =>
                {
                    // Add to messages if this is the active conversation
                    if (ActiveConversation?.Id == e.ConversationId)
                    {
                        Messages.Add(message);
                    }

                    // Update conversation preview
                    var conversation = Conversations.FirstOrDefault(c => c.Id == e.ConversationId);
                    if (conversation != null)
                    {
                        conversation.LastMessagePreview = decryptedContent;
                        conversation.LastMessageTime = e.Timestamp;

                        if (!message.IsSentByCurrentUser)
                        {
                            conversation.UnreadCount++;
                        }

                        // Move conversation to top
                        MoveConversationToTop(conversation);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling received message: {ex.Message}");
            }
        }

        private async void OnConversationUpdated(ConversationUpdateEventArgs e)
        {
            _dispatcher.TryEnqueue(async () =>
            {
                var conversation = Conversations.FirstOrDefault(c => c.Id == e.ConversationId);
                if (conversation != null)
                {
                    // Reload conversation data
                    await LoadConversationsAsync();
                }
            });
        }

        private void OnNotificationReceived(NotificationEventArgs e)
        {
            // Only show notifications if user is not on the messaging page or not viewing this conversation
            if (!_isCurrentPageActive || ActiveConversation?.Id != e.ConversationId)
            {
                _notificationService.ShowMessageNotification(
                    e.ConversationName,
                    e.SenderName,
                    e.MessagePreview,
                    () => {
                        // Navigate to conversation when notification is clicked
                        _dispatcher.TryEnqueue(async () =>
                        {
                            await SelectConversation(e.ConversationId);
                        });
                    }
                );
            }
        }

        private void OnReadStatusUpdated(ReadStatusUpdateEventArgs e)
        {
            _dispatcher.TryEnqueue(() =>
            {
                var message = Messages.FirstOrDefault(m => m.Id == e.MessageId);
                if (message != null)
                {
                    message.IsRead = true;
                }
            });
        }

        private void OnTypingIndicatorReceived(TypingIndicatorEventArgs e)
        {
            if (ActiveConversation?.Id != e.ConversationId) return;

            _dispatcher.TryEnqueue(() =>
            {
                var user = TypingUsers.FirstOrDefault(u => u.Id == e.UserId);

                if (e.IsTyping)
                {
                    if (user == null)
                    {
                        TypingUsers.Add(new UserModel
                        {
                            Id = e.UserId,
                            Username = e.UserName,
                            DisplayName = e.UserName
                        });
                    }
                }
                else
                {
                    if (user != null)
                    {
                        TypingUsers.Remove(user);
                    }
                }
            });
        }

        private void MoveConversationToTop(ConversationModel conversation)
        {
            var index = Conversations.IndexOf(conversation);
            if (index > 0)
            {
                Conversations.Move(index, 0);
            }
        }

        // Helper methods
        private ConversationModel ConvertToConversationModel(ConversationDto dto)
        {
            var model = new ConversationModel
            {
                Id = dto.Id,
                Name = dto.Name,
                ConversationType = dto.ConversationType,
                LastMessageTime = dto.LastMessageTime,
                LastMessagePreview = dto.LastMessagePreview,
                UnreadCount = dto.UnreadCount
            };

            foreach (var participant in dto.Participants)
            {
                model.Participants.Add(ConvertToUserModel(participant));
            }

            if (dto.LastMessage != null)
            {
                model.LastMessage = ConvertToMessageModel(dto.LastMessage);
            }

            return model;
        }

        private MessageModel ConvertToMessageModel(MessageDto dto)
        {
            return new MessageModel
            {
                Id = dto.Id,
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                SenderName = dto.SenderName,
                EncryptedContent = dto.EncryptedContent,
                MessageType = dto.MessageType,
                Timestamp = dto.Timestamp,
                IsRead = dto.IsRead,
                ReplyToId = dto.ReplyToId
            };
        }

        private UserModel ConvertToUserModel(UserDto dto)
        {
            return new UserModel
            {
                Id = dto.Id,
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Department = dto.Department,
                IsOnline = dto.IsOnline,
                LastSeen = dto.LastSeen,
                AvatarUrl = dto.AvatarUrl
            };
        }

        // Additional methods for conversation management
        public async Task<ConversationModel?> CreateConversationAsync(List<string> participantIds, string? name = null)
        {
            try
            {
                var conversationType = participantIds.Count > 1 ? "group" : "direct";
                var conversation = await _apiService.CreateConversationAsync(participantIds, conversationType, name);

                if (conversation != null)
                {
                    var conversationModel = ConvertToConversationModel(conversation);
                    _dispatcher.TryEnqueue(() =>
                    {
                        Conversations.Insert(0, conversationModel);
                        OnPropertyChanged(nameof(HasConversations));
                    });
                    return conversationModel;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create conversation: {ex.Message}");
                return null;
            }
        }

        public async Task<List<UserModel>> SearchUsersAsync(string query)
        {
            try
            {
                var users = await _apiService.SearchUsersAsync(query);
                return users.ConvertAll(ConvertToUserModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching users: {ex.Message}");
                return new List<UserModel>();
            }
        }

        public async Task SendFileAsync(string filePath)
        {
            if (ActiveConversation == null) return;

            try
            {
                var fileName = System.IO.Path.GetFileName(filePath);

                // Upload file
                var uploadResult = await _apiService.UploadFileAsync(filePath, fileName);

                if (uploadResult != null)
                {
                    // Create file message
                    var fileContent = JsonSerializer.Serialize(new
                    {
                        type = "file",
                        fileName = fileName,
                        fileId = uploadResult.FileId,
                        fileSize = uploadResult.FileSize
                    });

                    // Encrypt file info
                    var encryptedContent = await _encryptionService.EncryptMessageAsync(
                        fileContent,
                        ActiveConversation.Id
                    );

                    // Send via WebSocket
                    await _webSocketService.SendMessageAsync(ActiveConversation.Id, encryptedContent, "file");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send file: {ex.Message}");
                // Show error notification
                _notificationService.ShowNotification("Error", $"Failed to send file: {ex.Message}", NotificationService.NotificationType.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _webSocketService?.Dispose();
            _apiService?.Dispose();
            _encryptionService?.Dispose();
        }
    }
}