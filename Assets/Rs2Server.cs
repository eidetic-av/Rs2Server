using Intel.RealSense;
using OpenGL;
using Spout.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;

namespace Eidetic.Rs2
{
    public partial class Rs2Server : MonoBehaviour
    {
        const int CameraCount = 4;
        const int DepthWidth = 640;
        const int DepthHeight = 480;
        const int CamPoints = DepthWidth * DepthHeight;
        const int MaxPoints = CamPoints * CameraCount;
        const int StreamWidth = DepthWidth * 3;
        const int StreamHeight = DepthHeight * CameraCount * 2;

        [Serializable]
        public class DeviceOptions
        {
            public bool Active = true;
            public bool Paused = false;
            [Range(0f, 1f)]
            public float Brightness = 0f;
            [Range(0f, 10f)]
            public float Saturation = 1f;
            // Calibration rotation in quaternions
            public Vector4 CalibrationRotation = Vector4.zero;
            // Manual rotation in euler angles
            public Vector3 Rotation = Vector3.zero;
            // Translation in metres
            public Vector3 CalibrationTranslation = Vector3.zero;
            public Vector3 PreTranslation = Vector3.zero;
            public Vector3 PostTranslation = Vector3.zero;
            public (Vector3 Min, Vector3 Max) PointThreshold = (Vector3.one * -10, Vector3.one * 10);
        }
        public List<DeviceOptions> Cameras;

        public Vector3 ABBoxMin = new Vector3(-10, -10, -10);
        public Vector3 ABBoxMax = new Vector3(10, 10, 10);

        public Dictionary<string, CombinedDriver> Drivers = new Dictionary<string, CombinedDriver>();

        Context Rs2Context;

        ComputeShader TransferShader;

        NativeArray<float3> SpoutBuffer;
        SpoutSender SpoutSender;

        DeviceContext DeviceContext;
        IntPtr GLContext = IntPtr.Zero;

        public static Rs2Server Instance;

        void Awake()
        {
            // return;
            Instance = this;
            DeviceContext = DeviceContext.Create();
            GLContext = DeviceContext.CreateContext(IntPtr.Zero);
            DeviceContext.MakeCurrent(GLContext);
            Debug.Log("Initialised OpenGL Device.");
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
            SpoutBuffer = new NativeArray<float3>(10000000, Allocator.Persistent);

            SpoutSender = new SpoutSender();
            SpoutSender.CreateSender("Rs2", StreamWidth, StreamHeight, 0);

            InitialiseUI();
        }

        void Update()
        {
            if (Cameras.All(cam => !cam.Active))
                return;

            for(int i = 0; i < Drivers.Count(); i++)
                if (Cameras[i].Active && !Cameras[i].Paused) Drivers.Values.ElementAt(i).UpdateFrames();

            if (DeviceContext == null) return;
            SendPointCloudMaps();
        }

        public Vector3 rotOffset0;

        void RunCalibration()
        {
            for(int i = 0; i < Drivers.Count(); i++)
            {
                var borderImage = GameObject.Find($"FrameBorder{i}")
                    .GetComponent<RawImage>();
                if (ArucoGenerator.Instance.Generate(i, out var pose))
                {
                    Cameras[i].CalibrationTranslation = pose.pos;
                    var euler = pose.rot.eulerAngles;
                    euler = new Vector3(euler.x, euler.y, euler.z);
                    var quat = Quaternion.Euler(euler);
                    // quat = Quaternion.Inverse(quat);
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
                var brightness = !dummy ? Cameras[i].Brightness : 0;
                var saturation = !dummy ? Cameras[i].Saturation : 1;
                var rotation = !dummy ? Cameras[i].Rotation : Vector3.zero;
                var preTranslation = !dummy ? Cameras[i].PreTranslation : Vector3.zero;
                var postTranslation = !dummy ? Cameras[i].PostTranslation : Vector3.zero;
                var calibrationRotation = !dummy ? Cameras[i].CalibrationRotation : Vector4.zero;
                var calibrationTranslation = !dummy ? Cameras[i].CalibrationTranslation : Vector3.zero;
                var pointThresholdMin = !dummy ? Cameras[i].PointThreshold.Min : Vector3.one * -10;
                var pointThresholdMax = !dummy ? Cameras[i].PointThreshold.Max : Vector3.one * 10;

                TransferShader.SetInt($"BufferSize{i}", (i + 1) * CamPoints);
                TransferShader.SetInts($"MapDimensions{i}", dimensions);
                TransferShader.SetFloat($"Brightness{i}", brightness);
                TransferShader.SetFloat($"Saturation{i}", saturation);
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

            AsyncGPUReadback.RequestIntoNativeArray(ref SpoutBuffer, gpuOutput, MaxPoints * 2 * sizeof(float) * 3, 0);
            AsyncGPUReadback.WaitAllRequests();

            var bufferPtr = (byte*) GetUnsafeBufferPointerWithoutChecks(SpoutBuffer);

            SpoutSender.SendImage(bufferPtr, StreamWidth, StreamHeight, Gl.RGBA, false, 0);

            gpuOutput.Release();
        }

        void OnDestroy()
        {
            SpoutBuffer.Dispose();
            SpoutSender.ReleaseSender(0);
            SpoutSender.Dispose();
            DeviceContext?.DeleteContext(GLContext);
            DeviceContext?.Dispose();
            DeviceContext = null;
        }


    }
}
