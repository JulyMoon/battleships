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
                Console.WriteLine(reader.ReadString());
        }

        public void SendShips(IEnumerable<Battleships.Ship.Properties> shipPropArray)
            => Send($"ships:{SerializeShips(shipPropArray)}");

        private static string SerializeShips(IEnumerable<Battleships.Ship.Properties> shipPropArray)
            => shipPropArray.Aggregate("", (current, ship) => $"{current}|{ship.Serialize()}");

        private void Send(string text) => writer.Write(text);

        private void SendName(string name) => Send($"name:{name}");
    }
}
