using System;
using System.Threading.Tasks;

namespace UdpChat.Server
{
    internal class Program
    {
        private static UdpChatServer? _server;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== UDP Chat Server ===");
            Console.WriteLine("Введите порт для сервера (по умолчанию 9000):");
            
            var portInput = Console.ReadLine();
            var port = 9000;
            
            if (!string.IsNullOrEmpty(portInput) && int.TryParse(portInput, out var customPort))
            {
                port = customPort;
            }

            _server = new UdpChatServer(port);

            // Обработка завершения работы
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                StopServer();
            };

            try
            {
                // Запускаем сервер в отдельной задаче
                var serverTask = _server.StartAsync();

                Console.WriteLine("Команды сервера:");
                Console.WriteLine("  stats - показать статистику");
                Console.WriteLine("  clients - показать список клиентов");
                Console.WriteLine("  quit - завершить работу");
                Console.WriteLine();

                // Основной цикл обработки команд
                while (true)
                {
                    var command = Console.ReadLine()?.Trim().ToLower();
                    
                    switch (command)
                    {
                        case "stats":
                            ShowStats();
                            break;
                        case "clients":
                            ShowClients();
                            break;
                        case "quit":
                        case "exit":
                            StopServer();
                            return;
                        case "":
                            break;
                        default:
                            Console.WriteLine("Неизвестная команда. Доступные команды: stats, clients, quit");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска сервера: {ex.Message}");
            }
        }

        private static void ShowStats()
        {
            if (_server != null)
            {
                Console.WriteLine($"Статистика сервера: {_server.GetServerStats()}");
            }
        }

        private static void ShowClients()
        {
            if (_server != null)
            {
                var clients = _server.GetActiveClients();
                Console.WriteLine($"Активные клиенты ({clients.Count}):");
                
                if (clients.Count == 0)
                {
                    Console.WriteLine("  Нет активных клиентов");
                }
                else
                {
                    foreach (var client in clients)
                    {
                        Console.WriteLine($"  {client.Nickname} ({client.Id}) - {client.EndPoint}");
                    }
                }
            }
        }

        private static void StopServer()
        {
            if (_server != null)
            {
                Console.WriteLine("Остановка сервера...");
                _server.Stop();
            }
        }
    }
}
