using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketTest1
{
    // https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
    // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=net-5.0
    // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets?view=net-5.0

    public interface ISocketServer
    {
        void Start(IPAddress ipAddress, int port);
        Task StartAsync(IPAddress ipAddress, int port);
    }

    public class SocketServer : ISocketServer
    {
        private static readonly Socket ServerSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> ClientSocketsList = new();
        private const int RECEIVE_BUFFER_SIZE = 8 * 1024;
        private static readonly byte[] RECEIVE_BUFFER = new byte[RECEIVE_BUFFER_SIZE];

        private static IPEndPoint Bind(Socket socketToBind, IPAddress ipAdress, int port)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAdress, port);
            socketToBind.Bind(ipEndPoint);

            return ipEndPoint;
        }

        #region Non Async Methods
        public void Start(IPAddress ipAddress, int port)
        {
            IPEndPoint ipEndPoint = Bind(ServerSocket, ipAddress, port);
            ServerSocket.Listen();
            Console.WriteLine($"Socket server listening on {ipEndPoint.Address}:{ipEndPoint.Port}");
            Accept(ServerSocket);
        }

        private static void Accept(Socket serverSocket)
        {
            serverSocket.BeginAccept(AcceptCallback, serverSocket);
        }

        private static void Receive(Socket clientSocket)
        {
            clientSocket.BeginReceive(RECEIVE_BUFFER, 0, RECEIVE_BUFFER_SIZE, SocketFlags.None, ReceiveCallback, clientSocket);
        }

        private static void Send(Socket receiverSocket, string mesage)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(mesage);
            receiverSocket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), receiverSocket);
        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket _serverSocket = asyncResult.AsyncState as Socket;
            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);

                ClientSocketsList.Add(clientSocket);

                Receive(clientSocket);

                Console.WriteLine($"{clientSocket.RemoteEndPoint} connected");

                Accept(_serverSocket);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            Socket clientSocket = asyncResult.AsyncState as Socket;
            try
            {
                int receivedLength = clientSocket.EndReceive(asyncResult);

                if (receivedLength == 0)
                {
                    Console.WriteLine($"{clientSocket.RemoteEndPoint} disconnected");
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    ClientSocketsList.Remove(clientSocket);
                    return;
                }

                byte[] receivedBuffer = new byte[receivedLength];
                Array.Copy(RECEIVE_BUFFER, receivedBuffer, receivedLength);
                string msg = Encoding.ASCII.GetString(receivedBuffer);
                Console.WriteLine($"Client Msg: {msg}");

                Send(clientSocket, string.Format("{0}", receivedLength));
                Receive(clientSocket);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                clientSocket.Close();
                ClientSocketsList.Remove(clientSocket);
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        private static void SendCallback(IAsyncResult asyncResult)
        {
            Socket clientSocket = asyncResult.AsyncState as Socket;

            try
            {
                var sentBytesCount = clientSocket.EndSend(asyncResult);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                clientSocket.Close();
                ClientSocketsList.Remove(clientSocket);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }
        #endregion

        #region Async Methods
        public async Task StartAsync(IPAddress ipAddress, int port)
        {
            IPEndPoint ipEndPoint = Bind(ServerSocket, ipAddress, port);
            ServerSocket.Listen();
            Console.WriteLine($"Socket server listening on {ipEndPoint.Address}:{ipEndPoint.Port}");

            await AcceptAsync(ServerSocket);
        }

        private async static Task AcceptAsync(Socket serverSocket)
        {
            try
            {
                Socket clientSocket = await serverSocket.AcceptAsync();

                ClientSocketsList.Add(clientSocket);
                Console.WriteLine($"{clientSocket.RemoteEndPoint} connected");

                Task.Run(async () => { await ReceiveAsync(clientSocket); });

                Task.Run(async () => { await AcceptAsync(serverSocket); });
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            { throw new Exception(e.Message, e); }
        }

        private async static Task ReceiveAsync(Socket clientSocket)
        {
            try
            {
                var receivedBytesCount = await clientSocket.ReceiveAsync(RECEIVE_BUFFER, SocketFlags.None);

                if (receivedBytesCount == 0)
                {
                    Console.WriteLine($"{clientSocket.RemoteEndPoint} disconnected");
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    ClientSocketsList.Remove(clientSocket);
                    return;
                }

                byte[] receivedBuffer = new byte[receivedBytesCount];
                Array.Copy(RECEIVE_BUFFER, receivedBuffer, receivedBytesCount);
                string msg = Encoding.ASCII.GetString(receivedBuffer);
                Console.WriteLine($"Client Msg: {msg}");

                Task.Run(async () =>
                {
                    await SendAsync(clientSocket, string.Format("{0}", receivedBytesCount));
                });

                Task.Run(async () =>
                {
                    await ReceiveAsync(clientSocket);
                });
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                clientSocket.Close();
                ClientSocketsList.Remove(clientSocket);
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        private async static Task SendAsync(Socket clientSocket, string mesage)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(mesage);
            await clientSocket.SendAsync(messageBytes, SocketFlags.None);
        }
        #endregion

        //private static void CloseAllSockets(bool closeServerSocket = false)
        //{
        //    foreach (Socket socket in clientSockets)
        //    {
        //        socket.Shutdown(SocketShutdown.Both);
        //        socket.Close();
        //    }

        //    if (closeServerSocket)
        //        serverSocket.Close();
        //}
    }
}