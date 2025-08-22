using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        private static TcpListener? listener;
        private static bool isRunning = false;
        private static readonly string[] randomMessages = {
            "Xin chào! Tôi đã đếm xong 10 giây!",
            "Chào bạn! Thời gian đã hết rồi!",
            "Hi! Mình vừa đếm được 10 giây!",
            "Hello! 10 giây đã trôi qua!",
            "Xin chào! Đã hoàn thành việc đếm thời gian!"
        };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8; // Đặt mã hóa đầu ra để hỗ trợ tiếng Việt
            Console.WriteLine("=== TCP SERVER ===");
            Console.WriteLine("Khởi động server...");

            // Khởi tạo TcpListener trên localhost port 8888
            listener = new TcpListener(IPAddress.Any, 8888);
            
            try
            {
                listener.Start();
                isRunning = true;
                Console.WriteLine("Server đang lắng nghe trên port 8888");
                Console.WriteLine("Nhấn 'q' để thoát server");

                // Tạo task để lắng nghe phím thoát
                Task.Run(() => {
                    while (isRunning)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        {
                            Console.WriteLine("\nĐang thoát server...");
                            isRunning = false;
                            listener?.Stop();
                            break;
                        }
                    }
                });

                // Lắng nghe và xử lý client
                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await listener.AcceptTcpClientAsync();
                        Console.WriteLine($"Client kết nối: {tcpClient.Client.RemoteEndPoint}");
                        
                        // Xử lý client trong thread riêng để hỗ trợ nhiều client đồng thời
                        _ = Task.Run(() => HandleClientAsync(tcpClient));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Server đã được dừng
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (isRunning)
                        {
                            Console.WriteLine($"Lỗi khi chấp nhận client: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khởi động server: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("Server đã dừng.");
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            
            try
            {
                while (client.Connected && isRunning)
                {
                    // Đọc dữ liệu từ client
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"Client {client.Client.RemoteEndPoint} đã ngắt kết nối");
                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"Nhận từ {client.Client.RemoteEndPoint}: {receivedData}");

                    // Kiểm tra nếu client gửi số 10
                    if (int.TryParse(receivedData, out int number) && number == 10)
                    {
                        Console.WriteLine($"Đang đếm thời gian 10 giây cho client {client.Client.RemoteEndPoint}...");
                        
                        // Đếm thời gian 10 giây
                        await Task.Delay(10000);
                        
                        // Chọn message ngẫu nhiên
                        Random random = new Random();
                        string randomMessage = randomMessages[random.Next(randomMessages.Length)];
                        
                        // Gửi phản hồi về client
                        byte[] response = Encoding.UTF8.GetBytes(randomMessage);
                        await stream.WriteAsync(response, 0, response.Length);
                        
                        Console.WriteLine($"Đã gửi phản hồi cho {client.Client.RemoteEndPoint}: {randomMessage}");
                    }
                    else
                    {
                        // Nếu không phải số 10, gửi thông báo lỗi
                        string errorMessage = "Vui lòng gửi số 10!";
                        byte[] errorResponse = Encoding.UTF8.GetBytes(errorMessage);
                        await stream.WriteAsync(errorResponse, 0, errorResponse.Length);
                        
                        Console.WriteLine($"Gửi thông báo lỗi cho {client.Client.RemoteEndPoint}: {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xử lý client {client.Client.RemoteEndPoint}: {ex.Message}");
            }
            finally
            {
                stream.Close();
                client.Close();
                Console.WriteLine($"Đã đóng kết nối với client {client.Client.RemoteEndPoint}");
            }
        }
    }
}
