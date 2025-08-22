using System;
using System.Net.Sockets;
using System.Text;

namespace Client_Program
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.OutputEncoding = Encoding.UTF8; // Đặt mã hóa đầu ra để hỗ trợ tiếng Việt
            try
            {
                // 1. Kết nối đến server
                TcpClient client = new TcpClient("localhost", 8080);
                Console.WriteLine("Đã kết nối đến server!");

                // 2. Nhập số từ người dùng
                Console.Write("Nhập một số (0-9): ");
                string number = Console.ReadLine();

                // 3. Gửi số đến server
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(number);
                stream.Write(data, 0, data.Length);

                // 4. Nhận kết quả từ server
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string result = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // 5. Hiển thị kết quả
                Console.WriteLine($"Server trả về: {result}");

                // 6. Đóng kết nối
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
            }

            Console.WriteLine("Nhấn phím bất kỳ để thoát...");
            Console.ReadKey();
        }
    }
}