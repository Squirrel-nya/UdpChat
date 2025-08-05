using System;

namespace UdpChat.Server.Models
{
    /// <summary>
    /// Типы сообщений в чате
    /// </summary>
    public enum MessageType
    {
        HELLO,  // Приветствие при подключении
        PING,   // Проверка активности
        MSG,    // Текстовое сообщение
        ACK     // Подтверждение получения
    }

    /// Модель сообщения чата
    public class ChatMessage
    {
        public MessageType Header { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string DestinationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// Создает сообщение 
        public static ChatMessage Parse(string message)
        {
            var parts = message.Split('|');
            if (parts.Length < 5)
                throw new ArgumentException("Неверный формат сообщения");

            return new ChatMessage
            {
                Header = Enum.Parse<MessageType>(parts[0]),
                SourceId = parts[1],
                DestinationId = parts[2],
                MessageId = parts[3],
                Body = parts[4]
            };
        }

        /// Преобразует сообщение в строку для отправки
        public override string ToString()
        {
            return $"{Header}|{SourceId}|{DestinationId}|{MessageId}|{Body}";
        }

        /// Создает ACK сообщение для подтверждения получения
        public static ChatMessage CreateAck(string originalMessageId, string sourceId, string destinationId)
        {
            return new ChatMessage
            {
                Header = MessageType.ACK,
                SourceId = destinationId,  // ACK отправляется от получателя
                DestinationId = sourceId,  // ACK отправляется отправителю
                MessageId = originalMessageId,
                Body = "OK"
            };
        }

        /// Создает HELLO сообщение для регистрации клиента
        public static ChatMessage CreateHello(string clientId, string nickname)
        {
            return new ChatMessage
            {
                Header = MessageType.HELLO,
                SourceId = clientId,
                DestinationId = "SERVER",
                MessageId = Guid.NewGuid().ToString(),
                Body = nickname
            };
        }

        /// Создает PING сообщение для проверки активности
        public static ChatMessage CreatePing(string clientId)
        {
            return new ChatMessage
            {
                Header = MessageType.PING,
                SourceId = clientId,
                DestinationId = "SERVER",
                MessageId = Guid.NewGuid().ToString(),
                Body = "PING"
            };
        }

        /// Создает MSG сообщение для отправки текста
        public static ChatMessage CreateMessage(string sourceId, string destinationId, string text)
        {
            return new ChatMessage
            {
                Header = MessageType.MSG,
                SourceId = sourceId,
                DestinationId = destinationId,
                MessageId = Guid.NewGuid().ToString(),
                Body = text
            };
        }
    }
} 