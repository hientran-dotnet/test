
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace _1.Server
{
    internal class Program
    {
        static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                if (int.TryParse(received, out int seconds))
                {
                    Console.WriteLine($"Received: {seconds} (waiting {seconds} seconds)");
                    for (int i = seconds; i > 0; i--)
                    {
                        Console.Write($"{i} ");
                        await Task.Delay(1000);
                    }
                    Console.WriteLine();
                    string reply = $"Waited {seconds} seconds!";
                    byte[] replyBytes = Encoding.ASCII.GetBytes(reply);
                    await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
                }
                else
                {
                    string reply = "Invalid input";
                    byte[] replyBytes = Encoding.ASCII.GetBytes(reply);
                    await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
                }
            }
        }

        static async Task Main(string[] args)
        {
            int port = 9200;
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Server started on port {port}");
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // fire and forget
            }
        }
    }
}
