using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DHCPClientDemo
{
    public class DHCPClient
    {
        private const int DHCP_CLIENT_PORT = 68;
        private const int DHCP_SERVER_PORT = 67;
        private const int DHCP_PACKET_SIZE = 548;

        // DHCP Message Types
        private const byte DHCP_DISCOVER = 1;
        private const byte DHCP_OFFER = 2;
        private const byte DHCP_REQUEST = 3;
        private const byte DHCP_ACK = 5;

        // DHCP Options
        private const byte OPTION_DHCP_MESSAGE_TYPE = 53;
        private const byte OPTION_REQUESTED_IP = 50;
        private const byte OPTION_SERVER_IDENTIFIER = 54;
        private const byte OPTION_SUBNET_MASK = 1;
        private const byte OPTION_ROUTER = 3;
        private const byte OPTION_DNS = 6;
        private const byte OPTION_END = 255;

        private UdpClient udpClient;
        private byte[] clientMacAddress;
        private uint transactionId;
        private Random random;

        public DHCPClient()
        {
            random = new Random();
            transactionId = (uint)random.Next();
            clientMacAddress = GetMacAddress();
        }

        public async Task<DHCPResponse> RequestIPAddress()
        {
            try
            {
                Console.WriteLine("Bắt đầu quá trình DHCP...");

                // Tạo UDP client để lắng nghe trên port 68
                udpClient = new UdpClient(DHCP_CLIENT_PORT);
                udpClient.EnableBroadcast = true;

                // Bước 1: Gửi DHCP Discover
                Console.WriteLine("1. Gửi DHCP Discover...");
                await SendDHCPDiscover();

                // Bước 2: Nhận DHCP Offer
                Console.WriteLine("2. Chờ DHCP Offer...");
                var offer = await ReceiveDHCPMessage();
                if (offer == null || offer.MessageType != DHCP_OFFER)
                {
                    Console.WriteLine("Không nhận được DHCP Offer!");
                    return null;
                }

                Console.WriteLine($"Nhận được DHCP Offer: IP = {offer.OfferedIP}");

                // Bước 3: Gửi DHCP Request
                Console.WriteLine("3. Gửi DHCP Request...");
                await SendDHCPRequest(offer.OfferedIP, offer.ServerIP);

                // Bước 4: Nhận DHCP ACK
                Console.WriteLine("4. Chờ DHCP ACK...");
                var ack = await ReceiveDHCPMessage();
                if (ack == null || ack.MessageType != DHCP_ACK)
                {
                    Console.WriteLine("Không nhận được DHCP ACK!");
                    return null;
                }

                Console.WriteLine("Nhận được DHCP ACK - Hoàn tất!");
                return ack;
            }
            finally
            {
                udpClient?.Close();
            }
        }

        private async Task SendDHCPDiscover()
        {
            var packet = CreateDHCPPacket(DHCP_DISCOVER);
            await SendPacket(packet);
        }

        private async Task SendDHCPRequest(IPAddress requestedIP, IPAddress serverIP)
        {
            var packet = CreateDHCPPacket(DHCP_REQUEST, requestedIP, serverIP);
            await SendPacket(packet);
        }

        private byte[] CreateDHCPPacket(byte messageType, IPAddress requestedIP = null, IPAddress serverIP = null)
        {
            var packet = new byte[DHCP_PACKET_SIZE];
            var index = 0;

            // DHCP Header
            packet[index++] = 0x01; // Message type: Boot Request
            packet[index++] = 0x01; // Hardware type: Ethernet
            packet[index++] = 0x06; // Hardware address length: 6 bytes
            packet[index++] = 0x00; // Hops

            // Transaction ID (4 bytes)
            BitConverter.GetBytes(transactionId).CopyTo(packet, index);
            index += 4;

            // Seconds (2 bytes) và Flags (2 bytes)
            index += 4;

            // Client IP, Your IP, Server IP, Gateway IP (16 bytes total)
            index += 16;

            // Client MAC Address (16 bytes, padded)
            clientMacAddress.CopyTo(packet, index);
            index += 16;

            // Server name (64 bytes) và Boot filename (128 bytes)
            index += 192;

            // Magic Cookie
            packet[index++] = 0x63;
            packet[index++] = 0x82;
            packet[index++] = 0x53;
            packet[index++] = 0x63;

            // DHCP Options
            packet[index++] = OPTION_DHCP_MESSAGE_TYPE;
            packet[index++] = 1;
            packet[index++] = messageType;

            if (requestedIP != null)
            {
                packet[index++] = OPTION_REQUESTED_IP;
                packet[index++] = 4;
                requestedIP.GetAddressBytes().CopyTo(packet, index);
                index += 4;
            }

            if (serverIP != null)
            {
                packet[index++] = OPTION_SERVER_IDENTIFIER;
                packet[index++] = 4;
                serverIP.GetAddressBytes().CopyTo(packet, index);
                index += 4;
            }

            // End option
            packet[index] = OPTION_END;

            return packet;
        }

        private async Task SendPacket(byte[] packet)
        {
            var endPoint = new IPEndPoint(IPAddress.Broadcast, DHCP_SERVER_PORT);
            await udpClient.SendAsync(packet, packet.Length, endPoint);
        }

        private async Task<DHCPResponse> ReceiveDHCPMessage()
        {
            try
            {
                var timeoutTask = Task.Delay(10000); // 10 seconds timeout
                var receiveTask = udpClient.ReceiveAsync();

                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("Timeout khi chờ phản hồi DHCP");
                    return null;
                }

                var result = await receiveTask;
                return ParseDHCPMessage(result.Buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi nhận gói tin DHCP: {ex.Message}");
                return null;
            }
        }

        private DHCPResponse ParseDHCPMessage(byte[] packet)
        {
            if (packet.Length < 240) return null;

            var response = new DHCPResponse();

            // Kiểm tra Transaction ID
            var receivedTransactionId = BitConverter.ToUInt32(packet, 4);
            if (receivedTransactionId != transactionId) return null;

            // Your IP Address (địa chỉ IP được cấp)
            response.OfferedIP = new IPAddress(new byte[] { packet[16], packet[17], packet[18], packet[19] });

            // Server IP Address
            response.ServerIP = new IPAddress(new byte[] { packet[20], packet[21], packet[22], packet[23] });

            // Parse DHCP Options
            var optionsStart = 240; // Sau magic cookie
            for (var i = optionsStart; i < packet.Length - 1; i++)
            {
                var option = packet[i];
                if (option == OPTION_END) break;
                if (option == 0) continue; // Padding

                var length = packet[++i];

                switch (option)
                {
                    case OPTION_DHCP_MESSAGE_TYPE:
                        response.MessageType = packet[i + 1];
                        break;
                    case OPTION_SUBNET_MASK:
                        response.SubnetMask = new IPAddress(new byte[] { packet[i + 1], packet[i + 2], packet[i + 3], packet[i + 4] });
                        break;
                    case OPTION_ROUTER:
                        response.Gateway = new IPAddress(new byte[] { packet[i + 1], packet[i + 2], packet[i + 3], packet[i + 4] });
                        break;
                    case OPTION_DNS:
                        response.DNS = new IPAddress(new byte[] { packet[i + 1], packet[i + 2], packet[i + 3], packet[i + 4] });
                        break;
                }

                i += length;
            }

            return response;
        }

        private byte[] GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up)
                        {
                            return nic.GetPhysicalAddress().GetAddressBytes();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy MAC address: {ex.Message}");
            }

            // Fallback: tạo MAC address giả
            var mac = new byte[6];
            random.NextBytes(mac);
            mac[0] = (byte)(mac[0] & 0xFE); // Clear multicast bit
            mac[0] = (byte)(mac[0] | 0x02); // Set local bit
            return mac;
        }
    }

    public class DHCPResponse
    {
        public IPAddress OfferedIP { get; set; }
        public IPAddress ServerIP { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress Gateway { get; set; }
        public IPAddress DNS { get; set; }
        public byte MessageType { get; set; }

        public void DisplayInfo()
        {
            Console.WriteLine("\n=== Thông tin IP nhận được ===");
            Console.WriteLine($"IP Address: {OfferedIP}");
            Console.WriteLine($"Server IP: {ServerIP}");
            Console.WriteLine($"Subnet Mask: {SubnetMask}");
            Console.WriteLine($"Gateway: {Gateway}");
            Console.WriteLine($"DNS Server: {DNS}");
            Console.WriteLine($"Message Type: {MessageType}");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== DHCP Client Demo ===");
            Console.WriteLine("Lưu ý: Cần chạy với quyền Administrator");
            Console.WriteLine();

            try
            {
                var dhcpClient = new DHCPClient();
                var response = await dhcpClient.RequestIPAddress();

                if (response != null)
                {
                    response.DisplayInfo();
                    Console.WriteLine("\nDHCP Client hoàn tất thành công!");
                }
                else
                {
                    Console.WriteLine("\nDHCP Client thất bại!");
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
            {
                Console.WriteLine("Lỗi: Cần chạy ứng dụng với quyền Administrator để bind vào port 68!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
            }

            Console.WriteLine("\nNhấn phím bất kỳ để thoát...");
            Console.ReadKey();
        }
    }
}