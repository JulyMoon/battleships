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

        private static readonly int[,] diagonalNeighbors = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };

        private List<ShipProperties> myShipProps;
        private List<Ship> myShips;

        private readonly bool[,] enemyVerifiedEmptyCells = new bool[Game.BoardWidth, Game.BoardHeight];
        private readonly Cell[,] enemyCells = new Cell[Game.BoardWidth, Game.BoardHeight];
        private readonly bool[,] myVerifiedEmptyCells = new bool[Game.BoardWidth, Game.BoardHeight];
        private readonly bool[,] myMissCells = new bool[Game.BoardWidth, Game.BoardHeight];

        public bool MyTurn { get; private set; }
        public int LastShotX { get; private set; }
        public int LastShotY { get; private set; }

        public delegate void OpponentFoundEventHandler();

        public event OpponentFoundEventHandler OpponentFound;
        public event Client.OpponentShotEventHandler OpponentShot;
        public event Client.MyShotEventHandler MyShotReceived;

        public enum Cell { Unknown, Ship, Empty }

        public Cell GetEnemyCell(int x, int y) => enemyCells[x, y];
        public bool GetEnemyVerifiedEmptyCell(int x, int y) => enemyVerifiedEmptyCells[x, y];
        public bool GetMyVerifiedEmptyCell(int x, int y) => myVerifiedEmptyCells[x, y];
        public bool GetMyMissCell(int x, int y) => myMissCells[x, y];

        public ReadOnlyCollection<ShipProperties> MyShipProps => myShipProps.AsReadOnly();
        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();

        public Battleships()
        {
            for (int x = 0; x < Game.BoardWidth; x++)
                for (int y = 0; y < Game.BoardHeight; y++)
                {
                    enemyCells[x, y] = Cell.Unknown;
                    enemyVerifiedEmptyCells[x, y] = false;
                    myVerifiedEmptyCells[x, y] = false;
                    myMissCells[x, y] = false;
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

        private void OnOpponentShot(int x, int y)
        {
            int index, segment;
            bool hit = Game.GetShotShipSegment(myShips, x, y, out index, out segment);
            if (hit)
            {
                myShips[index].IsAlive[segment] = false;
                SetVerifiedEmptyCells(myVerifiedEmptyCells, x, y);
            }
            else
            {
                myMissCells[x, y] = true;
                MyTurn = true;
            }

            OpponentShot?.Invoke(x, y);
        }

        private static void SetVerifiedEmptyCells(bool[,] verifiedEmptyCells, int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {
                int xx = x + diagonalNeighbors[i, 0];
                int yy = y + diagonalNeighbors[i, 1];
                if (Game.WithinBoard(xx, yy))
                    verifiedEmptyCells[xx, yy] = true;
            }
        }

        private void OnMyShotReceived(bool hit)
        {
            enemyCells[LastShotX, LastShotY] = hit ? Cell.Ship : Cell.Empty;

            if (hit)
            {
                SetVerifiedEmptyCells(enemyVerifiedEmptyCells, LastShotX, LastShotY);
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
