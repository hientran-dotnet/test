using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace EncryptPackUDP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string message = "Hello World"; // Thông điệp cần gửi
            Console.OutputEncoding = Encoding.UTF8; // Đặt mã hóa đầu ra để hỗ trợ tiếng Việt
            

            SendUdpPacket("192.1.1.28", 12000, Encrypt(message, 2));  
        }
        static string Encrypt(string input, int shift)
        {
            string result = "";

            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    char baseChar = char.IsUpper(c) ? 'A' : 'a';
                    char encryptedChar = (char)((c - baseChar + shift) % 26 + baseChar);
                    result += encryptedChar;
                }
                else
                {
                    result += c; // giữ nguyên ký tự không phải chữ cái
                }
            }

            return result;
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
}
