using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using OpenGL;
using Spout.Interop;

namespace Eidetic.Rs2
{
    [Serializable]
    public class Rs2Client : MonoBehaviour
    {
        static TcpListener Receiver;
        static TcpClient Sender;
        static NetworkStream NetworkStream;
        static IPEndPoint LocalEndPoint;
        static byte[] ReceivedData = new byte[Rs2Server.BufferSize];
        static bool SendFrame = false;
        static bool UpdateLabel = false;

        static bool ListeningForConnection;
        static bool Connected = false;

        static SpoutSender SpoutSender;
        static DeviceContext DeviceContext;
        static IntPtr GLContext = IntPtr.Zero;

        void Awake()
        {
            StartListeningForConnection();

            DeviceContext = DeviceContext.Create();
            GLContext = DeviceContext.CreateContext(IntPtr.Zero);
            DeviceContext.MakeCurrent(GLContext);
            SpoutSender = new SpoutSender();
            SpoutSender.CreateSender("Rs2", Rs2Server.StreamWidth, Rs2Server.StreamHeight, 0);
        }

        static void StartListeningForConnection()
        {
            Receiver = new TcpListener(Rs2Server.NetworkPort);
            Receiver.Start();
            ReceivedData = new byte[Rs2Server.BufferSize];
            var listenerThread = new Thread(Listen);
            ListeningForConnection = true;
            listenerThread.Start();
        }

        static void Listen()
        {
            while (ListeningForConnection)
            {
                Sender = Receiver.AcceptTcpClient();
                NetworkStream = Sender.GetStream();

                // Send a byte to start the transfer
                var response = new byte[] { Byte.MaxValue };
                NetworkStream.Write(response, 0, response.Length);

                Connected = true;
                ListeningForConnection = false;
                UpdateLabel = true;
            }
        }

        unsafe void Update()
        {
            if (NetworkStream == null) return;
            if (NetworkStream.CanRead && Connected)
            {
                // read all of the data out of the stream
                do
                {
                    try
                    {
                        NetworkStream.Read(ReceivedData, 0, Rs2Server.BufferSize);
                    }
                    catch (Exception e)
                    {
                        // if we can't read, assume socket disconnected
                        Disconnect();
                        return;
                    }
                }
                while (NetworkStream.DataAvailable);

                // send the data as a spout image
                fixed (byte* bufferPtr = ReceivedData)
                {
                    SpoutSender.SendImage(bufferPtr,
                                          Rs2Server.StreamWidth,
                                          Rs2Server.StreamHeight,
                                          Gl.RGBA, false, 0);
                }

                // send a single byte to acknowledge the server can send again
                var response = new byte[] { Byte.MaxValue };
                try
                {
                    NetworkStream.Write(response, 0, response.Length);
                }
                catch (Exception e)
                {
                    Disconnect();
                }
            }
        }

        void Disconnect(bool listenForNewConnection = true)
        {
            Connected = false;
            UpdateLabel = true;
            Receiver?.Stop();
            Receiver = null;
            Sender?.Close();
            Sender = null;
            NetworkStream?.Close();
            NetworkStream?.Dispose();
            NetworkStream = null;
            ReceivedData = null;
            if (listenForNewConnection)
                StartListeningForConnection();
        }

        void LateUpdate()
        {
            if (UpdateLabel)
            {
                var connectedLabel = GameObject.Find("ConnectedLabel")
                    .GetComponent<UnityEngine.UI.Text>();
                connectedLabel.text = Connected ? "Connected" : "Not Connected";
                connectedLabel.color = Connected ? Color.green : Color.red;
                UpdateLabel = false;
            }
        }

        void OnDestroy()
        {
            ListeningForConnection = false;
            Disconnect(false);
            SpoutSender?.ReleaseSender(0);
            SpoutSender?.Dispose();
            DeviceContext?.DeleteContext(GLContext);
            DeviceContext = null;
        }
    }
}
