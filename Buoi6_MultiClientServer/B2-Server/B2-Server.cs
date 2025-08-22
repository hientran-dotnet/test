using System.Net;
using System.Net.Sockets;
using System.Text;

namespace B2_Server
{
    internal class Program
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
                if (client.Connected)
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

            try
            {
                // 1. Đọc tên file
                //byte[] nameBuffer = new byte[256];
                //int nameBytes = await stream.ReadAsync(nameBuffer, 0, nameBuffer.Length);
                //string fileName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytes);

                /// 1. Đọc độ dài tên file
                byte[] lengthBuffer = new byte[4];
                await stream.ReadAsync(lengthBuffer, 0, 4);
                int nameLength = BitConverter.ToInt32(lengthBuffer, 0);

                // 2. Đọc tên file đúng số byte
                byte[] nameBuffer = new byte[nameLength];
                await stream.ReadAsync(nameBuffer, 0, nameBuffer.Length);
                string fileName = Encoding.UTF8.GetString(nameBuffer);

                // 2. Đọc kích thước file
                byte[] sizeBuffer = new byte[8]; // long = 8 byte
                await stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length);
                long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

                Console.WriteLine($"Nhận file: {fileName}, dung lượng: {fileSize} bytes");

                // 3. Nhận dữ liệu file và lưu
                string savePath = Path.Combine("ReceivedFiles", fileName);
                Directory.CreateDirectory("ReceivedFiles");

                using FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                byte[] buffer = new byte[4096];
                long totalRead = 0;

                while (totalRead < fileSize)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // ngắt kết nối
                    await fs.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                }

                Console.WriteLine($"Hoàn tất lưu file {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
