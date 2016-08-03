using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleshipsClient
{
    public class ShipProperties
    {
        public readonly int Size;
        public readonly int X, Y;
        public readonly bool IsVertical;

        public ShipProperties(int size, bool isVertical, int x, int y)
        {
            Size = size;
            X = x;
            Y = y;
            IsVertical = isVertical;
        }

        public string Serialize() => $"{X}'{Y}'{Size}'{IsVertical}";

        public static ShipProperties Deserialize(string data)
        {
            var props = data.Split('\'');
            int x = Int32.Parse(props[0]);
            int y = Int32.Parse(props[1]);
            int size = Int32.Parse(props[2]);
            bool isVertical = Boolean.Parse(props[3]);
            return new ShipProperties(size, isVertical, x, y);
        }

        public static List<ShipProperties> DeserializeList(string data) => data.Split('|').Select(Deserialize).ToList();
    }
}
