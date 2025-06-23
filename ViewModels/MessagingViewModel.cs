// Views/MessagingPage.xaml
< !--
< Page x: Class = "LIMS.Views.MessagingPage"
      xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns: x = "http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns: mc = "http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns: d = "http://schemas.microsoft.com/expression/blend/2008"
      xmlns: messaging = "clr-namespace:LIMS.Views.Messaging"
      mc: Ignorable = "d"
      d: DesignHeight = "600" d: DesignWidth = "1000"
      Title = "Messaging" >

    < Grid Background = "#F5F5F5" >
        < Grid.ColumnDefinitions >
            < ColumnDefinition Width = "320" MinWidth = "250" />
            < ColumnDefinition Width = "*" />
        </ Grid.ColumnDefinitions >

        < !--Conversations Panel-- >
        < Border Grid.Column = "0"
                Background = "White"
                BorderBrush = "#E0E0E0"
                BorderThickness = "0,0,1,0" >
            < messaging:ConversationListPanel x:Name = "ConversationListPanel" />
        </ Border >

        < !--Messages Panel-- >
        < Grid Grid.Column = "1" >
            < messaging:MessageView x:Name = "MessageView" />
        </ Grid >

        < !--Splitter-- >
        < GridSplitter Grid.Column = "0"
                      Width = "1"
                      HorizontalAlignment = "Right"
                      Background = "#E0E0E0"
                      BorderThickness = "0" />
    </ Grid >
</ Page >
-->

// Views/MessagingPage.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using LIMS.ViewModels;

namespace LIMS.Views
{
    public partial class MessagingPage : Page
    {
        public MessagingViewModel ViewModel { get; private set; }

        public MessagingPage()
        {
            InitializeComponent();
            ViewModel = new MessagingViewModel();
            DataContext = ViewModel;

            // Wire up events
            ConversationListPanel.ConversationSelected += OnConversationSelected;
            MessageView.MessageSent += OnMessageSent;

            Loaded += MessagingPage_Loaded;
            Unloaded += MessagingPage_Unloaded;
        }

        private async void MessagingPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }

        private async void MessagingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.CleanupAsync();
        }

        private void OnConversationSelected(object sender, ConversationSelectedEventArgs e)
        {
            ViewModel.SelectConversation(e.ConversationId);
        }

        private void OnMessageSent(object sender, MessageSentEventArgs e)
        {
            ViewModel.SendMessage(e.Content, e.MessageType);
        }
    }

    // Event Args
    public class ConversationSelectedEventArgs : EventArgs
    {
        public string ConversationId { get; set; }
    }

    public class MessageSentEventArgs : EventArgs
    {
        public string Content { get; set; }
        public string MessageType { get; set; } = "text";
    }
}

// ViewModels/MessagingViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using LIMS.Models.Messaging;
using LIMS.Services;

namespace LIMS.ViewModels
{
    public class MessagingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly MessagingWebSocketService _webSocketService;
        private readonly NotificationService _notificationService;
        private readonly ApiService _apiService;
        private readonly EncryptionService _encryptionService;
        private ConversationModel _activeConversation;
        private bool _isConnected;
        private bool _isCurrentPageActive = true;
        private bool _isInitialized = false;

        public ObservableCollection<ConversationModel> Conversations { get; }
        public ObservableCollection<MessageModel> Messages { get; }

        public ConversationModel ActiveConversation
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
                    await LoadConversationMessagesAsync();
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

        public bool HasActiveConversation => ActiveConversation != null;
        public bool HasConversations => Conversations.Count > 0;

        public MessagingViewModel()
        {
            Conversations = new ObservableCollection<ConversationModel>();
            Messages = new ObservableCollection<MessageModel>();

            _webSocketService = new MessagingWebSocketService("ws://localhost:8000");
            _notificationService = new NotificationService();
            _apiService = new ApiService("http://localhost:8000");
            _encryptionService = new EncryptionService(_apiService);

            // Subscribe to WebSocket events
            _webSocketService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _webSocketService.MessageReceived += OnMessageReceived;
            _webSocketService.ConversationUpdated += OnConversationUpdated;
            _webSocketService.NotificationReceived += OnNotificationReceived;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Authenticate user
                var authResult = await _apiService.LoginAsync(
                    App.CurrentUserName, // You'll need to get this from your auth system
                    "user-password"     // You'll need to get this from your auth system
                );

                if (authResult == null)
                {
                    throw new Exception("Authentication failed");
                }

                // Update app context
                App.CurrentUserId = authResult.User.Id;
                App.CurrentUserName = authResult.User.Username;
                App.CurrentUserEmail = authResult.User.Email;

                // Initialize encryption
                await _encryptionService.InitializeUserKeysAsync(App.CurrentUserId, "user-password");

                // Connect to WebSocket
                await _webSocketService.ConnectAsync(authResult.Access);

                // Load conversations
                await LoadConversationsAsync();

                _isCurrentPageActive = true;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize messaging: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task CleanupAsync()
        {
            _isCurrentPageActive = false;
            await _webSocketService.DisconnectAsync();
        }

        public async void SelectConversation(string conversationId)
        {
            var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conversation != null)
            {
                ActiveConversation = conversation;

                // Mark all messages as read
                foreach (var message in Messages.Where(m => !m.IsRead && !m.IsSentByCurrentUser))
                {
                    message.IsRead = true;
                    _ = _webSocketService.MarkAsReadAsync(message.Id);
                }

                // Reset unread count
                conversation.UnreadCount = 0;
            }
        }

        public async void SendMessage(string content, string messageType = "text")
        {
            if (ActiveConversation == null || string.IsNullOrWhiteSpace(content))
                return;

            try
            {
                // Create temporary message for immediate UI update
                var tempMessage = new MessageModel
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = ActiveConversation.Id,
                    SenderId = App.CurrentUserId,
                    SenderName = App.CurrentUserName,
                    Content = content,
                    MessageType = messageType,
                    Timestamp = DateTime.Now,
                    IsSentByCurrentUser = true,
                    IsDecrypted = true
                };

                // Add to UI immediately
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(tempMessage);
                });

                // Encrypt content
                var encryptedContent = await _encryptionService.EncryptMessageAsync(content, ActiveConversation.Id);

                // Send via WebSocket and API
                await _webSocketService.SendMessageAsync(ActiveConversation.Id, encryptedContent, messageType);

                // Update conversation preview
                ActiveConversation.LastMessagePreview = content;
                ActiveConversation.LastMessageTime = DateTime.Now;

                // Move conversation to top
                MoveConversationToTop(ActiveConversation);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetPageActive(bool isActive)
        {
            _isCurrentPageActive = isActive;
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                var conversations = await _apiService.GetConversationsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();

                    foreach (var conversation in conversations)
                    {
                        Conversations.Add(conversation);
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();

                    foreach (var message in messages)
                    {
                        Messages.Add(message);
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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        message.Content = decryptedContent;
                        message.IsDecrypted = true;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to decrypt message {message.Id}: {ex.Message}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        message.Content = "Failed to decrypt message";
                        message.IsDecrypted = true;
                    });
                }
            }
        }

        private void OnConnectionStatusChanged(ConnectionStatusEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
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
                    IsSentByCurrentUser = e.SenderId == App.CurrentUserId,
                    IsDecrypted = true
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Add message to active conversation if it matches
                    if (ActiveConversation?.Id == e.ConversationId)
                    {
                        Messages.Add(message);

                        // Auto-mark as read if user is on this conversation
                        if (!message.IsSentByCurrentUser)
                        {
                            message.IsRead = true;
                            _ = _webSocketService.MarkAsReadAsync(message.Id);
                        }
                    }
                    else
                    {
                        // Update unread count for other conversations
                        var conversation = Conversations.FirstOrDefault(c => c.Id == e.ConversationId);
                        if (conversation != null && !message.IsSentByCurrentUser)
                        {
                            conversation.UnreadCount++;
                        }
                    }

                    // Update conversation preview
                    var conv = Conversations.FirstOrDefault(c => c.Id == e.ConversationId);
                    if (conv != null)
                    {
                        conv.LastMessagePreview = decryptedContent;
                        conv.LastMessageTime = e.Timestamp;
                        MoveConversationToTop(conv);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling received message: {ex.Message}");
            }
        }

        private void OnConversationUpdated(ConversationUpdateEventArgs e)
        {
            // Handle conversation updates (new participants, name changes, etc.)
            Application.Current.Dispatcher.Invoke(async () =>
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
                _notificationService.ShowNotification(
                    e.ConversationName,
                    $"{e.SenderName}: {e.MessagePreview}",
                    () => {
                        // Navigate to conversation when notification is clicked
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SelectConversation(e.ConversationId);
                        });
                    }
                );
            }
        }

        private void MoveConversationToTop(ConversationModel conversation)
        {
            var index = Conversations.IndexOf(conversation);
            if (index > 0)
            {
                Conversations.Move(index, 0);
            }
        }

        // Additional methods for conversation management
        public async Task<ConversationModel> CreateConversationAsync(List<string> participantIds, string name = null)
        {
            try
            {
                var conversationType = participantIds.Count > 1 ? "group" : "direct";
                var conversation = await _apiService.CreateConversationAsync(participantIds, conversationType, name);

                if (conversation != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Conversations.Insert(0, conversation);
                        OnPropertyChanged(nameof(HasConversations));
                    });
                }

                return conversation;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create conversation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public async Task<List<UserModel>> SearchUsersAsync(string query)
        {
            try
            {
                return await _apiService.SearchUsersAsync(query);
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
                    var fileContent = $"📎 {fileName}";

                    // Encrypt file info
                    var encryptedContent = await _encryptionService.EncryptMessageAsync(
                        JsonSerializer.Serialize(new
                        {
                            type = "file",
                            fileName = fileName,
                            fileId = uploadResult.FileId,
                            fileSize = uploadResult.FileSize
                        }),
                        ActiveConversation.Id
                    );

                    // Send via WebSocket
                    await _webSocketService.SendMessageAsync(ActiveConversation.Id, encryptedContent, "file");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _webSocketService?.Dispose();
            _apiService?.Dispose();
            _encryptionService?.Dispose();
            _notificationService?.Dispose();
        }
    }
}