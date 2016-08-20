using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace BattleshipsCommon
{
    public static class Game
    {
        public const string ServerHostname = "foxness.ddns.net";
        public const int Port = 7070;

        public static IPAddress ServerIP => GetIPFromHostname(ServerHostname);

        private static readonly Random random = new Random();
        private static readonly int[] shipSet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        private static readonly int[,] neighborsAndItselfPoints = { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 0, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };

        public const int BoardWidth = 10;
        public const int BoardHeight = BoardWidth;
        public static ReadOnlyCollection<int> ShipSet => Array.AsReadOnly(shipSet);

        public const string YourTurnString = "yourTurn";
        public const string OpponentsTurnString = "opponentsTurn";
        public const string YouMissedString = "youMissed";
        public const string YouHitString = "youHit";
        public const string YouSankString = "youSank";
        public const string OpponentShotString = "opponentShot";

        public const string NameString = "name";
        public const string EnterString = "enter";
        public const string LeaveString = "leave";
        public const string ShootString = "shoot";

        public static IPAddress GetIPFromHostname(string hostname) => Dns.GetHostAddresses(hostname)[0];

        public static void GetShipDimensions(bool vertical, int size, out int shipW, out int shipH)
        {
            if (vertical)
            {
                shipW = 1;
                shipH = size;
            }
            else
            {
                shipW = size;
                shipH = 1;
            }
        }

        public static bool WithinBoard(ShipProperties props)
        {
            int width, height;
            GetShipDimensions(props.IsVertical, props.Size, out width, out height);

            return WithinBoard(props.X, props.Y) && props.X + width - 1 < BoardWidth && props.Y + height - 1 < BoardHeight;
        }

        public static bool WithinBoard(int x, int y) => x >= 0 && x < BoardWidth && y >= 0 && y < BoardHeight;

        public static bool Overlaps(IEnumerable<ShipProperties> ships, ShipProperties other)
        {
            var grid = new bool[BoardWidth, BoardHeight];
            foreach (var ship in ships)
            {
                for (int i = 0; i < ship.Size; i++)
                {
                    if (ship.IsVertical)
                        grid[ship.X, ship.Y + i] = true;
                    else
                        grid[ship.X + i, ship.Y] = true;
                }
            }

            for (int i = 0; i < other.Size; i++)
            {
                int x, y;
                if (other.IsVertical)
                {
                    x = other.X;
                    y = other.Y + i;
                }
                else
                {
                    x = other.X + i;
                    y = other.Y;
                }

                for (int j = 0; j < 9; j++)
                {
                    int xx = x + neighborsAndItselfPoints[j, 0];
                    int yy = y + neighborsAndItselfPoints[j, 1];

                    if (WithinBoard(xx, yy) && grid[xx, yy])
                        return true;
                }
            }

            return false;
        }

        public static List<ShipProperties> GetRandomShips()
        {
            var randomShips = new List<ShipProperties>();

            foreach (int shipSize in shipSet.OrderByDescending(size => size))
            {
                ShipProperties randomShip;
                do
                {
                    bool vertical = random.Next(2) == 0;

                    int x, y;
                    if (vertical)
                    {
                        x = random.Next(BoardWidth);
                        y = random.Next(BoardHeight - (shipSize - 1));
                    }
                    else
                    {
                        x = random.Next(BoardWidth - (shipSize - 1));
                        y = random.Next(BoardHeight);
                    }

                    randomShip = new ShipProperties(shipSize, vertical, x, y);
                } while (Overlaps(randomShips, randomShip));

                randomShips.Add(randomShip);
            }

            return randomShips;
        }

        public static bool GetShotShipSegment(List<Ship> ships, int x, int y, out int index, out int segment)
        {
            for (int i = 0; i < ships.Count; i++)
                for (int j = 0; j < ships[i].Size; j++)
                {
                    int xx, yy;
                    if (ships[i].IsVertical)
                    {
                        xx = ships[i].X;
                        yy = ships[i].Y + j;
                    }
                    else
                    {
                        xx = ships[i].X + j;
                        yy = ships[i].Y;
                    }

                    if (x != xx || y != yy)
                        continue;

                    index = i;
                    segment = j;
                    return true;
                }

            index = -1;
            segment = -1;
            return false;
        }
    }
}
