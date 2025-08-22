using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        SendUdpPacket("127.1.1.57", 8000, "Hello World");
    }

    static void SendUdpPacket(string ipAddress, int port, string message)
    {
        try
        {
            using (UdpClient client = new UdpClient())
            {
                // Chuyển thông điệp sang mảng byte
                byte[] data = Encoding.UTF8.GetBytes(message);

                // Tạo endpoint đến server DNS (Google DNS ở đây)
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

                // Gửi gói tin UDP
                client.Send(data, data.Length, endPoint);

                Console.WriteLine($"Đã gửi: \"{message}\" đến {ipAddress}:{port}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi UDP: {ex.Message}");
        }
    }
}
