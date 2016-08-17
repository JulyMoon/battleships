using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleshipsCommon;

namespace BattleshipsClient
{
    public class Client
    {
        private readonly TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        private BinaryWriter writer;
        private BinaryReader reader;
        private const int port = 7070;

        public enum ShotResult { Miss, Hit, Sink }

        public delegate void TurnEventHandler(bool myTurn);
        public delegate void OpponentShotEventHandler(int x, int y);
        public delegate void MyShotEventHandler(ShotResult result);

        public event TurnEventHandler OpponentFound;
        public event OpponentShotEventHandler OpponentShot;
        public event MyShotEventHandler MyShotReceived;

        private void OnOpponentFound(bool myTurn) => OpponentFound?.Invoke(myTurn);
        private void OnOpponentShot(int x, int y) => OpponentShot?.Invoke(x, y);
        private void OnMyShotReceived(ShotResult result) => MyShotReceived?.Invoke(result);

        public bool Connected => client.Connected;

        public async Task ConnectAsync(IPAddress IP, string name)
        {
            await client.ConnectAsync(IP, port);
            writer = new BinaryWriter(client.GetStream());
            reader = new BinaryReader(client.GetStream());

#pragma warning disable 4014
            Task.Run(() => Listen());
#pragma warning restore 4014

            SendName(name);
        }

        private void Listen()
        {
            while (true)
                ParseTraffic(reader.ReadString());
        }

        private void ParseTraffic(string traffic)
        {
            int delimiterIndex = traffic.IndexOf(":", StringComparison.Ordinal);
            if (delimiterIndex == -1)
            {
                switch (traffic)
                {
                    case Game.YourTurnString: OnOpponentFound(true); break;
                    case Game.OpponentsTurnString: OnOpponentFound(false); break;
                    case Game.YouMissedString: OnMyShotReceived(ShotResult.Miss); break;
                    case Game.YouHitString: OnMyShotReceived(ShotResult.Hit); break;
                    case Game.YouSankString: OnMyShotReceived(ShotResult.Sink); break;
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                string header = traffic.Substring(0, delimiterIndex);
                string data = traffic.Substring(delimiterIndex + 1);

                if (header == Game.OpponentShotString)
                {
                    var split = data.Split('\'');
                    int x = Int32.Parse(split[0]);
                    int y = Int32.Parse(split[1]);

                    OnOpponentShot(x, y);
                }
                else
                    throw new NotImplementedException();
            }
        }

        public void Shoot(int x, int y) => Send($"{Game.ShootString}:{x}'{y}");

        public void EnterMatchmaking(IEnumerable<ShipProperties> shipPropArray) => Send($"{Game.EnterString}:{SerializeShips(shipPropArray)}");

        private static string SerializeShips(IEnumerable<ShipProperties> shipPropArray)
            => shipPropArray.Aggregate("", (current, ship) => $"{current}|{ship.Serialize()}").Substring(1);

        private void Send(string text) => writer.Write(text);

        private void SendName(string name) => Send($"{Game.NameString}:{name}");
    }
}
