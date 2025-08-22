using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        private static readonly string SERVER_IP = "127.0.0.1";
        private static readonly int SERVER_PORT = 9090;
        private static readonly int SHIFT_VALUE = 2;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                Console.WriteLine("=== TCP CLIENT ===");
                Console.WriteLine($"Đang kết nối đến server {SERVER_IP}:{SERVER_PORT}...");

                // Tạo kết nối đến server
                client = new TcpClient(SERVER_IP, SERVER_PORT);
                stream = client.GetStream();

                Console.WriteLine("✅ Kết nối thành công!");
                Console.WriteLine("Nhập ký tự để gửi đến server (gõ 'exit' để thoát):\n");

                string input;
                while (true)
                {
                    // Nhập dữ liệu từ người dùng
                    Console.Write("Nhập tin nhắn: ");
                    input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    // Hiển thị dữ liệu gốc
                    Console.WriteLine($"📝 Dữ liệu gốc: '{input}'");

                    // Mã hóa dữ liệu trước khi gửi
                    string encryptedMessage = EncryptMessage(input);
                    Console.WriteLine($"🔐 Dữ liệu sau khi mã hóa: '{encryptedMessage}'");

                    // Gửi dữ liệu đã mã hóa đến server
                    byte[] data = Encoding.UTF8.GetBytes(encryptedMessage);
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("📤 Đã gửi dữ liệu mã hóa đến server!");

                    // Kiểm tra nếu người dùng muốn thoát
                    if (input.ToLower() == "exit")
                    {
                        Console.WriteLine("Đang ngắt kết nối...");
                        break;
                    }

                    // Nhận phản hồi từ server
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        // Chuyển đổi dữ liệu nhận được
                        string encryptedResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📨 Nhận phản hồi mã hóa: '{encryptedResponse}'");
                        
                        // Giải mã phản hồi từ server (server mã hóa -2, client giải mã +2)
                        string decryptedResponse = DecryptServerMessage(encryptedResponse);
                        Console.WriteLine($"🔓 Phản hồi sau khi giải mã: '{decryptedResponse}'");
                    }
                    
                    Console.WriteLine(""); // Dòng trống để dễ đọc
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi client: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Console.WriteLine("Kết nối đã đóng.");
                Console.WriteLine("Nhấn Enter để thoát...");
                Console.ReadLine();
            }
        }

        // Hàm mã hóa ký tự cho client (dịch chuyển +2: A→C)
        static char EncryptChar(char c, int shift)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                return (char)((c - baseChar + shift) % 26 + baseChar);
            }
            return c; // Không mã hóa ký tự không phải chữ cái
        }

        // Hàm giải mã ký tự (dịch chuyển -2) - dùng cho giải mã dữ liệu gửi từ client
        static char DecryptChar(char c, int shift)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                return (char)((c - baseChar - shift + 26) % 26 + baseChar);
            }
            return c; // Không giải mã ký tự không phải chữ cái
        }

        // Hàm giải mã phản hồi từ server (server mã hóa -2, client giải mã +2)
        static char DecryptServerChar(char c, int shift)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                // Server mã hóa -2, nên client phải giải mã +2
                return (char)((c - baseChar + shift) % 26 + baseChar);
            }
            return c;
        }

        // Hàm mã hóa chuỗi cho client
        static string EncryptMessage(string message)
        {
            StringBuilder encrypted = new StringBuilder();
            foreach (char c in message)
            {
                encrypted.Append(EncryptChar(c, SHIFT_VALUE));
            }
            return encrypted.ToString();
        }

        // Hàm giải mã chuỗi phản hồi từ server
        static string DecryptServerMessage(string encryptedMessage)
        {
            StringBuilder decrypted = new StringBuilder();
            foreach (char c in encryptedMessage)
            {
                decrypted.Append(DecryptServerChar(c, SHIFT_VALUE));
            }
            return decrypted.ToString();
        }
    }
}
