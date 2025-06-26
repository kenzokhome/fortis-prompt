// See https://aka.ms/new-console-template for more information

using Server;
using Server.Networking;
using System;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerLogic server = new ServerLogic();
            server.Run();
        }
    }
}
//Console.WriteLine("Hello, World!");