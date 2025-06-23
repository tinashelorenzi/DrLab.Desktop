using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using DrLab.Desktop.Services;
using DrLab.Desktop.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace DrLab.Desktop.Views.Pages
{
    public sealed partial class MessagingPage : Page
    {
        private readonly UserSessionManager _sessionManager;
        private readonly NotificationService _notificationService;
        private readonly ApiService _apiService;
        private MessagingWebSocketService? _webSocketService;

        private List<Conversation> _conversations = new();
        private Conversation? _activeConversation;
        private List<Message> _activeMessages = new();

        public MessagingPage()
        {
            this.InitializeComponent();
            _sessionManager = UserSessionManager.Instance;
            _notificationService = NotificationService.Instance;
            _apiService = (ApiService)App.ServiceProvider.GetService(typeof(ApiService))!;

            this.Loaded += MessagingPage_Loaded;
            this.Unloaded += MessagingPage_Unloaded;
        }

        private async void MessagingPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeMessagingAsync();
        }

        private async void MessagingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            await CleanupAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = InitializeMessagingAsync();
        }

        private async Task InitializeMessagingAsync()
        {
            try
            {
                // Initialize WebSocket connection
                await InitializeWebSocketAsync();

                // Load conversations
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to initialize messaging: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Messaging initialization error: {ex}");
            }
        }

        private async Task InitializeWebSocketAsync()
        {
            try
            {
                if (_webSocketService == null)
                {
                    _webSocketService = new MessagingWebSocketService();

                    // Subscribe to events
                    _webSocketService.ConnectionStatusChanged += OnConnectionStatusChanged;
                    _webSocketService.MessageReceived += OnMessageReceived;
                    _webSocketService.ConversationUpdated += OnConversationUpdated;
                    _webSocketService.NotificationReceived += OnNotificationReceived;
                }

                var authToken = _sessionManager.AuthToken;
                if (!string.IsNullOrEmpty(authToken))
                {
                    await _webSocketService.ConnectAsync(authToken);
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowWarningAsync("Real-time messaging unavailable. You can still send messages.");
                System.Diagnostics.Debug.WriteLine($"WebSocket initialization error: {ex}");
            }
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                var response = await _apiService.GetConversationsAsync();
                if (response.Success && response.Data != null)
                {
                    _conversations = response.Data;
                    PopulateConversationsList();
                }
                else
                {
                    // Show empty state
                    ShowEmptyConversationsState();
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to load conversations: {ex.Message}");
                ShowEmptyConversationsState();
                System.Diagnostics.Debug.WriteLine($"Load conversations error: {ex}");
            }
        }

        private void PopulateConversationsList()
        {
            // Clear existing items except the sample ones (for now)
            // In a real implementation, you would clear everything and rebuild

            if (_conversations.Count == 0)
            {
                ShowEmptyConversationsState();
            }
            else
            {
                EmptyConversationsPanel.Visibility = Visibility.Collapsed;
                // TODO: Dynamically create conversation items from _conversations list
            }
        }

        private void ShowEmptyConversationsState()
        {
            EmptyConversationsPanel.Visibility = Visibility.Visible;
            // Hide sample conversation items
            ConversationItem1.Visibility = Visibility.Collapsed;
            ConversationItem2.Visibility = Visibility.Collapsed;
        }

        private async void NewConversationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Show new conversation dialog
                await _notificationService.ShowInfoAsync("New conversation dialog will be implemented soon!");

                // In a real implementation:
                // var dialog = new NewConversationDialog();
                // var result = await dialog.ShowAsync();
                // if (result == ContentDialogResult.Primary)
                // {
                //     await LoadConversationsAsync();
                // }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to start new conversation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"New conversation error: {ex}");
            }
        }

        private async void ConversationItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                // TODO: Get conversation ID from the clicked item
                // For now, simulate selecting the first conversation

                if (_conversations.Count > 0)
                {
                    await SelectConversationAsync(_conversations[0]);
                }
                else
                {
                    // Use sample data for demonstration
                    var sampleConversation = new Conversation
                    {
                        Id = "sample-1",
                        DisplayName = "Dr. Sarah Smith",
                        Title = "Dr. Sarah Smith"
                    };
                    await SelectConversationAsync(sampleConversation);
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to select conversation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Select conversation error: {ex}");
            }
        }

        private async Task SelectConversationAsync(Conversation conversation)
        {
            try
            {
                _activeConversation = conversation;

                // Update UI
                ConversationTitle.Text = conversation.DisplayName;
                ConversationAvatar.Text = GetInitials(conversation.DisplayName);
                ConversationStatus.Text = "Online"; // TODO: Get real status

                // Show conversation panel
                WelcomePanel.Visibility = Visibility.Collapsed;
                ActiveConversationPanel.Visibility = Visibility.Visible;

                // Load messages for this conversation
                await LoadMessagesAsync(conversation.Id);
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to load conversation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Load conversation error: {ex}");
            }
        }

        private async Task LoadMessagesAsync(string conversationId)
        {
            try
            {
                var response = await _apiService.GetMessagesAsync(conversationId);
                if (response.Success && response.Data != null)
                {
                    _activeMessages = response.Data;
                    PopulateMessagesList();
                }
                else
                {
                    _activeMessages.Clear();
                    MessagesContainer.Children.Clear();
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to load messages: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Load messages error: {ex}");
            }
        }

        private void PopulateMessagesList()
        {
            // TODO: Clear MessagesContainer and rebuild with actual messages
            // For now, the sample messages are hardcoded in XAML

            // Scroll to bottom
            MessagesScrollViewer.ScrollToVerticalOffset(MessagesScrollViewer.ScrollableHeight);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Check if Shift is held down
                var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                if (!shiftPressed)
                {
                    e.Handled = true;
                    await SendMessageAsync();
                }
            }
        }

        private async Task SendMessageAsync()
        {
            try
            {
                var messageText = MessageTextBox.Text.Trim();
                if (string.IsNullOrEmpty(messageText) || _activeConversation == null)
                    return;

                // Clear input immediately for better UX
                MessageTextBox.Text = string.Empty;

                // Send message via API
                var response = await _apiService.SendMessageAsync(_activeConversation.Id, messageText);
                if (response.Success && response.Data != null)
                {
                    // Add message to UI immediately (optimistic update)
                    AddMessageToUI(response.Data, true);

                    // Scroll to bottom
                    MessagesScrollViewer.ScrollToVerticalOffset(MessagesScrollViewer.ScrollableHeight);
                }
                else
                {
                    await _notificationService.ShowErrorAsync("Failed to send message. Please try again.");
                    // Restore message text
                    MessageTextBox.Text = messageText;
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to send message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Send message error: {ex}");
            }
        }

        private void AddMessageToUI(Message message, bool isOutgoing)
        {
            // TODO: Create message UI element and add to MessagesContainer
            // For now, this is a placeholder
            System.Diagnostics.Debug.WriteLine($"Adding message to UI: {message.Content}");
        }

        private async void AttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Show file picker and handle file upload
                await _notificationService.ShowInfoAsync("File attachment feature will be implemented soon!");
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Failed to attach file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Attachment error: {ex}");
            }
        }

        // WebSocket event handlers
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Update connection status in UI if needed
                System.Diagnostics.Debug.WriteLine($"WebSocket connection: {(isConnected ? "Connected" : "Disconnected")}");
            });
        }

        private void OnMessageReceived(object? sender, string messageData)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // TODO: Parse message and update UI
                    System.Diagnostics.Debug.WriteLine($"Received message: {messageData}");

                    // If this message is for the active conversation, add it to UI
                    // Otherwise, update conversation list with unread indicator
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing received message: {ex.Message}");
                }
            });
        }

        private void OnConversationUpdated(object? sender, string updateData)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // TODO: Update conversation in the list
                System.Diagnostics.Debug.WriteLine($"Conversation updated: {updateData}");
            });
        }

        private void OnNotificationReceived(object? sender, string notificationData)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                // TODO: Show notification
                await _notificationService.ShowInfoAsync("New message received");
            });
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (_webSocketService != null)
                {
                    await _webSocketService.DisconnectAsync();
                    _webSocketService.Dispose();
                    _webSocketService = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        private string GetInitials(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "?";

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length > 0)
            {
                return parts[0][0].ToString().ToUpper();
            }
            return "?";
        }

        // Method to handle real-time updates from parent window
        public void HandleRealTimeUpdate(string updateType, object data)
        {
            try
            {
                switch (updateType.ToLower())
                {
                    case "new_message":
                        OnMessageReceived(null, data.ToString() ?? "");
                        break;
                    case "conversation_updated":
                        OnConversationUpdated(null, data.ToString() ?? "");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle real-time update: {ex.Message}");
            }
        }

        // Method to refresh messaging data
        public async Task RefreshAsync()
        {
            await LoadConversationsAsync();
            if (_activeConversation != null)
            {
                await LoadMessagesAsync(_activeConversation.Id);
            }
        }
    }
}