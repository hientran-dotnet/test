using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class MyTcpListener
{

    // Hàm chuyển đổi số thành chữ
    static string ConvertNumberToWords(string number)
    {
        Dictionary<string, string> numberWords = new Dictionary<string, string>
            {
                {"0", "không"}, {"1", "một"}, {"2", "hai"}, {"3", "ba"},
                {"4", "bốn"}, {"5", "năm"}, {"6", "sáu"}, {"7", "bảy"},
                {"8", "tám"}, {"9", "chín"}
            };

        if (numberWords.ContainsKey(number))
            return numberWords[number];
        else
            return "Số không hợp lệ";
    }

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8; // Đặt mã hóa đầu ra để hỗ trợ tiếng Việt

        // 1. Tạo server socket
        TcpListener server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Console.WriteLine("Server đang chạy trên port 8080...");

        while (true)
        {
            // 2. Chấp nhận kết nối từ client
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client đã kết nối!");

            // 3. Nhận dữ liệu từ client
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string receivedNumber = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // 4. Chuyển đổi số thành chữ
            string numberInWords = ConvertNumberToWords(receivedNumber.Trim());

            // 5. Gửi kết quả về client
            byte[] response = Encoding.UTF8.GetBytes(numberInWords);
            stream.Write(response, 0, response.Length);

            // 6. Đóng kết nối
            client.Close();
            Console.WriteLine($"Đã xử lý: {receivedNumber} -> {numberInWords}");
        }
    }
}