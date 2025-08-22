using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8; // Đặt mã hóa đầu ra để hỗ trợ tiếng Việt
            Console.WriteLine("=== TCP CLIENT ===");
            
            TcpClient? client = null;
            NetworkStream? stream = null;
            
            try
            {
                // Kết nối đến server
                Console.WriteLine("Đang kết nối đến server (localhost:8888)...");
                client = new TcpClient();
                await client.ConnectAsync("localhost", 8888);
                stream = client.GetStream();
                
                Console.WriteLine("Kết nối thành công!");
                Console.WriteLine("Nhập 'exit' để thoát chương trình");
                Console.WriteLine();

                while (client.Connected)
                {
                    // Nhập dữ liệu từ người dùng
                    Console.Write("Nhập số (khuyến nghị nhập 10): ");
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(input))
                        continue;
                        
                    if (input.ToLower() == "exit")
                    {
                        Console.WriteLine("Đang thoát...");
                        break;
                    }

                    try
                    {
                        // Gửi dữ liệu đến server
                        byte[] data = Encoding.UTF8.GetBytes(input);
                        await stream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"Đã gửi: {input}");

                        // Kiểm tra nếu gửi số 10
                        if (int.TryParse(input, out int number) && number == 10)
                        {
                            Console.WriteLine("Đang chờ server đếm thời gian 10 giây...");
                            
                            // Hiển thị countdown
                            for (int i = 10; i >= 1; i--)
                            {
                                Console.Write($"\rChờ: {i} giây  ");
                                await Task.Delay(1000);
                            }
                            Console.WriteLine("\rChờ: Hoàn thành!");
                        }

                        // Đọc phản hồi từ server
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        
                        if (bytesRead > 0)
                        {
                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine($"Phản hồi từ server: {response}");
                        }
                        else
                        {
                            Console.WriteLine("Server đã ngắt kết nối.");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi giao tiếp với server: {ex.Message}");
                        break;
                    }
                    
                    Console.WriteLine();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Không thể kết nối đến server: {ex.Message}");
                Console.WriteLine("Hãy đảm bảo server đang chạy trên localhost:8888");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
            }
            finally
            {
                // Đóng kết nối an toàn
                stream?.Close();
                client?.Close();
                Console.WriteLine("Đã đóng kết nối.");
                Console.WriteLine("Nhấn phím bất kỳ để thoát...");
                Console.ReadKey();
            }
        }
    }
}
