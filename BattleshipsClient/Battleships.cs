using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BattleshipsClient
{
    public class Battleships
    {
        private static readonly Random random = new Random();
        private static readonly int[] shipSet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private static readonly int[,] neighborsAndItselfPoints = { {-1, -1}, {0, -1}, {1, -1}, {-1, 0}, {0, 0}, {1, 0}, {-1, 1}, {0, 1}, {1, 1} };
        public const int BoardWidth = 10;
        public const int BoardHeight = BoardWidth;

        private Client client = new Client();

        public event Client.SimpleEventHandler OpponentFound;

        private void OnOpponentFound()
        {
            myShips = myShipProps.Select(shipProps => new Ship(shipProps)).ToList();
            OpponentFound?.Invoke();
        } 

        private List<ShipProperties> myShipProps;
        private List<Ship> myShips;

        public ReadOnlyCollection<ShipProperties> MyShipProps => myShipProps.AsReadOnly();
        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();
        public static ReadOnlyCollection<int> ShipSet => Array.AsReadOnly(shipSet);

        public Battleships()
        {
            client.OpponentFound += OnOpponentFound;
        }

        public void EnterMatchmaking(List<ShipProperties> shipPropArray)
        {
            myShipProps = shipPropArray;
            client.EnterMatchmaking(shipPropArray);
        } 

        public async Task ConnectAsync(IPAddress ip, string name)
        {
            await client.ConnectAsync(ip, name);
        }

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

        /*public void AddShips(List<Ship.Properties> shipPropArray)
        {
            myShips = shipPropArray.Select(shipProps => new Ship(shipProps)).ToList();

            if (!MainWindow.DEBUG)
            {
                client.EnterMatchmaking(shipPropArray);
            }

            //if (!shipPropArray.Select(shipProps => shipProps.Size).OrderBy(size => size).SequenceEqual(shipSet.OrderBy(size => size)))
            //    throw new ArgumentException("Incorrect set of ships");
        }*/
    }
}
