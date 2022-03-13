using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace SocketTest1
{
    // https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
    // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=net-5.0
    // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets?view=net-5.0
    class SocketServer
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int RECEIVE_BUFFER_SIZE = 8 * 1024;
        private static readonly byte[] RECEIVE_BUFFER = new byte[RECEIVE_BUFFER_SIZE];

        public static void Start(IPAddress ipAddress, int port)
        {
            Setup(serverSocket, ipAddress, port);
        }

        private static IPEndPoint Bind(Socket socketToBind, IPAddress ipAdress, int port)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAdress, port);
            socketToBind.Bind(ipEndPoint);

            return ipEndPoint;
        }

        private static void Setup(Socket serverSocket, IPAddress ipAddress, int port)
        {
            IPEndPoint ipEndPoint = Bind(serverSocket, ipAddress, port);
            serverSocket.Listen();
            Console.WriteLine($"Socket server listening on {ipEndPoint.Address}:{ipEndPoint.Port}");
            Accept(serverSocket);
        }

        private static void Accept(Socket serverSocket)
        {
            serverSocket.BeginAccept(AcceptCallback, serverSocket);
        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket _serverSocket = asyncResult.AsyncState as Socket;
            try
            {
                Socket clientSocket = _serverSocket.EndAccept(asyncResult);

                clientSockets.Add(clientSocket);

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

        private static void Receive(Socket senderSocket)
        {
            senderSocket.BeginReceive(RECEIVE_BUFFER, 0, RECEIVE_BUFFER_SIZE, SocketFlags.None, ReceiveCallback, senderSocket);
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
                    clientSockets.Remove(clientSocket);
                    return;
                }

                byte[] receivedBuffer = new byte[receivedLength];
                Array.Copy(RECEIVE_BUFFER, receivedBuffer, receivedLength);
                string msg = Encoding.ASCII.GetString(receivedBuffer);
                Console.WriteLine($"Client Msg: {msg}");

                //Send(clientSocket, $"{receivedLength}");
                Receive(clientSocket);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                clientSocket.Close();
                clientSockets.Remove(clientSocket);
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

        private static void Send(Socket receiverSocket, string mesage)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(mesage);
            receiverSocket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), receiverSocket);
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
                clientSockets.Remove(clientSocket);
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