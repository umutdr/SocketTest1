using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SocketTest1
{
    public class SocketServerBackgroundService : BackgroundService
    {
        private readonly ISocketServer _socketServerService;

        public SocketServerBackgroundService(ISocketServer socketServerService)
        {
            _socketServerService = socketServerService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int socketServerPort = await GetPortNumberFromConsole();
            await _socketServerService.StartAsync(IPAddress.Any, socketServerPort);
        }

        private async static Task<int> GetPortNumberFromConsole()
        {
            const int DEFAULT_PORT = 3333;
            int socketServerPort = 0;

            Console.WriteLine("Type the port number that you want to listen:");
            Console.WriteLine("Press the ENTER key to use default port ({0}) ", DEFAULT_PORT);

            do
            {
                string portString = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(portString))
                {
                    socketServerPort = DEFAULT_PORT;
                    break;
                }

                if (!int.TryParse(portString, out int _port) || _port == 0)
                {
                    Console.WriteLine("invalid entry, try again");
                    continue;
                }

                socketServerPort = _port;
            }
            while (socketServerPort == 0);

            return await Task.FromResult(socketServerPort);
        }
    }
}
