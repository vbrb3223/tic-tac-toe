using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace KN
{
    /// <summary>
    /// Логика взаимодействия для Game.xaml
    /// </summary>
    public partial class Game : Page
    {
        Task task;


        public Game()
        {
            InitializeComponent();
            task = new Task(Listen);
        }

        public static string yourNickname;
        public static string enemiesNickname;
        public static string yourFig;
        public static string enemiesFig;
        public static bool CanGo;
        public static bool GameOver = false;
        public static Socket socket;
        public static EndPoint remotePoint;
        public static Rectangle[] rects;



        public void getValues(string yN, string eN, string yF, string eF, Socket socketPlayer, EndPoint remoteAddress)
        {
            yourNickname = yN;
            enemiesNickname = eN;
            yourFig = yF;
            enemiesFig = eF;
            socket = socketPlayer;
            remotePoint = remoteAddress;
            if (yF == "x")
                CanGo = true;
            else
                CanGo = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            yourNick.Content = yourNickname;
            EnemiesNick.Content = enemiesNickname;
            Image ImageContainer = new Image();
            ImageSource image = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + yourFig + ".png", UriKind.Absolute));
            ImageContainer.Source = image;
            YourFig.Fill = new ImageBrush
            {
                ImageSource = image
            };

            if (enemiesFig != "")
            {
                ImageContainer = new Image();
                image = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + enemiesFig + ".png", UriKind.Absolute));
                ImageContainer.Source = image;
                EnemiesFig.Fill = new ImageBrush
                {
                    ImageSource = image
                };
            }
            rects = new Rectangle[] { d00, d01, d02, d10, d11, d12, d20, d21, d22 };
            foreach (var rect in rects)
            {
                rect.MouseDown += Rect_MouseDown;
            }
            task.Start();
            task.GetAwaiter();
        }

        private void textBoxMessage_GotFocus(object sender, RoutedEventArgs e)
        {
            if (textBoxMessage.Text == "Введите сообщение...")
                textBoxMessage.Text = "";
        }

        private void textBoxMessage_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxMessage.Text))
                textBoxMessage.Text = "Введите сообщение...";
        }

        private void sendMessagebtn_Click(object sender, RoutedEventArgs e)
        {
            SendBTN();
        }

        private void SendBTN()
        {
            if (textBoxMessage.Text != "Введите сообщение..." && !string.IsNullOrEmpty(textBoxMessage.Text))
            {
                richText.AppendText("<Вы> " + textBoxMessage.Text + "\n");
                SendMessageEnemy(textBoxMessage.Text);
                textBoxMessage.Text = "";
            }
        }

        private void SendMessageEnemy(string msg)
        {
            byte[] data = new byte[256];
            data = Encoding.Unicode.GetBytes("command__SendMessage@" + msg);
            socket.SendTo(data, remotePoint);
        }

        private void Move(string xy)
        {
            byte[] data = new byte[256];
            data = Encoding.Unicode.GetBytes("command__Move@" + xy);
            socket.SendTo(data, remotePoint);
        }

        private void Listen()
        {
            byte[] data = new byte[256];
            int bytes;
            StringBuilder builder = new StringBuilder();
            while(true)
            {
                bytes = socket.ReceiveFrom(data, ref remotePoint);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                CheckMessage(builder.ToString());
                builder.Clear();
            }
        }

        private void CheckMessage(string msg)
        {
            string command = msg.Substring(0, msg.IndexOf('@'));
            msg = msg.Substring(msg.IndexOf('@') + 1, msg.Length - msg.IndexOf('@') - 1);
            switch (command)
            {
                case "command__ChatMessage":
                    ChatAddMessage(msg);
                    break;
                case "command__enemyJoin":
                    JoinEnemy(msg);
                    break;
                case "command__Move":
                    EnemyMove(msg);
                    break;
                case "command__IsWinner":
                    CheckWinner(msg);
                    break;
            }
        }

        private void CheckWinner(string msg)
        {
            GameOver = true;
            switch (msg)
            {
                case "true":
                    Winner();
                    break;
                case "false":
                    Loser();
                    break;
                case "draw":
                    Draw();
                    break;
            }
        }

        private void Winner()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                LBMessage.Content = "Вы победили";
            }));
        }

        private void Loser()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                LBMessage.Content = "Вы проиграли";
            }));
        }

        private void Draw()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                LBMessage.Content = "Ничья";
            }));
        }

        private void EnemyMove(string msg)
        {
            string xy = msg.Split(';')[0];
            string fig = msg.Split(';')[1];
            this.Dispatcher.Invoke(new Action(() =>
            {
            
                Rectangle currentRect = rects.Where(u => u.Name.ToString() == "d" + xy).FirstOrDefault();
                Image ImageContainer = new Image();
                ImageSource image = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + fig + ".png", UriKind.Absolute));
                ImageContainer.Source = image;
                currentRect.Fill = new ImageBrush
                {
                    ImageSource = image
                };
                LBMessage.Content = "Ваш ход";
            }));
            CanGo = true;
        }

        public void ChatAddMessage(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                richText.AppendText("<" + EnemiesNick.Content + ">" + msg + "\n");
            })); 
        }

        private void JoinEnemy(string nick)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                EnemiesNick.Content = nick;
            }));

            this.Dispatcher.Invoke(new Action(() =>
            {
                //Загружаем фигуру противника
                switch (yourFig)
                {
                    case "x":
                        Image ImageContainer = new Image();
                        ImageSource image = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + "0.png", UriKind.Absolute));
                        ImageContainer.Source = image;
                        EnemiesFig.Fill = new ImageBrush
                        {
                            ImageSource = image
                        };
                        break;
                    default:
                        Image ImageContainerDef = new Image();
                        ImageSource imageDef = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + "x.png", UriKind.Absolute));
                        ImageContainerDef.Source = imageDef;
                        EnemiesFig.Fill = new ImageBrush
                        {
                            ImageSource = imageDef
                        };
                        break;
                }
            }));
        }

        private void textBoxMessage_KeyDown_1(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                textBoxMessage.Text += "\r\n";
                textBoxMessage.CaretIndex = textBoxMessage.Text.Length - 1;
            }
            else if (Key.Enter == e.Key)
            {
                SendBTN();
            }
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!GameOver)
            {
                Rectangle currentRectangle = (Rectangle)sender;
                string name = currentRectangle.Name;
                if (CanGo && currentRectangle.Fill.ToString() == "#FFF4F4F5")
                {
                    string xy = "" + name[1] + name[2];
                    LBMessage.Content = "";
                    Image ImageContainer = new Image();
                    ImageSource image = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\media" + @"\" + yourFig + ".png", UriKind.Absolute));
                    ImageContainer.Source = image;
                    currentRectangle.Fill = new ImageBrush
                    {
                        ImageSource = image
                    };
                    Move(xy);
                    CanGo = false;
                }
                else if (!CanGo)
                    LBMessage.Content = "Сейчас ходит противник";
                else if (!(currentRectangle.Fill.ToString() == "#FFF4F4F5"))
                    LBMessage.Content = "Клетка занята";
            }
        }
    }
}
