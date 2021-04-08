using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace KN
{
    /// <summary>
    /// Логика взаимодействия для Connect.xaml
    /// </summary>
    public partial class Connect : Page
    {
        public Connect()
        {
            InitializeComponent();
        }

        private void IPtb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IPtb.Text == "IP-адрес")
                IPtb.Text = "";
        }

        private void Porttb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Porttb.Text == "Порт")
                Porttb.Text = "";
        }

        private void Porttb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Porttb.Text))
                Porttb.Text = "Порт";
        }

        private void IPtb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(IPtb.Text))
                IPtb.Text = "IP-адрес";
        }

        EndPoint remotePoint;
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EndPoint localIP;
        string nick;

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            nick = Nicknametb.Text;

            Regex optionIP = new Regex(@"^\d+\.\d+\.\d+\.\d+$");
            Regex optionPort = new Regex(@"^\d+$");

            if (optionIP.IsMatch(IPtb.Text) && optionPort.IsMatch(Porttb.Text) && Convert.ToInt32(Porttb.Text) <= 65535)
            {
                localIP = new IPEndPoint(IPAddress.Any, 0);
                remotePoint = new IPEndPoint(IPAddress.Parse(IPtb.Text), Convert.ToInt32(Porttb.Text));
                Listen();
            }
            else
                MessageBox.Show("IP-адрес или Порт неверного формата!");
        }

        private void Listen()
        {
            try
            {
                if (socket == null)
                    socket.Bind(localIP);

                //Запрос на подключение
                SendRequest("command__AddPlayer@" + nick);

                StringBuilder stringBuilder = new StringBuilder();
                int bytes;
                byte[] data = new byte[256];

                do
                {
                    bytes = socket.ReceiveFrom(data, ref remotePoint);
                    stringBuilder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);
                if (CheckAnswer(stringBuilder.ToString()))
                {
                    goNextPage();
                }

            }
            catch
            {
                MessageBox.Show("Не удалось подключиться!");
            }
        }

        private void goNextPage()
        {
            string yN = Nicknametb.Text;

            StringBuilder builder = new StringBuilder();
            SendRequest("command__GiveInfoNextPage@go");
            do
            {
                byte[] data = new byte[256];
                int bytes = socket.ReceiveFrom(data, ref remotePoint);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (socket.Available > 0);
            if (builder.ToString().Split('@')[0] == "command__infoNextPage")
            {
                var info = builder.ToString().Split('@');
                Game game = new Game();
                if (info.Length > 2)
                    game.getValues(yN, info[2], info[1], info[3], socket, remotePoint);
                else
                    game.getValues(yN, "Ожидаем соперника...", info[1], "", socket, remotePoint);

                File.WriteAllText("tempData", IPtb.Text);
                Navigation.frame.Navigate(game);
            }
        }

        private void SendRequest(string msg)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes(msg);
                socket.SendTo(data, remotePoint);
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к серверу!");
            }
        }

        private bool CheckAnswer(string answer)
        {
            if (answer == "command__trueJoin@" + nick)
                return true;
            else
                return false;
        }

        private void Nicknametb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Nicknametb.Text == "Никнейм")
                Nicknametb.Text = "";
        }

        private void Nicknametb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Nicknametb.Text))
                Nicknametb.Text = "Никнейм";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tempIP.Content = File.ReadAllText("tempData") != "" ? File.ReadAllText("tempData") : "";
        }

        private void tempIP_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IPtb.Text = Convert.ToString(tempIP.Content);
        }
    }
}
