using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsClient
{
    public class Client
    {
        private readonly TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        private BinaryWriter writer;
        private BinaryReader reader;
        private const int port = 7070;

        public delegate void MyEventHandler();
        public event MyEventHandler OpponentFound;
        //public event MyEventHandler MyTurn;

        private void OnOpponentFound() => OpponentFound?.Invoke();
        //private void OnMyTurn() => MyTurn?.Invoke();

        public async Task ConnectAsync(IPAddress IP, string name)
        {
            await client.ConnectAsync(IP, port);
            writer = new BinaryWriter(client.GetStream());
            reader = new BinaryReader(client.GetStream());

            Task.Run(() => Listen());

            SendName(name);
        }

        private void Listen()
        {
            while (true)
                ParseTraffic(reader.ReadString());
        }

        private void ParseTraffic(string traffic)
        {
            switch (traffic)
            {
                case "opponentFound": OnOpponentFound(); break;
                default: throw new NotImplementedException();
                //case "yourTurn": OnMyTurn(); break;
            }
        }

        public void SendShips(IEnumerable<Battleships.Ship.Properties> shipPropArray)
            => Send($"ships:{SerializeShips(shipPropArray)}");

        private static string SerializeShips(IEnumerable<Battleships.Ship.Properties> shipPropArray)
            => shipPropArray.Aggregate("", (current, ship) => $"{current}|{ship.Serialize()}").Substring(1);

        private void Send(string text) => writer.Write(text);

        private void SendName(string name) => Send($"name:{name}");
    }
}
