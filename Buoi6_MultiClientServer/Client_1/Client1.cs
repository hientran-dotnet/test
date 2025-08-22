using System.Net.Sockets;
using System.Text;

namespace Client_1
{
    internal class Client1
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            using TcpClient client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 8080);
            Console.WriteLine("Đã kết nối tới server!");

            using NetworkStream stream = client.GetStream();

            // Nếu có tham số từ command line -> lấy làm số giây
            Console.Write("Nhập tin nhắn: ");
            string message = Console.ReadLine();

            // Gửi dữ liệu
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);

            // Chờ phản hồi
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine("Phản hồi từ server: " + response);
        }
    }
}
