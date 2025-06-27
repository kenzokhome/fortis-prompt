using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Fortis.LAN
{
    [System.Serializable]
    public class LanManager
    {
        public List<string> _localAddresses { get; private set; }
        public List<string> _localSubAddresses { get; private set; }

        public List<string> _addresses { get; private set; }
        public string address = "";

        public int serverPort;
        public int clientPort;

        public bool _isSearching { get; private set; }
        public float _percentSearching { get; private set; }

        private Socket _socketClient;


        public bool socketIsCreated = false;

        private EndPoint _remoteEndPoint;

        private byte[] buffer;
        private bool debug = false;


        public LanManager(int serverPort, int clientPort, bool debug = false)
        {
            address = "";
            this.serverPort = serverPort;
            this.clientPort = clientPort;
            this.debug = debug;
            Init();
        }

        void Init()
        {
            _addresses = new List<string>();
            _localAddresses = new List<string>();
            _localSubAddresses = new List<string>();
            buffer = new byte[1024];
            socketIsCreated = false;
        }

        public void StartClient()
        {
            socketIsCreated = false;
            if (debug == true)
                Debug.Log("Starting Client");
            if (_socketClient == null)
            {
                if (debug == true)
                    Debug.Log("Client is NULL - generate");
                try
                {
                    _socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    if (_socketClient == null)
                    {
                        if (debug)
                            Debug.LogWarning("SocketClient creation failed");

                        StartClient();
                        return;
                    }

                    _socketClient.Bind(new IPEndPoint(IPAddress.Any, clientPort));

                    _socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    _socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);

                    _remoteEndPoint = new IPEndPoint(IPAddress.Any, clientPort);

                    if (debug)
                        if (_socketClient != null)

                            Debug.Log("SocketClient was created OK");

                    _socketClient.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                             ref _remoteEndPoint, new AsyncCallback(_ReceiveClient), null);
                    socketIsCreated = true;
                }
                catch (Exception ex)
                {

                    if (debug)
                    {
                        Debug.Log(ex.Message);
                        Debug.LogError("Client socket failed");
                        StartClient();
                        return;
                    }

                }

                if (socketIsCreated == false)
                {
                    StartClient();
                }
            }
        }

        public void CloseClient()
        {
            if (_socketClient != null)
            {
                _socketClient.Close();
                _socketClient = null;
                Init();
            }
        }

        public IEnumerator SendPing()
        {
            _addresses.Clear();
            if (_socketClient != null)
            {
                if (debug == true)
                    Debug.LogError("SOCKET IS NOT NULL");

                int maxSend = 4;
                float countMax = (maxSend * _localSubAddresses.Count) - 1;

                float index = 0;

                _isSearching = true;
                while (_socketClient != null)
                {
                    foreach (string subAddress in _localSubAddresses)
                    {

                        if (debug == true)
                            Debug.LogError(subAddress);

                        IPEndPoint destinationEndPoint = new IPEndPoint(IPAddress.Parse(subAddress + ".255"), serverPort);
                        byte[] str = Encoding.ASCII.GetBytes("ping");
                        if (_socketClient == null)
                            break;
                        _socketClient.SendTo(str, destinationEndPoint);

                        _percentSearching = index / countMax;

                        index++;

                        if (debug == true)
                            Debug.LogError("Send UDP to " + destinationEndPoint.ToString());

                        yield return new WaitForSeconds(1.0f);
                    }
                }
                _isSearching = false;
            }
        }

        private void _ReceiveClient(IAsyncResult ar)
        {
            if (_socketClient != null)
            {
                try
                {
                    Socket recvSock = (Socket)ar.AsyncState;
                    int msgLen = _socketClient.EndReceiveFrom(ar, ref _remoteEndPoint);
                    byte[] localMsg = new byte[msgLen];
                    Array.Copy(buffer, localMsg, msgLen);
                    string receiveString = Encoding.ASCII.GetString(localMsg);
                    if (receiveString == "pong")
                    {
                        //string thisAddress = _remoteEndPoint.ToString();
                        //this is not IPv6 ... Ip comes in the form of: x.x.x.x:1025:5055
                        string thisAddress = _remoteEndPoint.ToString().Split(':')[0];

                        if (address != thisAddress && !_localAddresses.Contains(address) && !_addresses.Contains(thisAddress))
                        {
                            _addresses.Add(thisAddress);
                            address = thisAddress;
                        }
                    }

                    if (debug == true)
                        Debug.LogError(address);

                    if (_socketClient != null)
                    {
                        _remoteEndPoint = new IPEndPoint(IPAddress.Any, clientPort);
                        buffer = new byte[1024];
                        _socketClient.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                                       ref _remoteEndPoint, new AsyncCallback(_ReceiveClient), null);
                    }
                }
                catch (Exception ex)
                {

                    if (debug)
                        Debug.Log(ex.ToString());

                    if (_socketClient != null)
                    {
                        _remoteEndPoint = new IPEndPoint(IPAddress.Any, clientPort);
                        buffer = new byte[1024];
                        _socketClient.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                                                       ref _remoteEndPoint, new AsyncCallback(_ReceiveClient), null);
                    }
                }
            }
        }

        public void ScanHost()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string address = ip.ToString();
                    string subAddress = address.Remove(address.LastIndexOf('.'));

                    _localAddresses.Add(address);

                    if (!_localSubAddresses.Contains(subAddress))
                    {
                        _localSubAddresses.Add(subAddress);
                    }
                }
            }
        }
    }
}