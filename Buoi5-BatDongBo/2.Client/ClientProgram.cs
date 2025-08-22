using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
namespace _2.Client
{
    internal class Program
    {
        private static readonly ConcurrentQueue<string> _uploadQueue = new();
        private static readonly ConcurrentDictionary<string, UploadProgress> _activeUploads = new();
        private static volatile bool _isRunning = true;
        private static readonly object _consoleLock = new object();

        public class UploadProgress
        {
            public string FileName { get; set; } = "";
            public long TotalBytes { get; set; }
            public long SentBytes { get; set; }
            public DateTime StartTime { get; set; }
            public string Status { get; set; } = "Starting";
        }

        static void DisplayProgress()
        {
            while (_isRunning)
            {
                lock (_consoleLock)
                {
                    if (_activeUploads.Any())
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.WriteLine(new string(' ', Console.WindowWidth - 1)); // Clear line
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        
                        foreach (var upload in _activeUploads.Values)
                        {
                            double progress = upload.TotalBytes > 0 ? (double)upload.SentBytes / upload.TotalBytes * 100 : 0;
                            var elapsed = DateTime.Now - upload.StartTime;
                            double speedMBps = elapsed.TotalSeconds > 0 ? (upload.SentBytes / (1024.0 * 1024.0)) / elapsed.TotalSeconds : 0;
                            
                            Console.WriteLine($"📤 {upload.FileName}: {progress:F1}% ({upload.SentBytes:N0}/{upload.TotalBytes:N0} bytes) - {speedMBps:F1} MB/s - {upload.Status}");
                        }
                        
                        // Move cursor back to input line
                        Console.Write("Nhập file/folder/pattern (hoặc 'exit' để thoát): ");
                    }
                }
                Thread.Sleep(500); // Update every 500ms
            }
        }
        static async Task<bool> UploadFileAsync(string server, int port, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var uploadId = Guid.NewGuid().ToString();
            
            try
            {
                var progress = new UploadProgress
                {
                    FileName = fileName,
                    TotalBytes = new FileInfo(filePath).Length,
                    SentBytes = 0,
                    StartTime = DateTime.Now,
                    Status = "Connecting"
                };
                
                _activeUploads.TryAdd(uploadId, progress);

                using TcpClient client = new TcpClient();
                await client.ConnectAsync(server, port);
                using var stream = client.GetStream();
                
                progress.Status = "Sending metadata";
                
                // Chuẩn bị tên file (tối đa 256 bytes)
                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                if (nameBytes.Length > 256)
                {
                    progress.Status = "Error: Name too long";
                    return false;
                }
                
                // Tạo buffer 256 bytes và copy tên file vào
                byte[] nameBuffer = new byte[256];
                Array.Copy(nameBytes, nameBuffer, nameBytes.Length);
                
                await stream.WriteAsync(nameBuffer, 0, nameBuffer.Length);
                
                // Gửi kích thước file (8 bytes)
                long fileSize = progress.TotalBytes;
                byte[] sizeBytes = BitConverter.GetBytes(fileSize);
                await stream.WriteAsync(sizeBytes, 0, sizeBytes.Length);
                
                progress.Status = "Uploading";
                
                // Gửi nội dung file
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, read);
                        progress.SentBytes += read;
                    }
                }
                
                // Đảm bảo dữ liệu được gửi hoàn toàn
                await stream.FlushAsync();
                progress.Status = "Waiting response";
                
                // Nhận tên file đã lưu từ server với timeout
                byte[] reply = new byte[256];
                var readTask = stream.ReadAsync(reply, 0, reply.Length);
                var timeoutTask = Task.Delay(10000); // 10 second timeout
                
                var completedTask = await Task.WhenAny(readTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    progress.Status = "Timeout";
                    return false;
                }
                
                int replyLen = await readTask;
                if (replyLen > 0)
                {
                    string savedName = Encoding.UTF8.GetString(reply, 0, replyLen).TrimEnd('\0');
                    progress.Status = $"✓ Complete -> {savedName}";
                    
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"\n✅ {fileName} uploaded successfully as {savedName}");
                        Console.Write("Nhập đường dẫn file (hoặc 'exit' để thoát): ");
                    }
                }
                else
                {
                    progress.Status = "✗ No response";
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                var progress = _activeUploads.GetValueOrDefault(uploadId);
                if (progress != null)
                {
                    progress.Status = $"✗ Error: {ex.Message}";
                }
                
                lock (_consoleLock)
                {
                    Console.WriteLine($"\n❌ Error uploading {fileName}: {ex.Message}");
                    Console.Write("Nhập đường dẫn file (hoặc 'exit' để thoát): ");
                }
                return false;
            }
            finally
            {
                // Remove from active uploads after 3 seconds to show final status
                _ = Task.Delay(3000).ContinueWith(_ => {
                    _activeUploads.TryRemove(uploadId, out var _);
                });
            }
        }

        static (List<string> files, bool needConfirmation) GetFilesFromPath(string inputPath)
        {
            var files = new List<string>();
            bool needConfirmation = false;
            
            try
            {
                if (File.Exists(inputPath))
                {
                    // Đó là file - không cần xác nhận
                    files.Add(inputPath);
                }
                else if (Directory.Exists(inputPath))
                {
                    // Đó là folder - cần xác nhận
                    var directoryFiles = Directory.GetFiles(inputPath, "*", SearchOption.TopDirectoryOnly);
                    files.AddRange(directoryFiles);
                    needConfirmation = true;
                    
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"📁 Found {directoryFiles.Length} files in folder: {Path.GetFileName(inputPath)}");
                        if (directoryFiles.Length > 0)
                        {
                            Console.WriteLine("Files to upload:");
                            var totalSize = 0L;
                            foreach (var file in directoryFiles.Take(10)) // Show first 10 files
                            {
                                var fileInfo = new FileInfo(file);
                                totalSize += fileInfo.Length;
                                Console.WriteLine($"  • {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
                            }
                            if (directoryFiles.Length > 10)
                            {
                                // Calculate remaining files size
                                var remainingFiles = directoryFiles.Skip(10);
                                foreach (var file in remainingFiles)
                                {
                                    totalSize += new FileInfo(file).Length;
                                }
                                Console.WriteLine($"  ... and {directoryFiles.Length - 10} more files");
                            }
                            Console.WriteLine($"📊 Total: {directoryFiles.Length} files, {totalSize:N0} bytes ({totalSize / (1024.0 * 1024.0):F1} MB)");
                            Console.WriteLine("⏰ Press ENTER to start upload, or type 'skip' to cancel:");
                        }
                        else
                        {
                            Console.WriteLine("❌ No files found in folder");
                        }
                    }
                }
                else
                {
                    // Thử pattern matching cho wildcard
                    var directory = Path.GetDirectoryName(inputPath);
                    var fileName = Path.GetFileName(inputPath);
                    
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory) && fileName.Contains("*"))
                    {
                        var matchedFiles = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
                        files.AddRange(matchedFiles);
                        needConfirmation = true;
                        
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"🔍 Pattern '{fileName}' matched {matchedFiles.Length} files");
                            if (matchedFiles.Length > 0)
                            {
                                Console.WriteLine("Files to upload:");
                                var totalSize = 0L;
                                foreach (var file in matchedFiles.Take(10)) // Show first 10 files
                                {
                                    var fileInfo = new FileInfo(file);
                                    totalSize += fileInfo.Length;
                                    Console.WriteLine($"  • {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
                                }
                                if (matchedFiles.Length > 10)
                                {
                                    // Calculate remaining files size
                                    var remainingFiles = matchedFiles.Skip(10);
                                    foreach (var file in remainingFiles)
                                    {
                                        totalSize += new FileInfo(file).Length;
                                    }
                                    Console.WriteLine($"  ... and {matchedFiles.Length - 10} more files");
                                }
                                Console.WriteLine($"📊 Total: {matchedFiles.Length} files, {totalSize:N0} bytes ({totalSize / (1024.0 * 1024.0):F1} MB)");
                                Console.WriteLine("⏰ Press ENTER to start upload, or type 'skip' to cancel:");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine($"❌ Error processing path '{inputPath}': {ex.Message}");
                }
            }
            
            return (files, needConfirmation);
        }

        static async Task ProcessUploadQueue()
        {
            while (_isRunning)
            {
                if (_uploadQueue.TryDequeue(out string? inputPath) && !string.IsNullOrEmpty(inputPath))
                {
                    var (files, needConfirmation) = GetFilesFromPath(inputPath);
                    
                    if (files.Any())
                    {
                        if (needConfirmation)
                        {
                            // Wait for user confirmation
                            string? confirmation = null;
                            lock (_consoleLock)
                            {
                                confirmation = Console.ReadLine();
                            }
                            
                            if (!string.IsNullOrEmpty(confirmation) && confirmation.Trim().ToLower() == "skip")
                            {
                                lock (_consoleLock)
                                {
                                    Console.WriteLine("❌ Upload cancelled by user");
                                    Console.Write("Nhập file/folder/pattern (hoặc 'exit' để thoát): ");
                                }
                                continue;
                            }
                        }
                        
                        // Start uploads
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"🚀 Starting upload of {files.Count} files...");
                        }
                        
                        foreach (var filePath in files)
                        {
                            // Start upload in background
                            _ = UploadFileAsync("127.0.0.1", 8080, filePath);
                            
                            // Small delay between starting uploads to avoid overwhelming
                            await Task.Delay(50);
                        }
                    }
                    else
                    {
                        lock (_consoleLock)
                        {
                            Console.WriteLine($"\n❌ No files found at: {inputPath}");
                            Console.Write("Nhập file/folder/pattern (hoặc 'exit' để thoát): ");
                        }
                    }
                }
                await Task.Delay(100); // Small delay to prevent busy waiting
            }
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Nhập đường dẫn folder: ");
            Console.WriteLine();

            // Start background tasks
            var progressTask = Task.Run(DisplayProgress);
            var queueTask = Task.Run(ProcessUploadQueue);

            // Input loop
            while (_isRunning)
            {
                
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                    
                if (input.Trim().ToLower() == "exit")
                {
                    _isRunning = false;
                    break;
                }

                // Add to upload queue
                _uploadQueue.Enqueue(input.Trim());
                
                lock (_consoleLock)
                {
                    if (Directory.Exists(input.Trim()))
                    {
                        Console.WriteLine($"✅ Added folder to queue: {Path.GetFileName(input.Trim())}");
                    }
                    else if (input.Trim().Contains("*"))
                    {
                        Console.WriteLine($"✅ Added pattern to queue: {Path.GetFileName(input.Trim())}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Added file to queue: {Path.GetFileName(input.Trim())}");
                    }
                }
            }

            // Wait for remaining uploads to complete
            Console.WriteLine("\n⏳ Waiting for remaining uploads to complete...");
            
            // Wait up to 30 seconds for uploads to finish
            var timeout = DateTime.Now.AddSeconds(30);
            while (_activeUploads.Any() && DateTime.Now < timeout)
            {
                await Task.Delay(1000);
            }
            
            if (_activeUploads.Any())
            {
                Console.WriteLine($"⚠️  {_activeUploads.Count} uploads still in progress, exiting anyway...");
            }

            Console.WriteLine("👋 Client exited. Thank you!");
        }
    }
}
