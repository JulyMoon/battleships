using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BattleshipsServer
{
    public class Server
    {
        private const int port = 7070;

        private readonly List<Player> players = new List<Player>();

        public void Start() => StartListening();

        private async void StartListening()
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Listening...");
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                if (players.Count >= 2)
                    continue; // todo

                Console.WriteLine($"Connection from {client.Client.LocalEndPoint} accepted");
                var player = new Player(client);
                players.Add(player);
                new Thread(() => HandlePlayer(player)).Start();
            }
        }

        private void HandlePlayer(Player player)
        {
            var reader = new BinaryReader(player.Client.GetStream());
            while (true)
                ParseTraffic(player, reader.ReadString());
        }

        private void ParseTraffic(Player player, string data)
        {
            int separatorIndex = data.IndexOf(":", StringComparison.Ordinal);

            if (separatorIndex == -1)
                throw new ArgumentException("Invalid data");

            string header = data.Substring(0, separatorIndex);
            data = data.Substring(separatorIndex + 1);

            string message;
            switch (header)
            {
                case "name":
                    player.Name = data;
                    message = $"{player.Name} has joined the game.";
                    break;

                case "message":
                    message = $"{player.Name}: {data}";
                    break;

                default: throw new ArgumentException("Invalid data");
            }

            Console.WriteLine(message);
            // todo
        }
    }
}
