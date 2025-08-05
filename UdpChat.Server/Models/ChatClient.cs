using System.Net;

namespace UdpChat.Server.Models
{
    /// Модель клиента чата
    public class ChatClient
    {
        /// Уникальный идентификатор клиента
        public string Id { get; set; } = string.Empty;

        /// Никнейм клиента
        public string Nickname { get; set; } = string.Empty;

        /// Конечная точка клиента (IP и порт)
        public IPEndPoint EndPoint { get; set; } = null!;

        /// Время последней активности клиента
        public DateTime LastActivity { get; set; }

        /// Время регистрации клиента
        public DateTime RegistrationTime { get; set; }

        /// Статус клиента (активен/неактивен)
        public bool IsActive { get; set; } = true;

        public ChatClient()
        {
            LastActivity = DateTime.Now;
            RegistrationTime = DateTime.Now;
        }

        /// Обновляет время последней активности
        public void UpdateActivity()
        {
            LastActivity = DateTime.Now;
            IsActive = true;
        }

        /// Проверяет, активен ли клиент (не более 2 минут бездействия)
        public bool IsClientActive()
        {
            return DateTime.Now.Subtract(LastActivity).TotalMinutes <= 2;
        }

        /// Помечает клиента как неактивного
        public void MarkAsInactive()
        {
            IsActive = false;
        }

        public override string ToString()
        {
            return $"{Nickname} ({Id}) - {EndPoint}";
        }
    }
} 