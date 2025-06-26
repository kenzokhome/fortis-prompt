using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SimpleServer : INetEventListener
    {
        private NetManager _netManager;

        public void Run()
        {
            _netManager = new NetManager(this);
            if (!_netManager.Start(10515))
                Console.WriteLine("Failed to start server");
            else
                Console.WriteLine("Server started on port 10515");

            while (true)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);
            }
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Console.WriteLine("REQUEST TO JOIN");
            request.AcceptIfKey("ExampleGame");
        }

        public void OnPeerConnected(NetPeer peer) => Console.WriteLine($"Client connected: ");
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => Console.WriteLine("Disconnected");
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Console.WriteLine("Error");
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }
    }
}
