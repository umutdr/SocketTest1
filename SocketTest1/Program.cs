using System;
using System.Net;
using System.Threading;

namespace SocketTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            int socketServerPort = 0;
            const int DEFAULT_PORT = 3333;
            Console.WriteLine("Type the port number that you want to listen:");
            do
            {
                string portString = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(portString))
                {
                    socketServerPort = DEFAULT_PORT;
                    break;
                }

                if (!int.TryParse(portString, out int _port))
                {
                    Console.WriteLine("invalid entry, try again");
                    continue;
                }

                socketServerPort = _port;
            }
            while (socketServerPort == 0);

            SocketServer.Start(IPAddress.Any, socketServerPort);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}