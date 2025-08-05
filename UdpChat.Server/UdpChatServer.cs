using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpChat.Server.Models;
using Timer = System.Threading.Timer;

namespace UdpChat.Server
{

    /// UDP-сервер чата
    public class UdpChatServer
    {
        private readonly UdpClient _udpClient;
        private readonly Dictionary<string, ChatClient> _clients;
        private readonly Dictionary<string, HashSet<string>> _rooms = new();
        private readonly Timer _cleanupTimer;
        private readonly int _port;
        private bool _isRunning;

        public UdpChatServer(int port = 9000)
        {
            _port = port;
            _udpClient = new UdpClient(port);
            _clients = new Dictionary<string, ChatClient>();
            _cleanupTimer = new Timer(CleanupInactiveClients, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            _isRunning = false;
        }

        /// Запускает сервер
        public async Task StartAsync()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            Console.WriteLine($"UDP-сервер чата запущен на порту {_port}");
            Console.WriteLine("Ожидание подключений...");

            try
            {
                while (_isRunning)
                {
                    var result = await _udpClient.ReceiveAsync();
                    await ProcessMessageAsync(result.Buffer, result.RemoteEndPoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в сервере: {ex.Message}");
            }
        }

        /// Останавливает сервер
        public void Stop()
        {
            _isRunning = false;
            _udpClient?.Close();
            _cleanupTimer?.Dispose();
            Console.WriteLine("Сервер остановлен");
        }

        /// Обрабатывает входящее сообщение
        private async Task ProcessMessageAsync(byte[] data, IPEndPoint remoteEndPoint)
        {
            try
            {
                var messageText = Encoding.UTF8.GetString(data);
                var message = ChatMessage.Parse(messageText);

                Console.WriteLine($"Получено сообщение от {remoteEndPoint}: {message.Header}");

                switch (message.Header)
                {
                    case MessageType.HELLO:
                        await HandleHelloAsync(message, remoteEndPoint);
                        break;
                    case MessageType.PING:
                        await HandlePingAsync(message, remoteEndPoint);
                        break;
                    case MessageType.MSG:
                        await HandleMessageAsync(message, remoteEndPoint);
                        break;
                    case MessageType.ACK:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        private async Task HandleHelloAsync(ChatMessage message, IPEndPoint remoteEndPoint)
        {
            var client = new ChatClient
            {
                Id = message.SourceId,
                Nickname = message.Body,
                EndPoint = remoteEndPoint
            };

            _clients[client.Id] = client;
            Console.WriteLine($"Клиент зарегистрирован: {client}");

            // Отправляем ACK для подтверждения регистрации
            var ackMessage = ChatMessage.CreateAck(message.MessageId, message.SourceId, "SERVER");
            await SendMessageAsync(ackMessage, remoteEndPoint);

            // Уведомляем всех клиентов о новом пользователе
            await BroadcastUserListAsync();
        }

        /// Обрабатывает PING сообщение (обновление активности)
        private async Task HandlePingAsync(ChatMessage message, IPEndPoint remoteEndPoint)
        {
            if (_clients.TryGetValue(message.SourceId, out var client))
            {
                client.UpdateActivity();
                Console.WriteLine($"PING от клиента: {client.Nickname}");

                // Отправляем ACK для подтверждения PING
                var ackMessage = ChatMessage.CreateAck(message.MessageId, message.SourceId, "SERVER");
                await SendMessageAsync(ackMessage, remoteEndPoint);
            }
        }

        /// Обрабатывает MSG сообщение (пересылка сообщения)
        private async Task HandleMessageAsync(ChatMessage message, IPEndPoint remoteEndPoint)
        {
            // Обновляем активность отправителя
            if (_clients.TryGetValue(message.SourceId, out var sender))
            {
                sender.UpdateActivity();
            }

            if (message.DestinationId == "ALL")
            {
                await BroadcastMessageAsync(message);
            }
            // ==== Команды групповых рассылок ====
            if (message.Body.StartsWith("JOIN:"))
            {
                var room = message.Body[5..];
                if (!_rooms.TryGetValue(room, out var set))
                    set = _rooms[room] = new HashSet<string>();
                set.Add(message.SourceId);
                Console.WriteLine($"{message.SourceId} присоединился к комнате {room}");
            }
            else if (message.Body.StartsWith("LEAVE:"))
            {
                var room = message.Body[6..];
                if (_rooms.TryGetValue(room, out var set)) set.Remove(message.SourceId);
                Console.WriteLine($"{message.SourceId} покинул комнату {room}");
            }
            else if (message.Body.StartsWith("GROUP:"))
            {
                var parts = message.Body.Split(':', 3);
                if (parts.Length == 3)
                {
                    var room = parts[1];
                    var text = parts[2];
                    if (_rooms.TryGetValue(room, out var set))
                    {
                        foreach (var id in set.Where(id => id != message.SourceId))
                        {
                            if (_clients.TryGetValue(id, out var cli))
                                await SendMessageAsync(ChatMessage.CreateMessage(message.SourceId, id, text), cli.EndPoint);
                        }
                    }
                    Console.WriteLine($"GROUP {room}: {text} от {message.SourceId}");
                }

            }
            // Если сообщение адресовано конкретному клиенту
            else if (_clients.TryGetValue(message.DestinationId, out var recipient))
            {
                await SendMessageAsync(message, recipient.EndPoint);
                Console.WriteLine($"Сообщение от {message.SourceId} к {message.DestinationId}: {message.Body}");
            }
            else
            {
                Console.WriteLine($"Получатель {message.DestinationId} не найден");
            }

            // Отправляем ACK отправителю
            var ackMessage = ChatMessage.CreateAck(message.MessageId, message.SourceId, "SERVER");
            await SendMessageAsync(ackMessage, remoteEndPoint);
        }

        /// Отправляет сообщение конкретному клиенту
        private async Task SendMessageAsync(ChatMessage message, IPEndPoint endPoint)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message.ToString());
                await _udpClient.SendAsync(data, data.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        /// Рассылает сообщение всем активным клиентам
        private async Task BroadcastMessageAsync(ChatMessage message)
        {
            var activeClients = _clients.Values.Where(c => c.IsActive && c.Id != message.SourceId).ToList();
            
            foreach (var client in activeClients)
            {
                await SendMessageAsync(message, client.EndPoint);
            }

            Console.WriteLine($"Сообщение от {message.SourceId} отправлено {activeClients.Count} клиентам: {message.Body}");
        }

        /// Рассылает список пользователей всем клиентам
        private async Task BroadcastUserListAsync()
        {
            var userList = string.Join(",", _clients.Values.Where(c => c.IsActive).Select(c => $"{c.Nickname}:{c.Id}"));
            var message = ChatMessage.CreateMessage("SERVER", "ALL", $"USERS:{userList}");
            
            await BroadcastMessageAsync(message);
        }

        /// Очищает неактивных клиентов
        private void CleanupInactiveClients(object? state)
        {
            var inactiveClients = _clients.Values.Where(c => !c.IsClientActive()).ToList();

            foreach (var client in inactiveClients)
            {
                _clients.Remove(client.Id);
                Console.WriteLine($"Клиент удален за неактивность: {client}");
            }

            if (inactiveClients.Count > 0)
            {
                // Уведомляем оставшихся клиентов об изменении списка пользователей
                _ = BroadcastUserListAsync();
            }
        }

        /// Получает список активных клиентов
        public List<ChatClient> GetActiveClients()
        {
            return _clients.Values.Where(c => c.IsActive).ToList();
        }

        /// Получает статистику сервера
        public string GetServerStats()
        {
            var activeClients = _clients.Values.Count(c => c.IsActive);
            var totalClients = _clients.Count;
            return $"Активных клиентов: {activeClients}, Всего зарегистрировано: {totalClients}";
        }
    }
} 