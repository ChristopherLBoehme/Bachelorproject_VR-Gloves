using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// State object for receiving data from remote device.  
namespace ManusVR.Core.Apollo
{
    public delegate void byteArrayDelegate(byte[] data);

    public class DynamicByteArray
    {
        private byte[] buffer = new byte[0];

        public byte[] Packet => buffer;

        public int Length => buffer.Length;

        public int Append(byte[] data, int length, int offset = 0)
        {
            try
            {
                //Debug.Log("Data: " + ApolloTCP.ByteArrayToString(data) + " length: " + length + " offset: " + offset);
                // copy current buffer to temporary buffer
                byte[] tempBuffer = new byte[buffer.Length];
                Array.Copy(buffer, tempBuffer, buffer.Length);
                // create new buffer with increased size
                //Debug.Log("new buffer length will be " + (buffer.Length + length));

                buffer = new byte[buffer.Length + length];
                // copy old buffer + new data back
                Array.Copy(tempBuffer, 0, buffer, 0, tempBuffer.Length);

                if (offset < 0)
                {
                    Debug.LogError("offset value should be >= 0, but is " + offset);
                }
                if (tempBuffer.Length < 0)
                {
                    Debug.LogError("tempBuffer.Length value should be >= 0, but is " + tempBuffer.Length);
                }
                if (length < 0)
                {
                    Debug.LogError("length value should be >= 0, but is " + length);
                }

                Array.Copy(data, offset, buffer, tempBuffer.Length, length);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
            // return the amount of bytes copied
            return length;
        }

        public void Clear()
        {
            buffer = new byte[0];
        }
    }

    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Complete packet
        public DynamicByteArray package = new DynamicByteArray();
    }

    public class ApolloTCP
    {
        public static bool ShowDebugLog = true;

        private const UInt16 clientId = 44178;

        private const UInt32 APOLLO_MIN_PACKET_BYTES = 8;
        private const UInt32 APOLLO_MAX_PACKET_BYTES = 1024;

        private static Socket socket = null;
        private static byteArrayDelegate receiveCallBack;
        private static string _ip;
        private static int _port;
        private static bool running = false;

        // Returns if the client is connected to the server
        public static bool connected => socket != null && socket.Connected;

        // Use this for initialization
        public ApolloTCP(byteArrayDelegate receiveFunction, string ip, int port)
        {
            // Register the receive message callback
            receiveCallBack = receiveFunction;

            _ip = ip;
            _port = port;
        }

        ~ApolloTCP()
        {
            StopClient();
        }

        // break the connection if there is any
        public static void StopClient()
        {
            log("Closed connection");
            // Set running to false to make sure the async receive thread stops
            running = false;
            // Clean-up
            if (connected)
                socket.Close();
        }

        // Connect the socket to the specified server
        public void ConnectClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, _port);

                if (socket != null && connected)
                    socket.Close();
                // Create a TCP/IP socket.  
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                socket.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), socket);
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (connected)
                {
                    log("Socket connected to " +
                        socket.RemoteEndPoint.ToString());

                    running = true;
                    // Start receiving data
                    Receive();
                }
                else
                {
                    log("Can't find the Apollo server");
                }
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        private static void Receive()
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();

                // Begin receiving the data from the remote device.  
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                log(e.ToString());
                // TODO: retry receive for n times then disconnect


            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;

                // Read data from the remote device.  
                int bytesRead = socket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.package.Append(state.buffer, bytesRead);

                    // check if I still need to read more data
                    if (socket.Available > 0)
                        // Get the rest of the data.  
                        socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    else
                    {
                        // All the data has arrived; put it in response.  
                        if (state.package.Length > 1)
                        {
                            // start chopping up the packet if needed
                            processPacket(state.package.Packet);
                        }
                        else
                        {
                            log("Completed transaction with 0 bytes in the buffer");
                        }
                        // Start listening again for new data if the client is still running
                        if (running)
                            Receive();
                    }
                }
                else
                    log("Got 0 byteRead ReceiveCallback call");
            }
            catch (Exception e)
            {
                log(e.ToString());
                //StopClient();
                // TODO: retry receive for n times then disconnect
            }
        }

        // handles chopping the packet and sends out the required delegates
        private static void processPacket(byte[] packet)
        {
            try
            {
                // chop up the packet if more then one enter at the same time
                // packetOffset keeps track of the current position in the packet
                int packetOffset = 0;
                DynamicByteArray buffer = new DynamicByteArray();
                do
                {
                    // get the packetsize (first 4 bytes)
                    packetOffset += buffer.Append(packet, 4, packetOffset);
                    int packetSize = (int)BitConverter.ToUInt32(buffer.Packet, 0);

                    // check if the packetSize makes sense, if this is a valid header
                    if (packetSize < APOLLO_MIN_PACKET_BYTES)
                    {
                        //Debug.LogError("the first 4 bytes of the packet describe a packet size that would be SMALLER than minimum packet size, probably corrupt packet received, packetSize from header = " + packetSize);
                        throw new System.ArgumentException("the first 4 bytes of the packet describe a packet size that would be SMALLER than minimum packet size, probably corrupt packet received, packetSize from header = " + packetSize);
                    }

                    if (packetSize > APOLLO_MAX_PACKET_BYTES)
                    {
                        //Debug.LogError("the first 4 bytes of the packet describe a packet size that would be LARGER than the maximum packet size, probably corrupt packet received, packetSize from header = " + packetSize);
                        throw new System.ArgumentException("the first 4 bytes of the packet describe a packet size that would be LARGER than the maximum packet size, probably corrupt packet received, packetSize from header = " + packetSize);
                    }

                    // the total length of the package can at most be the specified package length + the 4 bytes specifying the package length
                    if (packetSize + 4 > packet.Length)
                    {
                        //Debug.LogError("the first 4 bytes of the packet describe a packet size that is larger than the length of the package, packetSize from header plus 4 bytes = " + packetSize + 4 + ", packet.Length = " + packet.Length);
                        throw new System.ArgumentException("the first 4 bytes of the packet describe a packet size that is larger than the length of the package, packetSize from header plus 4 bytes = " + packetSize + 4 + ", packet.Length = " + packet.Length);
                    }

                    // reset the buffer and read [packSize] bytes into the buffer
                    buffer.Clear();

                    //Debug.Log("packetsize in header = " + packetSize.ToString() + ", actual packet lenght = " + packet.Length.ToString() + ", offset = " + packetOffset);

                    packetOffset += buffer.Append(packet, packetSize, packetOffset);

                    // check the data

                    //log("packet length sent to SDK = " + buffer.Packet.Length);

                    // send out the data
                    receiveCallBack(buffer.Packet);
                    buffer.Clear();
                }
                while (packetOffset < packet.Length);
            }
            catch (Exception e)
            {
                //logError(e.ToString());
                //Debug.LogError(); // apparently logError() does not work?
                logError(e.ToString());

            }
        }

        // Sends a byte array of data to the connected server
        public void Send(byte[] packet)
        {
            if (connected)
            {
                // Begin sending the data to the remote device.  
                socket.BeginSend(packet, 0, packet.Length, 0,
                    new AsyncCallback(SendCallback), socket);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Complete sending the data to the remote device.  
                socket.EndSend(ar);
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        public static string ByteArrayToString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }

        private static void log(string message)
        {
            if (ShowDebugLog)
                Debug.Log(message);
        }

        private static void logError(string message)
        {
            if (ShowDebugLog)
                Debug.LogWarning(message);
            // Always restart the TCP client when an error occured
            StopClient();
        }
    }
}