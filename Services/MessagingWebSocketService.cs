// Services/MessagingWebSocketService.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DrLab.Desktop;

namespace LIMS.Services
{
    public class MessagingWebSocketService : INotifyPropertyChanged, IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string _baseUrl;
        private string _authToken;
        private bool _isConnected;
        private readonly object _lockObject = new object();

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<MessageReceivedEventArgs> MessageReceived;
        public event Action<ConversationUpdateEventArgs> ConversationUpdated;
        public event Action<NotificationEventArgs> NotificationReceived;
        public event Action<ConnectionStatusEventArgs> ConnectionStatusChanged;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    ConnectionStatusChanged?.Invoke(new ConnectionStatusEventArgs { IsConnected = value });
                }
            }
        }

        public MessagingWebSocketService(string baseUrl)
        {
            _baseUrl = baseUrl.Replace("http", "ws");
        }

        public async Task ConnectAsync(string authToken)
        {
            try
            {
                _authToken = authToken;
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                // Add authorization header
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

                var uri = new Uri($"{_baseUrl}/ws/messaging/");
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                IsConnected = true;

                // Start listening for messages
                _ = Task.Run(ListenForMessages);

                // Send connection confirmation
                await SendAsync(new
                {
                    type = "connection_established"
                });
            }
            catch (Exception ex)
            {
                IsConnected = false;
                throw new Exception($"Failed to connect to messaging server: {ex.Message}", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw on disconnect
                System.Diagnostics.Debug.WriteLine($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
                _webSocket?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
        }

        public async Task SendMessageAsync(string conversationId, string encryptedContent, string messageType = "text")
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "send_message",
                conversation_id = conversationId,
                encrypted_content = encryptedContent,
                message_type = messageType
            });
        }

        public async Task JoinConversationAsync(string conversationId)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "join_conversation",
                conversation_id = conversationId
            });
        }

        public async Task MarkAsReadAsync(string messageId)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "mark_as_read",
                message_id = messageId
            });
        }

        public async Task SendTypingIndicatorAsync(string conversationId, bool isTyping)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "typing_indicator",
                conversation_id = conversationId,
                is_typing = isTyping
            });
        }

        private async Task SendAsync(object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                var arraySegment = new ArraySegment<byte>(bytes);

                await _webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send message: {ex.Message}", ex);
            }
        }

        private async Task ListenForMessages()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleReceivedMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        IsConnected = false;
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is triggered
            }
            catch (Exception ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine($"Error listening for messages: {ex.Message}");

                // Attempt to reconnect after a delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    if (!string.IsNullOrEmpty(_authToken))
                    {
                        try
                        {
                            await ConnectAsync(_authToken);
                        }
                        catch
                        {
                            // Reconnection failed, will try again later
                        }
                    }
                });
            }
        }

        private async Task HandleReceivedMessage(string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                var messageType = root.GetProperty("type").GetString();

                switch (messageType)
                {
                    case "new_message":
                        await HandleNewMessage(root);
                        break;

                    case "conversation_updated":
                        await HandleConversationUpdate(root);
                        break;

                    case "new_message_notification":
                        await HandleNotification(root);
                        break;

                    case "read_status_update":
                        await HandleReadStatusUpdate(root);
                        break;

                    case "typing_indicator":
                        await HandleTypingIndicator(root);
                        break;

                    case "connection_established":
                        // Connection confirmed
                        break;

                    case "error":
                        var errorMessage = root.GetProperty("message").GetString();
                        System.Diagnostics.Debug.WriteLine($"WebSocket error: {errorMessage}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling received message: {ex.Message}");
            }
        }

        private async Task HandleNewMessage(JsonElement root)
        {
            var messageData = new MessageReceivedEventArgs
            {
                MessageId = root.GetProperty("message_id").GetString(),
                ConversationId = root.GetProperty("conversation_id").GetString(),
                SenderId = root.GetProperty("sender_id").GetString(),
                SenderName = root.GetProperty("sender_name").GetString(),
                EncryptedContent = root.GetProperty("encrypted_content").GetString(),
                MessageType = root.GetProperty("message_type").GetString(),
                Timestamp = DateTime.Parse(root.GetProperty("timestamp").GetString()),
                ReplyToId = root.TryGetProperty("reply_to_id", out var replyTo) ? replyTo.GetString() : null
            };

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageReceived?.Invoke(messageData);
            });
        }

        private async Task HandleConversationUpdate(JsonElement root)
        {
            var updateData = new ConversationUpdateEventArgs
            {
                ConversationId = root.GetProperty("conversation_id").GetString(),
                UpdateType = root.GetProperty("update_type").GetString(),
                Data = root.GetProperty("data").GetRawText()
            };

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ConversationUpdated?.Invoke(updateData);
            });
        }

        private async Task HandleNotification(JsonElement root)
        {
            var notificationData = new NotificationEventArgs
            {
                ConversationId = root.GetProperty("conversation_id").GetString(),
                ConversationName = root.GetProperty("conversation_name").GetString(),
                SenderName = root.GetProperty("sender_name").GetString(),
                MessagePreview = root.GetProperty("message_preview").GetString(),
                Timestamp = DateTime.Parse(root.GetProperty("timestamp").GetString())
            };

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                NotificationReceived?.Invoke(notificationData);
            });
        }

        private async Task HandleReadStatusUpdate(JsonElement root)
        {
            // Handle read status updates
            await Task.CompletedTask;
        }

        private async Task HandleTypingIndicator(JsonElement root)
        {
            // Handle typing indicators
            await Task.CompletedTask;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            DisconnectAsync().Wait(1000);
        }
    }

    // Event argument classes
    public class MessageReceivedEventArgs
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string EncryptedContent { get; set; }
        public string MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public string ReplyToId { get; set; }
    }

    public class ConversationUpdateEventArgs
    {
        public string ConversationId { get; set; }
        public string UpdateType { get; set; }
        public string Data { get; set; }
    }

    public class NotificationEventArgs
    {
        public string ConversationId { get; set; }
        public string ConversationName { get; set; }
        public string SenderName { get; set; }
        public string MessagePreview { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ConnectionStatusEventArgs
    {
        public bool IsConnected { get; set; }
    }
}