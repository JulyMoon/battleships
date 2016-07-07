using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsClient
{
    public class Battleships
    {
        public struct Ship
        {
            public readonly int Size;
            public readonly int X, Y;
            public readonly bool IsVertical;
            private bool[] isAlive;

            public ReadOnlyCollection<bool> IsAlive => isAlive.ToList().AsReadOnly();

            public Ship(int size, bool isVertical, int x, int y)
            {
                Size = size;
                X = x;
                Y = y;
                IsVertical = isVertical;
                isAlive = new bool[Size];
            }
        }

        private static readonly int[] shipSet = new[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        public const int BoardWidth = 10;
        public const int BoardHeight = BoardWidth;

        private List<Ship> myShips = new List<Ship>();
        private List<Ship> enemyShips = new List<Ship>();

        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();

        public Battleships()
        {

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

        public static bool WithinBoard(bool vertical, int size, int x, int y)
        {
            int width, height;
            GetShipDimensions(vertical, size, out width, out height);

            return x >= 0 && x < BoardWidth &&
                   y >= 0 && y < BoardHeight &&
                   x + width - 1 < BoardWidth &&
                   y + height - 1 < BoardHeight;
        }

        private static bool WithinRules(List<Ship> shipsSoFar, int size)
        {
            var setSoFar = shipsSoFar.Select((ship) => ship.Size).ToList();
            return setSoFar.Count((element) => element == size) < shipSet.Count((element) => element == size);
        }

        public void AddShip(bool vertical, int size, int x, int y)
        {
            if (!WithinBoard(vertical, size, x, y))
                throw new ArgumentException("Your ship doesn't fit on the board");

            //if (!WithinRules(myShips, size))
            //    throw new ArgumentException("There are already enough ships of that size");

            myShips.Add(new Ship(size, vertical, x, y));
        }
    }
}
