using System.Net.Sockets;

namespace _1.Client
{
    internal class ClientProgram
    {
        static async Task ConnectAsync(string server, string message)
        {
            try
            {
                int port = 9200;
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(server, port);
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"Sent: {message}");
                data = new Byte[256];
                int bytes = await stream.ReadAsync(data, 0, data.Length);
                string responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine($"Received: {responseData}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
            }
        }



        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter numbers to send to server (type 'exit' to quit):");
            while (true)
            {
                Console.Write("input = ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Trim().ToLower() == "exit")
                    break;
                _ = ConnectAsync("127.0.0.1", input); // fire and forget, không chờ
            }
            Console.WriteLine("Client exited.");
        }
    }
}
