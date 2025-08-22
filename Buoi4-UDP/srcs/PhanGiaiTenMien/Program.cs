using System.Net;
using System.Text;

namespace PhanGiaiTenMien
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write("Nhập tên miền cần phân giải: ");
            string domain = Console.ReadLine();

            try
            {
                // Gọi DNS phân giải
                IPHostEntry hostEntry = Dns.GetHostEntry(domain);

                Console.WriteLine($"Tên miền: {hostEntry.HostName}");
                Console.WriteLine("Địa chỉ IP:");

                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    Console.WriteLine($" - {ip}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
            }
        }
    }
}
