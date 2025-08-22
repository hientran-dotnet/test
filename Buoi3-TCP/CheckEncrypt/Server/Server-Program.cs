using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        private static readonly int PORT = 9090;
        private static readonly int SHIFT_VALUE = 2;

        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                Console.WriteLine("=== TCP SERVER ===");
                Console.WriteLine($"Đang khởi động server trên port {PORT}...");
                
                // Tạo TcpListener
                IPAddress localAddress = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddress, PORT);
                
                // Bắt đầu lắng nghe
                server.Start();
                Console.WriteLine("Server đã sẵn sàng! Đang chờ client kết nối...");
                Console.WriteLine("Nhấn Ctrl+C để dừng server.\n");

                while (true)
                {
                    // Chấp nhận kết nối từ client
                    using (TcpClient client = server.AcceptTcpClient())
                    {
                        Console.WriteLine("Có client đã kết nối!");
                        
                        // Xử lý client
                        HandleClient(client);
                        
                        Console.WriteLine("Client đã ngắt kết nối.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi server: {ex.Message}");
            }
            finally
            {
                server?.Stop();
                Console.WriteLine("Server đã dừng.");
            }
        }

        static void HandleClient(TcpClient client)
        {
            Console.OutputEncoding = Encoding.UTF8;

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (client.Connected)
                {
                    // Đọc dữ liệu từ client
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    // Chuyển đổi dữ liệu nhận được
                    string encryptedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // Hiển thị dữ liệu đã mã hóa nhận được
                    Console.WriteLine($"📨 Nhận dữ liệu mã hóa: '{encryptedMessage}'");
                    
                    // Giải mã dữ liệu
                    string decryptedMessage = DecryptMessage(encryptedMessage);
                    Console.WriteLine($"🔓 Dữ liệu sau khi giải mã: '{decryptedMessage}'");

                    // Kiểm tra nếu client muốn thoát
                    if (decryptedMessage.ToLower() == "exit")
                    {
                        Console.WriteLine("Client yêu cầu ngắt kết nối.");
                        break;
                    }

                    // Tạo phản hồi
                    string response = $"Server đã nhận: {decryptedMessage}";
                    
                    // Mã hóa phản hồi trước khi gửi
                    string encryptedResponse = EncryptMessage(response);
                    Console.WriteLine($"📤 Gửi phản hồi mã hóa: '{encryptedResponse}'");
                    
                    // Gửi phản hồi đã mã hóa cho client
                    byte[] responseData = Encoding.UTF8.GetBytes(encryptedResponse);
                    stream.Write(responseData, 0, responseData.Length);
                    
                    Console.WriteLine("✅ Đã gửi phản hồi!\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xử lý client: {ex.Message}");
            }
        }

        // Hàm mã hóa ký tự cho server (dịch chuyển -2: A→Y, B→Z, C→A)
        static char EncryptChar(char c, int shift)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                // Dịch chuyển âm: giảm 2 ký tự (A→Y)
                return (char)((c - baseChar - shift + 26) % 26 + baseChar);
            }
            return c; // Không mã hóa ký tự không phải chữ cái
        }

        // Hàm giải mã ký tự từ client (dịch chuyển +2 để giải mã dữ liệu từ client)
        static char DecryptChar(char c, int shift)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                // Giải mã dữ liệu client gửi (client mã hóa +2, server giải mã +2)
                return (char)((c - baseChar - shift + 26) % 26 + baseChar);
            }
            return c; // Không giải mã ký tự không phải chữ cái
        }

        // Hàm mã hóa chuỗi
        static string EncryptMessage(string message)
        {
            StringBuilder encrypted = new StringBuilder();
            foreach (char c in message)
            {
                encrypted.Append(EncryptChar(c, SHIFT_VALUE));
            }
            return encrypted.ToString();
        }

        // Hàm giải mã chuỗi
        static string DecryptMessage(string encryptedMessage)
        {
            StringBuilder decrypted = new StringBuilder();
            foreach (char c in encryptedMessage)
            {
                decrypted.Append(DecryptChar(c, SHIFT_VALUE));
            }
            return decrypted.ToString();
        }
    }
}
