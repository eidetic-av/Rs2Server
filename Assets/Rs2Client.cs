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

        static bool Listening;
        static bool UpdateLabel;
        static bool SpoutFrame;

        static SpoutSender SpoutSender;
        static DeviceContext DeviceContext;
        static IntPtr GLContext = IntPtr.Zero;

        void Awake()
        {
            Receiver = new TcpListener(Rs2Server.NetworkPort);
            Receiver.Start();
            var listenerThread = new Thread(Listen);
            ReceivedData = new byte[Rs2Server.BufferSize];
            Listening = true;
            listenerThread.Start();

            DeviceContext = DeviceContext.Create();
            GLContext = DeviceContext.CreateContext(IntPtr.Zero);
            DeviceContext.MakeCurrent(GLContext);
            SpoutSender = new SpoutSender();
            SpoutSender.CreateSender("Rs2", Rs2Server.StreamWidth, Rs2Server.StreamHeight, 0);
        }

        static void Listen()
        {
            while (Listening)
            {
                if (Sender == null)
                {
                    Sender = Receiver.AcceptTcpClient();
                    NetworkStream = Sender.GetStream();
                    UpdateLabel = true;

                    // Send a byte to start the transfer
                    var response = new byte[] { Byte.MaxValue };
                    NetworkStream.Write(response, 0, response.Length);
                }
            }
        }

        unsafe void Update()
        {
            if (NetworkStream == null) return;
            if (NetworkStream.CanRead)
            {
                // read all of the data out of the stream
                do NetworkStream.Read(ReceivedData, 0, Rs2Server.BufferSize);
                while (NetworkStream.DataAvailable);

                fixed (byte* bufferPtr = ReceivedData)
                {
                    SpoutSender.SendImage(bufferPtr,
                                          Rs2Server.StreamWidth,
                                          Rs2Server.StreamHeight,
                                          Gl.RGBA, false, 0);
                }

                // send a single byte to acknowledge the server can send again
                var response = new byte[] { Byte.MaxValue };
                NetworkStream.Write(response, 0, response.Length);
            }
        }

        void LateUpdate()
        {
            if (UpdateLabel)
            {
                var connectedLabel = GameObject.Find("ConnectedLabel")
                    .GetComponent<UnityEngine.UI.Text>();
                connectedLabel.text = "Connected";
                UpdateLabel = false;
            }
        }

        void OnDestroy()
        {
            Listening = false;
            SpoutSender?.ReleaseSender(0);
            SpoutSender?.Dispose();
            DeviceContext?.DeleteContext(GLContext);
            DeviceContext = null;
        }
    }
}
