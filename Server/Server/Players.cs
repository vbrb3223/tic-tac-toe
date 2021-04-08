using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Server
{
    class Players
    {
        public static List<Player> playersList = new List<Player>();
        static Random r = new Random();
        /// <summary>
        /// Добавление игрока в список playersList
        /// </summary>
        /// <param name="nickname">Имя подключившегося игрока</param>
        /// <param name="ipEndPoint">Адрес подключившегося игрока</param>
        public static void AddPlayer(string nickname, IPEndPoint ipEndPoint)
        {
            char fig = 'x';
            if (playersList.Count == 0)
            {
                if (r.Next(0, 2) == 0)
                    fig = 'x';
                else
                    fig = '0';
            }
            else
            {
                if (playersList[0].figure == 'x')
                    fig = '0';
                else
                    fig = 'x';
            }
            var p = new Player()
            {
                IPEndPoint = ipEndPoint,
                Nickname = nickname,
                figure = fig
            };
            playersList.Add(p);
        }

        /// <summary>
        /// Возвращение игрока с полученным адресом
        /// </summary>
        /// <param name="endPoint">Адрес игрока</param>
        /// <returns>Игрок из списка</returns>
        public static Player GetPlayer(IPEndPoint endPoint)
        {
            return playersList.Where(u => u.IPEndPoint == endPoint).FirstOrDefault();
        }

        /// <summary>
        /// Проверяет, существует ли игрок с таким адресом
        /// </summary>
        /// <param name="ipEndPoint">Адресс игрока</param>
        /// <returns>Булевая переменная. Равна true, если игрок с таким адресом существует, false - если не существует.</returns>
        public static bool IsExists(IPEndPoint ipEndPoint)
        {
            return playersList.Where(u => u.IPEndPoint == ipEndPoint).FirstOrDefault() != null ? true : false;
        }

        /// <summary>
        /// Объект "Игрок"
        /// </summary>
        public class Player
        {
            public IPEndPoint IPEndPoint { get; set; }
            public string Nickname { get; set; }
            public char figure { get; set; }
        }
    }
}
