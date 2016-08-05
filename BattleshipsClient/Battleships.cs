using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BattleshipsCommon;

namespace BattleshipsClient
{
    public class Battleships
    {
        private readonly Client client = new Client();

        private List<ShipProperties> myShipProps;
        private List<Ship> myShips;

        private readonly bool[,] verifiedEmptyCell = new bool[Game.BoardWidth, Game.BoardHeight];
        private static readonly int[,] diagonalNeighbors = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };

        private readonly Cell[,] enemyCells = new Cell[Game.BoardWidth, Game.BoardHeight];

        public bool MyTurn { get; private set; }
        public int LastShotX { get; private set; }
        public int LastShotY { get; private set; }

        public delegate void OpponentFoundEventHandler();

        public event OpponentFoundEventHandler OpponentFound;
        public event Client.OpponentShotEventHandler OpponentShot;
        public event Client.MyShotEventHandler MyShotReceived;

        public enum Cell { Unknown, Ship, Empty }

        public Cell GetEnemyCell(int x, int y) => enemyCells[x, y];
        public bool GetVerifiedEmptyCell(int x, int y) => verifiedEmptyCell[x, y];

        public ReadOnlyCollection<ShipProperties> MyShipProps => myShipProps.AsReadOnly();
        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();

        public Battleships()
        {
            for (int x = 0; x < Game.BoardWidth; x++)
                for (int y = 0; y < Game.BoardHeight; y++)
                {
                    enemyCells[x, y] = Cell.Unknown;
                    verifiedEmptyCell[x, y] = false;
                }

            client.OpponentFound += OnOpponentFound;
            client.OpponentShot += OnOpponentShot;
            client.MyShotReceived += OnMyShotReceived;
        }

        private void OnOpponentFound(bool myTurn)
        {
            MyTurn = myTurn;
            myShips = myShipProps.Select(shipProps => new Ship(shipProps)).ToList();
            OpponentFound?.Invoke();
        }

        private void OnOpponentShot(bool hit_notUsed___________, int x, int y)
        {
            int index, segment;
            bool hit = Game.GetShotShipSegment(myShips, x, y, out index, out segment);
            if (hit)
            {
                myShips[index].IsAlive[segment] = false;
            }
            else
            {
                MyTurn = true;
            }

            OpponentShot?.Invoke(hit, x, y);
        }

        private void OnMyShotReceived(bool hit)
        {
            enemyCells[LastShotX, LastShotY] = hit ? Cell.Ship : Cell.Empty;

            if (hit)
            {
                for (int i = 0; i < 4; i++)
                {
                    int x = LastShotX + diagonalNeighbors[i, 0];
                    int y = LastShotY + diagonalNeighbors[i, 1];
                    if (Game.WithinBoard(x, y))
                        verifiedEmptyCell[x, y] = true;
                }
            }
            else
                MyTurn = false;

            MyShotReceived?.Invoke(hit);
        }

        public void EnterMatchmaking(List<ShipProperties> shipPropArray)
        {
            myShipProps = shipPropArray;
            client.EnterMatchmaking(shipPropArray);
        }

        public void Shoot(int x, int y)
        {
            LastShotX = x;
            LastShotY = y;
            client.Shoot(x, y);
        }

        public async Task ConnectAsync(IPAddress ip, string name) => await client.ConnectAsync(ip, name);

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
