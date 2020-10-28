using System.Collections.Generic;
using System.Linq;
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
        public (Intrinsics color, Intrinsics depth) Intrinsics;
        public int DepthFramerate;
        public int ColorFramerate;

        public float Exposure = 0.5f;
        public float Brightness = 0.1f;
        public float Saturation = 1f;
        public float Gain = 0.5f;
        public float Contrast = 0.5f;

        public readonly object FrameLock = new object();

        public DepthConverter Converter;

        public RawImage ColorImage;
        public Texture2D ColorTexture;

        (VideoFrame color, Points points) Frame;
        double FrameTime;

        Pipeline Pipe;
        Sensor DepthSensor;
        Sensor ColorSensor;
        Thread ProcessThread;
        bool Terminate;

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
            PipelineProfile pipelineProfile;
            // Depth camera pipeline activation
            using (var config = new Config())
            {
                config.EnableDevice(DeviceSerial);
                config.EnableStream(Stream.Depth, DepthResolution.width, DepthResolution.height, Format.Z16, DepthFramerate);
                config.EnableStream(Stream.Color, ColorResolution.width, ColorResolution.height, Format.Rgba8, ColorFramerate);
                pipelineProfile = Pipe.Start(config);
            }

            var device = pipelineProfile.Device;
            DepthSensor = device.Sensors.Single(s => s.Is(Extension.DepthSensor));
            ColorSensor = device.Sensors.Single(s => s.Is(Extension.ColorSensor));

            // full laser power
            if (DepthSensor.Options.Supports(Option.LaserPower))
            {
                var laserPower = DepthSensor.Options[Option.LaserPower];
                laserPower.Value = laserPower.Max;
            }
            // Minimal confidence threshold to capture as many points as
            // possible
            if (DepthSensor.Options.Supports(Option.ConfidenceThreshold))
                DepthSensor.Options[Option.ConfidenceThreshold].Value = 1f;
            // Capture closest distance points
            if (DepthSensor.Options.Supports(Option.MinDistance))
                DepthSensor.Options[Option.MinDistance].Value = 0f;
            // No sharpening of the depth image
            if (DepthSensor.Options.Supports(Option.PostProcessingSharpening))
                DepthSensor.Options[Option.PostProcessingSharpening].Value = 0f;
            if (DepthSensor.Options.Supports(Option.PreProcessingSharpening))
                DepthSensor.Options[Option.PreProcessingSharpening].Value = 0f;
            // Minimal on-board noise filtering
            if (DepthSensor.Options.Supports(Option.NoiseFilterLevel))
                DepthSensor.Options[Option.NoiseFilterLevel].Value = 2f;
            // Disable auto-exposure and leave this option to the UI
            if (DepthSensor.Options.Supports(Option.EnableAutoExposure))
                DepthSensor.Options[Option.EnableAutoExposure].Value = 0f;
            // Disable auto-exposure and leave this option to the UI
            if (ColorSensor.Options.Supports(Option.EnableAutoExposure))
                ColorSensor.Options[Option.EnableAutoExposure].Value = 0f;

            // Worker thread activation
            ProcessThread = new Thread(ProcessFrames);
            ProcessThread.Start();

            // Local objects initialization
            Converter = new DepthConverter();
        }

        float exposureValue;
        float brightnessValue;
        float saturationValue;
        float gainValue;
        float contrastValue;
        void LateUpdate()
        {
            if (Exposure != exposureValue)
            {
                if (ColorSensor.Options.Supports(Option.Exposure))
                {
                    var mappedExposure = Exposure.Map(0f, 1f, 1f, 2000f, 3f);
                    ColorSensor.Options[Option.Exposure].Value = mappedExposure;
                }
                exposureValue = Exposure;
            }
            if (Brightness != brightnessValue)
            {
                if (ColorSensor.Options.Supports(Option.Brightness))
                {
                    var mappedBrightness = Brightness.Map(0f, 1f, -25f, 64f, 1f);
                    ColorSensor.Options[Option.Brightness].Value = mappedBrightness;
                }
                brightnessValue = Brightness;
            }
            if (Saturation != saturationValue)
            {
                if (ColorSensor.Options.Supports(Option.Saturation))
                {
                    var mappedSaturation = Saturation.Map(0f, 10f, 0f, 100f, .85f);
                    ColorSensor.Options[Option.Saturation].Value = mappedSaturation;
                }
                saturationValue = Saturation;
            }
            if (Gain != gainValue)
            {
                if (ColorSensor.Options.Supports(Option.Gain))
                {
                    var mappedGain = Gain.Map(0f, 1f, 0f, 128f, 1f);
                    ColorSensor.Options[Option.Gain].Value = mappedGain;
                }
                gainValue = Gain;
            }
            if (Contrast != contrastValue)
            {
                if (ColorSensor.Options.Supports(Option.Contrast))
                {
                    var mappedContrast = Contrast.Map(0f, 1f, 0f, 100f, 1f);
                    ColorSensor.Options[Option.Contrast].Value = mappedContrast;
                }
                contrastValue = Contrast;
            }
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

                if (ColorImage != null)
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
