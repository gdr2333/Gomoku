using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Server;

public class IPBroadcastService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new UdpClient(new IPEndPoint(IPAddress.Any, 19471));
        string ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(addr => addr.AddressFamily == AddressFamily.InterNetwork && !Regex.IsMatch(addr.ToString(), @"127\.\d+\.\d+\.\d+")).First().ToString();
        byte[] message;
        {
            // 计算要发送的消息
            // |--端口号（2字节）--|--IP地址（变长，UTF8）--|
            byte[] ipBytes = Encoding.UTF8.GetBytes(ip);
            message = new byte[ipBytes.Length + 2];
            Array.Copy(BitConverter.GetBytes(5073), message, 2);
            Array.Copy(ipBytes, 0, message, 2, ipBytes.Length);
        }
        while (true)
        {
            await Task.Delay(1000, stoppingToken);
            await client.SendAsync(message, message.Length, "255.255.255.255", 19472);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
