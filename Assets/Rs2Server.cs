using Intel.RealSense;
using OpenGL;
using Spout.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;

namespace Eidetic.Rs2
{
    [Serializable]
    public partial class Rs2Server : MonoBehaviour
    {
        public const int CameraCount = 4;
        public const int DepthWidth = 640;
        public const int DepthHeight = 480;
        public const int CamPoints = DepthWidth * DepthHeight;
        public const int MaxPoints = CamPoints * CameraCount;
        public const int BufferSize = MaxPoints * sizeof(float) * 3 * 2;
        public const int StreamWidth = DepthWidth * 3;
        public const int StreamHeight = DepthHeight * CameraCount * 2;

        public static Rs2Server Instance;

        public string CurrentConfigName = "Rs2Config";
        public List<DeviceOptions> Cameras;

        public Vector3 ABBoxMin = new Vector3(-10, -10, -10);
        public Vector3 ABBoxMax = new Vector3(10, 10, 10);

        public bool SendOverSpout = false;
        public bool SendOverNetwork = false;
        public string Hostname = "127.0.0.1";

        [NonSerialized]
        public Dictionary<string, CombinedDriver> Drivers = new Dictionary<string, CombinedDriver>();

        ComputeShader TransferShader;

        Context Rs2Context;
        SpoutSender SpoutSender;
        DeviceContext DeviceContext;
        IntPtr GLContext = IntPtr.Zero;
        byte[] SpoutBuffer;
        bool GLContextInitialised = false;

        TcpClient NetworkSender;
        NetworkStream NetworkStream;
        public const int NetworkPort = 9876;

        void Awake() => Instance = this;

        void InitialiseOpenGL()
        {
            DeviceContext = DeviceContext.Create();
            GLContext = DeviceContext.CreateContext(IntPtr.Zero);
            DeviceContext.MakeCurrent(GLContext);
            SpoutSender = new SpoutSender();
            SpoutSender.CreateSender("Rs2", StreamWidth, StreamHeight, 0);
            Debug.Log("Initialised OpenGL Device.");
            GLContextInitialised = true;
        }

        void Start()
        {
            Rs2Context = new Context();
            foreach(var dev in Rs2Context.QueryDevices())
            {
                Cameras.Add(new DeviceOptions());

                var deviceInfo = dev.Info;
                var deviceSerial = deviceInfo[CameraInfo.SerialNumber];
                var deviceName = deviceInfo[CameraInfo.Name];

                var manager = new GameObject("Rs2DeviceManager_" + deviceSerial);
                var driver = manager.AddComponent<CombinedDriver>();
                driver.DeviceSerial = deviceSerial;
                Drivers[deviceSerial] = driver;

                if (deviceName.Contains("D435"))
                {
                    driver.DepthResolution = (DepthWidth, DepthHeight);
                    driver.ColorResolution = (960, 540);
                    // driver.DepthResolution = (848, 480);
                    // driver.ColorResolution = (848, 480);
                    driver.DepthFramerate = 30;
                    driver.ColorFramerate = 30;
                } else if (deviceName.Contains("L515"))
                {
                    // driver.DepthResolution = (1024, 768);
                    // driver.ColorResolution = (1280, 720);
                    driver.DepthResolution = (DepthWidth, DepthHeight);
                    driver.ColorResolution = (960, 540);
                    driver.DepthFramerate = 30;
                    driver.ColorFramerate = 30;

                }

                driver.ColorImage = GameObject.Find($"ColorTexture{Cameras.Count() - 1}")?
                    .GetComponent<UnityEngine.UI.RawImage>();
            }

            TransferShader = Resources.Load("Transfer") as ComputeShader;
            SpoutBuffer = new byte[BufferSize];

            TryLoadLatestJson();
            InitialiseUI();
        }

        void Update()
        {
            if (Cameras.All(cam => !cam.Active))
                return;

            for(int i = 0; i < Drivers.Count(); i++)
                if (Cameras[i].Active && !Cameras[i].Paused) Drivers.Values.ElementAt(i).UpdateFrames();

            UpdateCameraSettings();
            SendPointCloudMaps();
        }

        void RunCalibration()
        {
            for(int i = 0; i < Drivers.Count(); i++)
            {
                var borderImage = GameObject.Find($"FrameBorder{i}")
                    .GetComponent<RawImage>();
                if (ArucoGenerator.Instance.Generate(i, out var pose))
                {
                    Cameras[i].CalibrationTranslation = -pose.pos;
                    var euler = pose.rot.eulerAngles;
                    euler = new Vector3(euler.x, euler.y, euler.z);
                    var quat = Quaternion.Euler(euler);
                    quat = Quaternion.Inverse(quat);
                    Cameras[i].CalibrationRotation = quat.AsVector();
                    borderImage.color = Color.green;
                }
                else borderImage.color = Color.red;
            }
        }

        unsafe void SendPointCloudMaps()
        {
            for (int i = 0; i < CameraCount; i++)
            {
                // if the device doesn't exist, or it's not active
                // send dummy (empty) values to the shader
                bool dummy = false;
                if (i >= Cameras.Count() || !Cameras[i].Active)
                    dummy = true;

                CombinedDriver driver = !dummy ? Drivers.ElementAt(i).Value : null;
                DepthConverter converter = driver?.Converter;

                // if the converter or buffers aren't properly initialised,
                // also send dummy values
                if (converter == null || converter.PositionBuffer == null
                    || converter.ColorBuffer == null) dummy = true;

                var dimensions = !dummy ? converter.Dimensions : math.int2(1, 1);
                var colorBuffer = !dummy ? converter.ColorBuffer : new ComputeBuffer(1, 4);
                var positionBuffer = !dummy ? converter.PositionBuffer : new ComputeBuffer(1, sizeof(float));
                var remapBuffer = !dummy ? converter.RemapBuffer : new ComputeBuffer(1, sizeof(float));
                var rotation = !dummy ? Cameras[i].Rotation : Vector3.zero;
                var preTranslation = !dummy ? Cameras[i].PreTranslation : Vector3.zero;
                var postTranslation = !dummy ? Cameras[i].PostTranslation : Vector3.zero;
                var calibrationRotation = !dummy ? Cameras[i].CalibrationRotation : Vector4.zero;
                var calibrationTranslation = !dummy ? Cameras[i].CalibrationTranslation : Vector3.zero;
                var pointThresholdMin = !dummy ? Cameras[i].PointThresholdMin : Vector3.one * -10;
                var pointThresholdMax = !dummy ? Cameras[i].PointThresholdMax : Vector3.one * 10;

                TransferShader.SetInt($"BufferSize{i}", (i + 1) * CamPoints);
                TransferShader.SetInts($"MapDimensions{i}", dimensions);
                TransferShader.SetVector($"Rotation{i}", Quaternion.Euler(rotation).AsVector());
                TransferShader.SetVector($"PreTranslation{i}", preTranslation);
                TransferShader.SetVector($"PostTranslation{i}", postTranslation);
                TransferShader.SetVector($"PointThresholdMin{i}", pointThresholdMin);
                TransferShader.SetVector($"PointThresholdMax{i}", pointThresholdMax);
                TransferShader.SetVector($"CalibrationRotation{i}", calibrationRotation);
                TransferShader.SetVector($"CalibrationTranslation{i}", calibrationTranslation);
                TransferShader.SetBuffer(0, $"ColorBuffer{i}", colorBuffer);
                TransferShader.SetBuffer(0, $"PositionBuffer{i}", positionBuffer);
                TransferShader.SetBuffer(0, $"RemapBuffer{i}", remapBuffer);
            }

            TransferShader.SetInt("MaxPoints", MaxPoints);
            TransferShader.SetVector("ABBoxMin", ABBoxMin);
            TransferShader.SetVector("ABBoxMax", ABBoxMax);

            var gpuOutput = new ComputeBuffer(MaxPoints * 2, sizeof(float) * 3);
            TransferShader.SetBuffer(0, "OutputBuffer", gpuOutput);

            int gfxThreadWidth = MaxPoints / 64;

            TransferShader.Dispatch(0, gfxThreadWidth, 1, 1);

            gpuOutput.GetData(SpoutBuffer, 0, 0, BufferSize);

            if (SendOverSpout)
            {
                if (!GLContextInitialised) InitialiseOpenGL();
                fixed (byte* bufferPtr = SpoutBuffer)
                {
                    SpoutSender.SendImage(bufferPtr, StreamWidth, StreamHeight, Gl.RGBA, false, 0);
                }
            }

            // Make this happen on another thread so we can compute GPU
            // while we're sending last frames computed bytes at the same time
            if (SendOverNetwork)
            {
                if (NetworkSender == null)
                {
                    NetworkSender = new TcpClient();
                    NetworkSender.Connect(Hostname, NetworkPort);
                    NetworkStream = NetworkSender.GetStream();
                }
                if (NetworkStream.CanWrite)
                {
                    // wait until client response before sending
                    while (!NetworkStream.DataAvailable) {  }

                    // and read the response out of the buffer
                    var response = new byte[1];
                    do NetworkStream.Read(response, 0, 1);
                    while (NetworkStream.DataAvailable);

                    // then send the pointcloud data
                    NetworkStream.Write(SpoutBuffer, 0, BufferSize);
                }
            }

            gpuOutput.Release();
        }

        void UpdateCameraSettings()
        {
            for (int i = 0; i < Cameras.Count(); i++)
            {
                var deviceOptions = Cameras[i];
                var driver = Drivers.Values.ElementAt(i);
                driver.Exposure = deviceOptions.Exposure;
                driver.Brightness = deviceOptions.Brightness;
                driver.Saturation = deviceOptions.Saturation;
                driver.Gain = deviceOptions.Gain;
                driver.Contrast = deviceOptions.Contrast;
            }
        }

        void OnDestroy()
        {
            NetworkSender?.Dispose();
            NetworkStream?.Dispose();
            SpoutSender?.ReleaseSender(0);
            SpoutSender?.Dispose();
            DeviceContext?.DeleteContext(GLContext);
            DeviceContext?.Dispose();
            DeviceContext = null;
        }

        void TryLoadLatestJson()
        {
            var jsonFiles = System.IO.Directory.EnumerateFiles(
                Application.dataPath, "*.json");
            if (jsonFiles.Count() == 0) return;

            var latestSave = jsonFiles.Select(fp => new System.IO.FileInfo(fp))
                .OrderBy(info => info.LastWriteTime).Last()
                .Name.Replace(".json", "");
            Deserialize(latestSave, false);
        }

        void Serialize() => System.IO.File.WriteAllLines(
            $"{Application.dataPath}\\{CurrentConfigName}.json",
            new string[]{ JsonUtility.ToJson(Instance) });

        void Deserialize(string configName, bool refreshUI = true)
        {
            try
            {
                var jsonString = System.IO.File.ReadAllText(
                    $"{Application.dataPath}\\{configName}.json");
                JsonUtility.FromJsonOverwrite(jsonString, Instance);
                if (refreshUI) InitialiseUI();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed loading from json; printing trace:");
                Debug.LogError(e.StackTrace);
            }
        }

        [Serializable]
        public class DeviceOptions
        {
            public bool Active = true;
            public bool Paused = false;
            [Range(0f, 1f)]
            public float Brightness = 0f;
            [Range(0f, 10f)]
            public float Saturation = 1f;
            [Range(0f, 1f)]
            public float Exposure = 0.5f;
            [Range(0f, 1f)]
            public float Gain = 0.5f;
            [Range(0f, 1f)]
            public float Contrast = 0.5f;
            // Calibration rotation in quaternions
            public Vector4 CalibrationRotation = Vector4.zero;
            // Manual rotation in euler angles
            public Vector3 Rotation = Vector3.zero;
            // Translation in metres
            public Vector3 CalibrationTranslation = Vector3.zero;
            public Vector3 PreTranslation = Vector3.zero;
            public Vector3 PostTranslation = Vector3.zero;
            public Vector3 PointThresholdMin = Vector3.one * -10;
            public Vector3 PointThresholdMax = Vector3.one * 10;
        }
    }
}
