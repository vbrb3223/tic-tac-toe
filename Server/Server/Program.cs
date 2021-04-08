using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.ComponentModel;
using System.Windows;

namespace Server
{
    class Program
    {
        //Порт сервера
        static int localPort = 4004;
        //Сокет сервера
        static Socket listeningSocket;
        static string serverIP;

        static string[,] LstUsedRects = new string[3,3];

        static void Main(string[] args)
        {
            try
            {
                Console.Write("Введите ip адрес сервера: ");
                serverIP = Console.ReadLine();
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //Пассивное прослушивание сообщений игроков
                Task listeningTask = new Task(Listen);
                listeningTask.Start();
                Console.WriteLine("Сервер запущен!");

                //Заполнение поля единицами (1 - пустая ячейка)
                for (int i = 0; i < LstUsedRects.GetLength(0); i++)
                    for (int j = 0; j < LstUsedRects.GetLength(1); j++)
                        LstUsedRects[i, j] = "1";


                while (true)
                {
                    if (Console.ReadLine() == "CloseServer")
                    {
                        listeningSocket.Shutdown(SocketShutdown.Both);
                        listeningSocket.Close();
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
            }
        }

        private static void Listen()
        {
            try
            {
                //Адрес сервера
                IPEndPoint localIP = new IPEndPoint(IPAddress.Parse(serverIP), localPort);
                Console.WriteLine("Адрес сервера: " + localIP.ToString());
                //Присваиваем адрес сервера сокету
                listeningSocket.Bind(localIP);

                while (true)
                {
                    //Получаем сообщение
                    StringBuilder builder = new StringBuilder();
                    //Количество байт
                    int bytes = 0;
                    //Байты для данных
                    byte[] data = new byte[256];

                    //Адрес, с которого пришли данные
                    EndPoint remoteIP = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        //Получаем сообщение
                        bytes = listeningSocket.ReceiveFrom(data, ref remoteIP);
                        //Строим сообщение
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (listeningSocket.Available > 0);
                    //Получаем данные о подключении
                    IPEndPoint remoteFullIP = remoteIP as IPEndPoint;
                    CheckMessage(remoteFullIP, builder.ToString());
                    builder.Clear();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            { 
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
            }
        }

        private static void CheckMessage(IPEndPoint remoteFullIP, string msg)
        {
            string command = msg.Substring(0, msg.IndexOf('@'));
            msg = msg.Substring(msg.IndexOf('@') + 1, msg.Length - msg.IndexOf('@') - 1);
            switch(command)
            {
                case "command__AddPlayer":
                    AddPlayer(remoteFullIP, msg);
                    break;
                case "command__SendMessage":
                    SendMessage(remoteFullIP, msg);
                    break;
                case "command__Move":
                    Move(remoteFullIP, msg);
                    break;
                case "command__GiveInfoNextPage":
                    GiveInfoNextPage(remoteFullIP);
                    break;
            }
        }

        private static void GiveInfoNextPage(IPEndPoint playerFullIP)
        {
            string infoEnemy = "";
            char infoPlayer='g';
            foreach (var player in Players.playersList)
            {
                if (player.IPEndPoint.ToString() == playerFullIP.ToString())
                {
                    infoPlayer = player.figure;
                }
                else
                    infoEnemy = "@" + player.Nickname.ToString() + "@" + player.figure.ToString();
            }
            byte[] data = Encoding.Unicode.GetBytes("command__infoNextPage@" + infoPlayer + infoEnemy);
            listeningSocket.SendTo(data, playerFullIP);
            Console.WriteLine("Данные отправлены игроку!");
        }

        private static void GetWinner(string fig, bool IsDraw)
        {
            try
            {
                byte[] data = new byte[256];
                if (!IsDraw)
                {
                    if (fig != "1")
                    {
                        Console.WriteLine("" + char.Parse(fig));
                        var IPWinner = Players.playersList.Where(u => u.figure == char.Parse(fig)).FirstOrDefault().IPEndPoint;
                        foreach (var player in Players.playersList)
                        {
                            if (player.IPEndPoint == IPWinner)
                                data = Encoding.Unicode.GetBytes("command__IsWinner@true");
                            else
                                data = Encoding.Unicode.GetBytes("command__IsWinner@false");
                            listeningSocket.SendTo(data, player.IPEndPoint);
                        }
                    }
                }
                else
                {
                    foreach (var player in Players.playersList)
                    {
                        data = Encoding.Unicode.GetBytes("command__IsWinner@draw");
                        listeningSocket.SendTo(data, player.IPEndPoint);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Связь с игроками потеряна!");
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
            }
        }

        private static void Move(IPEndPoint playerFullIP, string xy)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes("command__Move@" + xy + ";" + Players.playersList.Where(u => u.IPEndPoint.ToString() == playerFullIP.ToString()).FirstOrDefault().figure);
                listeningSocket.SendTo(data, Players.playersList.Where(u => u.IPEndPoint.ToString() != playerFullIP.ToString()).FirstOrDefault().IPEndPoint);
                LstUsedRects[int.Parse(Convert.ToString(xy[0])), int.Parse(Convert.ToString(xy[1]))] = "" + Players.playersList.Where(u => u.IPEndPoint.ToString() == playerFullIP.ToString()).FirstOrDefault().figure;
                CheckWinner();
            }
            catch
            {
                Console.WriteLine("Связь с игроками потеряна!");
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
            }
        }

        private static void CheckWinner()
        {
            //Проверка одинаковых фигур по диагоналям
            if (LstUsedRects[0, 0] != "1" && LstUsedRects[0,0] == LstUsedRects[1,1] && LstUsedRects[1, 1] == LstUsedRects[2, 2])
            {
                GetWinner(LstUsedRects[0, 0], false);
            }
            else if (LstUsedRects[2, 0] != "1" && LstUsedRects[2, 0] == LstUsedRects[1, 1] && LstUsedRects[0, 2] == LstUsedRects[1, 1])
            {
                GetWinner(LstUsedRects[2, 0], false);
            }
            //Проверка одинаковых фигур по вертикали и горизонтали
            else
            {
                bool win = true;
                //Проверка одинаковых фигур по гозиронтали
                for (int i = 0; i < LstUsedRects.GetLength(0); i++)
                {
                    win = true;
                    string firstFig = LstUsedRects[i, 0];
                    for (int j = 1; j < LstUsedRects.GetLength(1); j++)
                    {
                        if (LstUsedRects[i, j] != firstFig)
                            win = false;
                    }
                    if (win)
                    {
                        GetWinner(firstFig, false);
                        return;
                    }
                }
                
                //Проверка одинаковых по горизонтали
                for (int i = 0; i < LstUsedRects.GetLength(1); i++)
                {
                    win = true;
                    string firstFig = LstUsedRects[0, i];
                    for (int j = 0; j < LstUsedRects.GetLength(0); j++)
                    {
                        if (LstUsedRects[j, i] != firstFig)
                            win = false;
                    }    
                    if (win)
                    {
                        GetWinner(firstFig, false);
                        return;
                    }
                }

                //Нет совпадений
                bool IsDraw = true;
                for (int i = 0; i < LstUsedRects.GetLength(0); i++)
                    for (int j = 0; j < LstUsedRects.GetLength(1); j++)
                        if (LstUsedRects[i, j] == "1")
                            IsDraw = false;
                if (IsDraw)
                {
                    Console.WriteLine("Ничья");
                    //Ничья
                    GetWinner("1", true);
                }
            }
        }

        private static void AddPlayer(IPEndPoint playerFullIP, string nickname)
        {
            try
            {
                if (Players.playersList.Where(u => u.IPEndPoint == playerFullIP).FirstOrDefault() == null)
                {
                    if (Players.playersList.Count < 2)
                    {
                        Players.AddPlayer(nickname, playerFullIP);
                        byte[] data = Encoding.Unicode.GetBytes($"command__trueJoin@{nickname}");
                        listeningSocket.SendTo(data, playerFullIP);
                        Console.WriteLine($"Игрок с IP <{playerFullIP}> был добавен! Его фигура: " + Players.playersList.Where(u=>u.IPEndPoint == playerFullIP).FirstOrDefault().figure);

                        data = Encoding.Unicode.GetBytes($"command__enemyJoin@{nickname}");
                        //Добавляем игрока тому, кто уже в комнате
                        if (Players.playersList.Where(u => u.IPEndPoint != playerFullIP).FirstOrDefault() != null)
                        {
                            listeningSocket.SendTo(data, Players.playersList.Where(u => u.IPEndPoint != playerFullIP).FirstOrDefault().IPEndPoint);
                        }
                        //Отправляем сообщение о подключении в чат
                        string message = " подключился к игре!";
                        SendMessage(playerFullIP, message);
                    }
                    else
                    {
                        byte[] data = Encoding.Unicode.GetBytes($"command__falseJoin@{nickname}");
                        listeningSocket.SendTo(data, playerFullIP);
                        Console.WriteLine($"Игрок с IP <{playerFullIP}> не был добавен!");
                    }
                }
                else
                {
                    byte[] data = Encoding.Unicode.GetBytes($"command__falseJoin@{nickname}");
                    listeningSocket.SendTo(data, playerFullIP);
                    Console.WriteLine($"Игрок с IP <{playerFullIP}> не был добавен!");
                }
            }
            catch
            {
                Console.WriteLine($"Игрок с IP <{playerFullIP}> не был добавен!");
            }
        }

        private static void SendMessage(IPEndPoint playerFullIP, string msg)
        {
            try
            {
                byte[] data =  Encoding.Unicode.GetBytes("command__ChatMessage@" + msg);
                foreach (var player in Players.playersList)
                {
                    if (player.IPEndPoint.ToString() != playerFullIP.ToString())
                    {
                        listeningSocket.SendTo(data, player.IPEndPoint);
                        Console.WriteLine($"Сообщение <{msg}> было отправлено!");
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Сообщение <{msg}> не было отправлено!");
            }
        }
    }
}
