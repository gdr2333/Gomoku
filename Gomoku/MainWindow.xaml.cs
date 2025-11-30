using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace Gomoku
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public string serverAddr = "";
        public MainWindow()
        {
            InitializeComponent();
            WaitForServerAddr();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var gw = new GameWindow(serverAddr);
            gw.Show();
        }

        private async void WaitForServerAddr()
        {
            using (var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 19472)))
            {
                var msg = await udpClient.ReceiveAsync();
                serverAddr = $"http://{Encoding.UTF8.GetString(msg.Buffer, 2, msg.Buffer.Length - 2)}:{BitConverter.ToInt16(msg.Buffer, 0)}/game";
            }
            StartButton.IsEnabled = true;
            StartButton.Content = "开始匹配";
        }
    }
}
