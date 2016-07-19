using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsServer
{
    public class Player
    {
        public TcpClient Client { get; }
        public string Name;

        public Player(TcpClient client)
        {
            Client = client;
        }
    }
}
