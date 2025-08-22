using System.Net.Sockets;
using System.Text;

namespace B2_Client_2
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write("Nhập đường dẫn file cần gửi: ");
            string filePath = Console.ReadLine();
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File không tồn tại!");
                return;
            }
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File không tồn tại!");
                return;
            }

            using TcpClient client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 8080);
            Console.WriteLine("Đã kết nối tới server!");

            using NetworkStream stream = client.GetStream();
            FileInfo fileInfo = new FileInfo(filePath);

            // 1. Gửi độ dài tên file (4 byte int)
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await stream.WriteAsync(nameLength, 0, nameLength.Length);
            await stream.WriteAsync(nameBytes, 0, nameBytes.Length);

            // 2. Gửi kích thước file
            byte[] sizeBytes = BitConverter.GetBytes(fileInfo.Length);
            await stream.WriteAsync(sizeBytes, 0, sizeBytes.Length);

            // 3. Gửi dữ liệu file
            byte[] buffer = new byte[4096];
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }

            Console.WriteLine($"Đã gửi file {fileInfo.Name} ({fileInfo.Length} bytes)");
        }
    }
}
