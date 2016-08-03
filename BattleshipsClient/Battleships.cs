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
        private Client client = new Client();

        public event Client.TurnEventHandler OpponentFound;
        public event Client.OpponentShotEventHandler OpponentShot;
        public event Client.MyShotEventHandler MyShotReceived;

        private void OnOpponentFound(bool myTurn)
        {
            myShips = myShipProps.Select(shipProps => new Ship(shipProps)).ToList();
            OpponentFound?.Invoke(myTurn);
        }

        private void OnOpponentShot(bool hit_, int x, int y)
        {
            int index, segment;
            bool hit = Game.GetShotShipSegment(myShips, x, y, out index, out segment);
            if (hit)
            {
                myShips[index].IsAlive[segment] = false;
            }
            else
            {
                // todo
            }

            OpponentShot?.Invoke(hit, x, y);
        }

        private void OnMyShotReceived(bool hit)
        {
            MyShotReceived?.Invoke(hit);
        }

        private List<ShipProperties> myShipProps;
        private List<Ship> myShips;

        public ReadOnlyCollection<ShipProperties> MyShipProps => myShipProps.AsReadOnly();
        public ReadOnlyCollection<Ship> MyShips => myShips.AsReadOnly();

        public Battleships()
        {
            client.OpponentFound += OnOpponentFound;
            client.OpponentShot += OnOpponentShot;
            client.MyShotReceived += OnMyShotReceived;
        }

        public void EnterMatchmaking(List<ShipProperties> shipPropArray)
        {
            myShipProps = shipPropArray;
            client.EnterMatchmaking(shipPropArray);
        }

        public void Shoot(int x, int y) => client.Shoot(x, y);

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
