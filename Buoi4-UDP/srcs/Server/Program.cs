using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace Server
{
    internal class Program
    {
        private static UdpClient? udpServer;
        private static IPEndPoint? serverEndPoint;
        private static readonly ConcurrentDictionary<string, IPEndPoint> connectedClients = new();
        private static bool isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== UDP Chat Server ===");
            
            // Cấu hình server
            int port = 8888;
            serverEndPoint = new IPEndPoint(IPAddress.Any, port);
            udpServer = new UdpClient(serverEndPoint);
            
            Console.WriteLine($"Server đang chạy trên port {port}");
            Console.WriteLine("Nhấn 'q' để thoát server");
            Console.WriteLine("=======================");

            // Bắt đầu lắng nghe client
            _ = Task.Run(ListenForClients);
            
            // Chờ lệnh thoát
            while (isRunning)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    isRunning = false;
                    Console.WriteLine("\nĐang tắt server...");
                }
                await Task.Delay(100); // Thêm delay nhỏ để tránh busy waiting
            }
            
            udpServer?.Close();
            Console.WriteLine("Server đã tắt.");
        }

        private static async Task ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    var result = await udpServer!.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);
                    string clientKey = result.RemoteEndPoint.ToString();
                    
                    // Xử lý các loại tin nhắn
                    if (message.StartsWith("JOIN:"))
                    {
                        string username = message.Substring(5);
                        connectedClients[clientKey] = result.RemoteEndPoint;
                        
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {username} đã tham gia chat từ {result.RemoteEndPoint}");
                        
                        // Thông báo cho client khác
                        string joinMessage = $"SYSTEM:{username} đã tham gia chat";
                        await BroadcastMessage(joinMessage, result.RemoteEndPoint);
                        
                        // Gửi xác nhận cho client vừa join
                        string welcomeMessage = "SYSTEM:Chào mừng bạn đến với chat room!";
                        await SendToClient(welcomeMessage, result.RemoteEndPoint);
                    }
                    else if (message.StartsWith("LEAVE:"))
                    {
                        string username = message.Substring(6);
                        connectedClients.TryRemove(clientKey, out _);
                        
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {username} đã rời khỏi chat");
                        
                        // Thông báo cho client khác
                        string leaveMessage = $"SYSTEM:{username} đã rời khỏi chat";
                        await BroadcastMessage(leaveMessage, result.RemoteEndPoint);
                    }
                    else if (message.StartsWith("MESSAGE:"))
                    {
                        string chatMessage = message.Substring(8);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Nhận tin nhắn từ {result.RemoteEndPoint}: {chatMessage}");
                        
                        // Chuyển tiếp tin nhắn cho tất cả client khác
                        await BroadcastMessage($"MESSAGE:{chatMessage}", result.RemoteEndPoint);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Server đã đóng
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xử lý client: {ex.Message}");
                }
            }
        }

        private static async Task BroadcastMessage(string message, IPEndPoint senderEndPoint)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            
            var clientsToRemove = new List<string>();
            
            foreach (var client in connectedClients)
            {
                if (!client.Value.Equals(senderEndPoint))
                {
                    try
                    {
                        await udpServer!.SendAsync(data, client.Value);
                    }
                    catch
                    {
                        // Client không còn hoạt động, đánh dấu để xóa
                        clientsToRemove.Add(client.Key);
                    }
                }
            }
            
            // Xóa các client không hoạt động
            foreach (var clientKey in clientsToRemove)
            {
                connectedClients.TryRemove(clientKey, out _);
            }
        }

        private static async Task SendToClient(string message, IPEndPoint clientEndPoint)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await udpServer!.SendAsync(data, clientEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi tin nhắn đến client: {ex.Message}");
            }
        }
    }
}
