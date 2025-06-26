using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class LanManager
    {
        private Socket _socketServer;
        private byte[] buffer;
        private EndPoint _remoteEndPoint;
        public bool serverSocketIsCreated = false;

        private int serverPort;
        private int clientPort;
        private bool debug = false;

        private Thread serverThread;
        private bool serverRunning = false;

        public LanManager(int serverPort, int clientPort, bool debug = true)
        {
            this.serverPort = serverPort;
            this.clientPort = clientPort;
            this.debug = debug;
            Init();
        }

        void Init()
        {
            buffer = new byte[1024];
            serverSocketIsCreated = false;
        }

        public void StartInBackground()
        {
            if (serverThread == null || !serverThread.IsAlive)
            {
                serverThread = new Thread(() =>
                {
                    serverRunning = true;
                    StartServerLoop();
                });
                serverThread.IsBackground = true;
                serverThread.Start();
            }
        }

        private void StartServerLoop()
        {
            try
            {
                _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socketServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socketServer.Bind(new IPEndPoint(IPAddress.Any, serverPort));
                _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                buffer = new byte[1024];
                _socketServer.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                               ref _remoteEndPoint, new AsyncCallback(_ReceiveServer), _socketServer);

                serverSocketIsCreated = true;

                if (debug)
                    Console.WriteLine($"LAN server listening on port {serverPort}");

                while (serverRunning)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server socket failed: " + ex.Message);
            }
        }


        public void StartServer()
        {
            if (_socketServer == null)
            {
                try
                {
                    _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _socketServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (_socketServer == null)
                    {
                        if (debug)
                            Console.WriteLine($"Server started on port {serverPort}.");
                        StartServer();
                        return;
                    }

                    _socketServer.Bind(new IPEndPoint(IPAddress.Any, serverPort));

                    _remoteEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

                    _socketServer.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                                   ref _remoteEndPoint, new AsyncCallback(_ReceiveServer), null);
                    serverSocketIsCreated = true;
                }
                catch (Exception ex)
                {
                    if (debug)
                    {
                        Console.WriteLine("Server socket failed: " + ex.Message);
                    }
                }

                if (serverSocketIsCreated == false)
                    StartServer();
            }
        }

        public void Stop()
        {
            serverRunning = false;
            if (_socketServer != null)
            {
                _socketServer.Close();
                _socketServer = null;
            }

            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Join();
            }

            //Init();

            if (debug)
                Console.WriteLine("Server stopped.");
        }

        private void _ReceiveServer(IAsyncResult ar)
        {
            if (_socketServer == null)
                return;
            Console.WriteLine("received server message");
            try
            {
                int msgLen = _socketServer.EndReceiveFrom(ar, ref _remoteEndPoint);
                byte[] localMsg = new byte[msgLen];
                Array.Copy(buffer, localMsg, msgLen);
                string receiveString = Encoding.ASCII.GetString(localMsg);

                if (debug)
                    Console.WriteLine($"Received from {_remoteEndPoint}: {receiveString}");

                if (receiveString == "ping")
                {
                    byte[] str = Encoding.ASCII.GetBytes("pong");
                    string ipAddr = ((IPEndPoint)_remoteEndPoint).Address.ToString();

                    IPEndPoint destinationEndPoint = new IPEndPoint(IPAddress.Parse(ipAddr), clientPort);

                    _socketServer.SendTo(str, destinationEndPoint);

                    if (debug)
                        Console.WriteLine($"Sent pong to {destinationEndPoint}");
                }

                buffer = new byte[1024];
                _remoteEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
                _socketServer.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                               ref _remoteEndPoint, new AsyncCallback(_ReceiveServer), null);
            }
            catch (Exception ex)
            {
                if (debug)
                    Console.WriteLine("Receive error: " + ex);
                buffer = new byte[1024];
                _remoteEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
                _socketServer.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                               ref _remoteEndPoint, new AsyncCallback(_ReceiveServer), null);
            }

        }
    }
}
