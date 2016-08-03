using System.Linq;

namespace BattleshipsClient
{
    public class Ship : ShipProperties
    {
        public readonly bool[] IsAlive;

        public bool Dead => IsAlive.All(cell => !cell);

        public Ship(int size, bool isVertical, int x, int y) : base(size, isVertical, x, y)
        {
            IsAlive = Enumerable.Repeat(true, size).ToArray();
        }

        public Ship(ShipProperties shipProps) : this(shipProps.Size, shipProps.IsVertical, shipProps.X, shipProps.Y) { }
    }
}
