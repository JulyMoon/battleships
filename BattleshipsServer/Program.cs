using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new Server().Start();

            while (true)
                Console.ReadLine();
        }
    }
}
