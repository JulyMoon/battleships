using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BattleshipsClient
{
    public class Battleships
    {
        public struct Ship
        {
            public struct Properties
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
            }

            public Properties Props;
            
            private readonly bool[] isAlive;

            public ReadOnlyCollection<bool> IsAlive => isAlive.ToList().AsReadOnly();

            public Ship(Properties shipProps)
            {
                Props = shipProps;
                isAlive = new bool[Props.Size];
            }

            public Ship(int size, bool isVertical, int x, int y) : this(new Properties(size, isVertical, x, y)) { }
        }

        private static readonly int[] shipSet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
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

        public static bool WithinBoard(Ship.Properties props)
        {
            int width, height;
            GetShipDimensions(props.IsVertical, props.Size, out width, out height);

            return props.X >= 0 && props.X < BoardWidth &&
                   props.Y >= 0 && props.Y < BoardHeight &&
                   props.X + width - 1 < BoardWidth &&
                   props.Y + height - 1 < BoardHeight;
        }

        public static bool WithinBoard(bool vertical, int size, int x, int y) => WithinBoard(new Ship.Properties(size, vertical, x, y));

        /*private static bool WithinRules(List<Ship> shipsSoFar, int size)
        {
            var setSoFar = shipsSoFar.Select((ship) => ship.Props.Size).ToList();
            return setSoFar.Count((element) => element == size) < shipSet.Count((element) => element == size);
        }*/

        public void AddShips(List<Ship.Properties> shipPropArray)
        {
            if (!shipPropArray.Select((shipProps) => shipProps.Size).OrderBy(size => size).SequenceEqual(shipSet.OrderBy(size => size)))
                throw new ArgumentException("Incorrect set of ships");

            foreach (var shipProps in shipPropArray)
            {
                if (!WithinBoard(shipProps))
                    throw new ArgumentException("This ship doesn't fit on the board");

                // TODO: add a check for overlapping ships

                myShips.Add(new Ship(shipProps));
            }
        }
    }
}
