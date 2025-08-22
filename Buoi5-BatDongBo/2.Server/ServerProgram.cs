using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;
using System.Collections.Concurrent;
namespace _2.Server
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<string, int> _activeConnections = new();
        private static long _totalFilesReceived = 0;
        private static long _totalBytesReceived = 0;

        static string GetVietnamTimeFileName(string originalExt)
        {
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            return vnTime.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + originalExt;
        }

        static void LogServerStats()
        {
            Console.WriteLine($"[STATS] Active connections: {_activeConnections.Count}, Total files: {_totalFilesReceived}, Total bytes: {_totalBytesReceived:N0}");
        }

        // Helper method để đảm bảo đọc đủ bytes
        static async Task<int> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new EndOfStreamException("Connection closed before reading all data");
                totalRead += read;
            }
            return totalRead;
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            _activeConnections.TryAdd(clientEndpoint, 1);
            
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var startTime = DateTime.Now;
                    
                    // Đọc tên file gốc (dài tối đa 256 bytes, gửi trước)
                    byte[] nameBuffer = new byte[256];
                    await ReadExactlyAsync(stream, nameBuffer, 0, nameBuffer.Length);
                    
                    // Tìm null terminator và chỉ lấy phần tên file thực tế
                    int nullIndex = Array.IndexOf(nameBuffer, (byte)0);
                    string originalName = nullIndex >= 0 
                        ? Encoding.UTF8.GetString(nameBuffer, 0, nullIndex) 
                        : Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0');
                    
                    if (string.IsNullOrWhiteSpace(originalName))
                    {
                        Console.WriteLine($"[{clientEndpoint}] Invalid file name received");
                        return;
                    }

                    string ext = System.IO.Path.GetExtension(originalName);
                    string saveName = GetVietnamTimeFileName(ext);
                    string savePath = Path.Combine(Directory.GetCurrentDirectory(), saveName);

                    // Đọc file size (8 bytes, Int64)
                    byte[] sizeBuffer = new byte[8];
                    await ReadExactlyAsync(stream, sizeBuffer, 0, 8);
                    long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

                    if (fileSize <= 0)
                    {
                        Console.WriteLine($"[{clientEndpoint}] Invalid file size received: {fileSize}");
                        return;
                    }

                    Console.WriteLine($"[{clientEndpoint}] Receiving: {originalName} ({fileSize:N0} bytes)");

                    // Nhận file
                    long received = 0;
                    byte[] buffer = new byte[81920]; // 80KB buffer
                    using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        while (received < fileSize)
                        {
                            int toRead = (int)Math.Min(buffer.Length, fileSize - received);
                            int read = await stream.ReadAsync(buffer, 0, toRead);
                            if (read == 0) 
                            {
                                Console.WriteLine($"[{clientEndpoint}] Connection closed unexpectedly. Received {received:N0}/{fileSize:N0} bytes");
                                break;
                            }
                            await fs.WriteAsync(buffer, 0, read);
                            received += read;
                        }
                    }

                    var endTime = DateTime.Now;
                    var duration = endTime - startTime;

                    if (received == fileSize)
                    {
                        Interlocked.Increment(ref _totalFilesReceived);
                        Interlocked.Add(ref _totalBytesReceived, fileSize);
                        
                        double speedMBps = fileSize / (1024.0 * 1024.0) / duration.TotalSeconds;
                        Console.WriteLine($"[{clientEndpoint}] ✓ {originalName} -> {saveName} ({fileSize:N0} bytes, {duration.TotalSeconds:F1}s, {speedMBps:F1} MB/s)");
                        
                        // Gửi lại tên file đã lưu
                        byte[] reply = Encoding.UTF8.GetBytes(saveName);
                        await stream.WriteAsync(reply, 0, reply.Length);
                        
                        LogServerStats();
                    }
                    else
                    {
                        Console.WriteLine($"[{clientEndpoint}] ✗ Transfer incomplete: {received:N0}/{fileSize:N0} bytes");
                        // Xóa file không hoàn chỉnh
                        if (File.Exists(savePath))
                            File.Delete(savePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{clientEndpoint}] Error: {ex.Message}");
            }
            finally
            {
                _activeConnections.TryRemove(clientEndpoint, out _);
                Console.WriteLine($"[{clientEndpoint}] Disconnected");
            }
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; // Đảm bảo hiển thị tiếng Việt đúng
            int port = 8080;
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            
            Console.WriteLine("=== Multi-Client File Upload Server ===");
            Console.WriteLine($"Server started on port {port}");
            Console.WriteLine($"Files will be saved to: {Directory.GetCurrentDirectory()}");
            Console.WriteLine("Press Ctrl+C to stop the server\n");
            
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                Console.WriteLine($"[{remoteEndPoint}] Connected (Active: {_activeConnections.Count + 1})");
                
                // Handle each client asynchronously without waiting
                _ = HandleClientAsync(client);
            }
        }
    }
}
