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
        public struct Ship
        {
            public class Properties
            {
                public readonly int Size;
                public readonly int X, Y;
                public readonly bool IsVertical;

                public Properties(int size, bool isVertical, int x, int y)
                {
                    Size = size;
                    X = x;
                    Y = y;
                    IsVertical = isVertical;
                }

                public string Serialize() => $"{X}'{Y}'{Size}'{IsVertical}";

                public Properties Deserialize(string data)
                {
                    var props = data.Split('\'');
                    int x = Int32.Parse(props[0]);
                    int y = Int32.Parse(props[1]);
                    int size = Int32.Parse(props[2]);
                    bool isVertical = Boolean.Parse(props[3]);
                    return new Properties(size, isVertical, x, y);
                }
            }

            public Properties Props;
            
            private readonly bool[] isAlive;

            public ReadOnlyCollection<bool> IsAlive => isAlive.ToList().AsReadOnly();
            public bool Dead => isAlive.All(cell => !cell);

            public Ship(Properties shipProps)
            {
                Props = shipProps;
                isAlive = Enumerable.Repeat(true, Props.Size).ToArray();
            }

            public Ship(int size, bool isVertical, int x, int y) : this(new Properties(size, isVertical, x, y)) { }
        }

        private static readonly int[] shipSet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private static readonly int[,] neighborsAndItselfPoints = { {-1, -1}, {0, -1}, {1, -1}, {-1, 0}, {0, 0}, {1, 0}, {-1, 1}, {0, 1}, {1, 1} };
        public const int BoardWidth = 10;
        public const int BoardHeight = BoardWidth;

        private Client client = new Client();

        private List<Ship> myShips;// = new List<Ship>();

        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();
        public static ReadOnlyCollection<int> ShipSet => Array.AsReadOnly(shipSet);

        public Battleships()
        {

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

        public static bool WithinBoard(Ship.Properties props)
        {
            int width, height;
            GetShipDimensions(props.IsVertical, props.Size, out width, out height);

            return WithinBoard(props.X, props.Y) && props.X + width - 1 < BoardWidth && props.Y + height - 1 < BoardHeight;
        }

        public static bool WithinBoard(int x, int y) => x >= 0 && x < BoardWidth && y >= 0 && y < BoardHeight;

        public static bool Overlaps(IEnumerable<Ship.Properties> ships, Ship.Properties other)
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

        public void AddShips(List<Ship.Properties> shipPropArray)
        {
            myShips = shipPropArray.Select(shipProps => new Ship(shipProps)).ToList();
            client.SendShips(shipPropArray);

            //if (!shipPropArray.Select(shipProps => shipProps.Size).OrderBy(size => size).SequenceEqual(shipSet.OrderBy(size => size)))
            //    throw new ArgumentException("Incorrect set of ships");
        }
    }
}
