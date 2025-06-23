using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace DrLab.Desktop.Services
{
    public class MessagingWebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly DispatcherQueue _dispatcher;
        private bool _isConnected;
        private bool _disposed;

        // Events
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConversationUpdated;
        public event EventHandler<string>? NotificationReceived;
        public event EventHandler<string>? ReadStatusUpdated;
        public event EventHandler<string>? TypingIndicatorReceived;

        public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

        public MessagingWebSocketService()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public async Task ConnectAsync(string authToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessagingWebSocketService));

            try
            {
                // Cleanup existing connection
                await DisconnectAsync();

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Add authorization header
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

                // Connect to WebSocket endpoint
                var uri = new Uri("ws://localhost:8000/ws/messaging/");
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, true);

                // Start listening for messages
                _ = Task.Run(async () => await ListenForMessagesAsync(_cancellationTokenSource.Token));

                System.Diagnostics.Debug.WriteLine("WebSocket connected successfully");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                System.Diagnostics.Debug.WriteLine($"WebSocket connection failed: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);

                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during WebSocket disconnect: {ex.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task SendMessageAsync(string messageType, object data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessagingWebSocketService));

            if (_webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            try
            {
                var message = new
                {
                    type = messageType,
                    data = data,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);

                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending WebSocket message: {ex.Message}");
                throw;
            }
        }

        public async Task SendTypingIndicatorAsync(string conversationId, bool isTyping)
        {
            try
            {
                await SendMessageAsync("typing", new
                {
                    conversation_id = conversationId,
                    is_typing = isTyping
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending typing indicator: {ex.Message}");
            }
        }

        public async Task MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                await SendMessageAsync("mark_read", new
                {
                    message_id = messageId
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking message as read: {ex.Message}");
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessIncomingMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                System.Diagnostics.Debug.WriteLine("WebSocket listening cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in WebSocket listening: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                _dispatcher.TryEnqueue(() => ConnectionStatusChanged?.Invoke(this, false));
            }
        }

        private async Task ProcessIncomingMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<WebSocketMessage>(json);
                if (message?.Type == null) return;

                // Dispatch to UI thread
                _dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        switch (message.Type.ToLower())
                        {
                            case "message":
                            case "new_message":
                                MessageReceived?.Invoke(this, json);
                                break;
                            case "conversation_updated":
                                ConversationUpdated?.Invoke(this, json);
                                break;
                            case "notification":
                                NotificationReceived?.Invoke(this, json);
                                break;
                            case "read_status":
                            case "message_read":
                                ReadStatusUpdated?.Invoke(this, json);
                                break;
                            case "typing":
                            case "typing_indicator":
                                TypingIndicatorReceived?.Invoke(this, json);
                                break;
                            case "error":
                                System.Diagnostics.Debug.WriteLine($"WebSocket error: {json}");
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.Type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing message in UI thread: {ex.Message}");
                    }
                });
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing WebSocket message: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing WebSocket message: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                DisconnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex.Message}");
            }

            GC.SuppressFinalize(this);
        }

        ~MessagingWebSocketService()
        {
            Dispose();
        }
    }

    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public JsonElement? Data { get; set; }
        public string? ConversationId { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}