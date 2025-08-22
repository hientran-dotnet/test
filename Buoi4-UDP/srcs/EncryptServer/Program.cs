using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UdpDecryptServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = 8000;
            Console.OutputEncoding = Encoding.UTF8;

            using (UdpClient server = new UdpClient(port))
            {
                Console.WriteLine($"🔒 Server đang lắng nghe UDP trên cổng {port}...");

                while (true)
                {
                    // Nhận dữ liệu từ bất kỳ IP nào gửi đến
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = server.Receive(ref remoteEndPoint);
                    string encryptedMessage = Encoding.UTF8.GetString(receivedBytes);
                    // Giải mã
                    string decryptedMessage = Decrypt(encryptedMessage, 2);
                    Console.WriteLine($"Giải mã: {decryptedMessage}");
                    string pass = "12345";
                    if(encryptedMessage == pass)
                    {
                        Console.WriteLine("Mật khẩu chính xác");
                    }
                    else
                    {
                        Console.WriteLine("Mật khẩu không chính xác");
                    }
                }
            }
        }

        // Caesar Cipher Decrypt
        static string Decrypt(string input, int shift)
        {
            string result = "";

            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    char baseChar = char.IsUpper(c) ? 'A' : 'a';
                    char decryptedChar = (char)((c - baseChar - shift + 26) % 26 + baseChar);
                    result += decryptedChar;
                }
                else
                {
                    result += c; // giữ nguyên ký tự không phải chữ cái
                }
            }

            return result;
        }
    }
}
