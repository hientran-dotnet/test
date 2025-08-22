using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Server
    {
        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Tạo TcpListener lắng nghe trên IP localhost, port 8080
            TcpListener server = new TcpListener(IPAddress.Any, 8080);
            server.Start();
            Console.WriteLine("Server đang chạy...");
            int dem = 0;

            while (true)
            {
                // Chờ client kết nối (không block)
                TcpClient client = await server.AcceptTcpClientAsync();
                if(client.Connected)
                {
                    dem++;
                    Console.WriteLine($"Client {dem} đã kết nối!");
                }    


                // Xử lý client trên một task riêng
                _ = HandleClientAsync(client);
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Nhận từ client: {message}");

            if (int.TryParse(message, out int seconds))
            {
                // Đếm ngược
                for (int i = seconds; i > 0; i--)
                {
                    Console.WriteLine($"[Client {client.Client.RemoteEndPoint}] Còn {i} giây...");
                    await Task.Delay(1000);
                }

                string response = $"Server đã đếm xong {seconds} giây!";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            else
            {
                string error = "Giá trị không hợp lệ!";
                byte[] responseBytes = Encoding.UTF8.GetBytes(error);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }

            client.Close();
        }
    }
}
