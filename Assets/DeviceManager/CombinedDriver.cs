using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Intel.RealSense;
using System.Threading;
using UnityEngine.UI;

namespace Eidetic.Rs2
{
    // a lot of this was taken from Rsvfx by keijiro

    public sealed class CombinedDriver : MonoBehaviour
    {
        public string DeviceSerial;
        public (int width, int height) DepthResolution;
        public (int width, int height) ColorResolution;
        public int DepthFramerate;
        public int ColorFramerate;

        Pipeline Pipe;
        Thread ProcessThread;
        bool Terminate;

        (VideoFrame color, Points points) Frame;
        (Intrinsics color, Intrinsics depth) Intrinsics;
        public readonly object FrameLock = new object();

        public DepthConverter Converter;
        double FrameTime;

        public RawImage ColorImage;
        Texture2D ColorTexture;

        void ProcessFrames()
        {
            using (var pcBlock = new PointCloud())
                while (!Terminate)
                    using (var fs = Pipe.WaitForFrames())
                    {
                        // Retrieve and store the color frame.
                        lock (FrameLock)
                        {
                            Frame.color?.Dispose();
                            Frame.color = fs.ColorFrame;

                            using (var prof = Frame.color.
                                   GetProfile<VideoStreamProfile>())
                                Intrinsics.color = prof.GetIntrinsics();

                            pcBlock.MapTexture(Frame.color);
                        }

                        // Construct and store a point cloud.
                        using (var df = fs.DepthFrame)
                        {
                            var pc = pcBlock.Process(df).Cast<Points>();

                            lock (FrameLock)
                            {
                                Frame.points?.Dispose();
                                Frame.points = pc;

                                using (var prof = df.
                                       GetProfile<VideoStreamProfile>())
                                    Intrinsics.depth = prof.GetIntrinsics();
                            }
                        }
                    }
        }

        void Start()
        {
            Pipe = new Pipeline();
            // Depth camera pipeline activation
            using (var config = new Config())
            {
                config.EnableDevice(DeviceSerial);
                config.EnableStream(Stream.Color, ColorResolution.width, ColorResolution.height, Format.Rgba8, ColorFramerate);
                config.EnableStream(Stream.Depth, DepthResolution.width, DepthResolution.height, Format.Z16, DepthFramerate);
                Pipe.Start(config);
            }

            // Worker thread activation
            ProcessThread = new Thread(ProcessFrames);
            ProcessThread.Start();

            // Local objects initialization
            Converter = new DepthConverter();
        }

        void OnDestroy()
        {
            // Thread termination
            Terminate = true;
            ProcessThread?.Join();
            ProcessThread = null;

            // Depth frame finalization
            Frame.color?.Dispose();
            Frame.points?.Dispose();
            Frame = (null, null);

            // Pipeline termination
            Pipe?.Dispose();
            Pipe = null;

            // Local objects finalization
            Converter?.Dispose();
            Converter = null;

        }

        public void Destroy()
        {
            OnDestroy();
            GameObject.Destroy(gameObject);
        }

        public void UpdateFrames()
        {
            var time = 0.0;
            // Retrieve the depth frame data.
            lock (FrameLock)
            {
                if (Frame.color == null) return;
                if (Frame.points == null) return;

                if (ColorImage != null && Rs2Server.RenderCameraPreviews)
                {
                    if (ColorTexture == null)
                        ColorTexture = new Texture2D(Frame.color.Width, Frame.color.Height, Convert(Frame.color.Profile.Format), false, true);
                    ColorTexture.LoadRawTextureData(Frame.color.Data, Frame.color.Stride * Frame.color.Height);
                    ColorTexture.Apply();
                    ColorImage.texture = ColorTexture;
                }

                Converter.LoadColorData(Frame.color, Intrinsics.color);
                Converter.LoadPointData(Frame.points, Intrinsics.depth);
                time = Frame.color.Timestamp;
            }

            // Record the timestamp of the depth frame.
            FrameTime = time;
        }

        private static TextureFormat Convert(Format lrsFormat)
        {
            switch (lrsFormat)
            {
                case Format.Z16: return TextureFormat.R16;
                case Format.Disparity16: return TextureFormat.R16;
                case Format.Rgb8: return TextureFormat.RGB24;
                case Format.Rgba8: return TextureFormat.RGBA32;
                case Format.Bgra8: return TextureFormat.BGRA32;
                case Format.Y8: return TextureFormat.Alpha8;
                case Format.Y16: return TextureFormat.R16;
                case Format.Raw16: return TextureFormat.R16;
                case Format.Raw8: return TextureFormat.Alpha8;
                case Format.Disparity32: return TextureFormat.RFloat;
                default:
                    throw new System.ArgumentException(string.Format("librealsense format: {0}, is not supported by Unity", lrsFormat));
            }
        }
    }
}
