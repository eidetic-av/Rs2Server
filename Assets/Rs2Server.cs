using Intel.RealSense;
using OpenGL;
using Spout.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;

namespace Eidetic.Rs2
{
    public class Rs2Server : MonoBehaviour
    {
        public static bool RenderCameraPreviews = true;
        public bool renderCameraPreviews = true;

        const int CameraCount = 4;
        const int DepthWidth = 640;
        const int DepthHeight = 480;
        const int CamPoints = DepthWidth * DepthHeight;
        const int MaxPoints = CamPoints * CameraCount;
        const int StreamWidth = DepthWidth * 3;
        const int StreamHeight = DepthHeight * CameraCount;

        [Serializable]
        public class DeviceOptions
        {
            public bool Active = false;
            [Range(0f, 1f)]
            public float Brightness = 0f;
            [Range(0f, 10f)]
            public float Saturation = 1f;

        }
        public List<DeviceOptions> Cameras;

        public Vector3 CutoffMin = new Vector3(-5, -5, 0);
        public Vector3 CutoffMax = new Vector3(5, 5, 10);

        Context Rs2Context;

        Dictionary<string, CombinedDriver> Drivers = new Dictionary<string, CombinedDriver>();

        ComputeShader TransferShader;

        NativeArray<float3> ColorsSpoutBuffer;
        NativeArray<float3> PositionsSpoutBuffer;
        SpoutSender ColorSender;
        SpoutSender VertexSender;

        DeviceContext DeviceContext;
        IntPtr GLContext = IntPtr.Zero;

        void Awake()
        {
            // return;
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
            PositionsSpoutBuffer = new NativeArray<float3>(5000000, Allocator.Persistent);
            ColorsSpoutBuffer = new NativeArray<float3>(5000000, Allocator.Persistent);

            VertexSender = new SpoutSender();
            VertexSender.CreateSender("Rs2Vertices", StreamWidth, StreamHeight, 0);
            ColorSender = new SpoutSender();
            ColorSender.CreateSender("Rs2Colors", StreamWidth, StreamHeight, 0);
        }

        void Update()
        {
            if (Cameras.All(cam => !cam.Active))
                return;

            for(int i = 0; i < Drivers.Count(); i++)
                if (Cameras[i].Active) Drivers.Values.ElementAt(i).UpdateFrames();

            SendPointCloudMaps();
        }

        void LateUpdate()
        {
            RenderCameraPreviews = renderCameraPreviews;
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

                TransferShader.SetInt($"BufferSize{i}", (i + 1) * CamPoints);
                TransferShader.SetInts($"MapDimensions{i}", dimensions);
                TransferShader.SetFloat($"Brightness{i}", brightness);
                TransferShader.SetFloat($"Saturation{i}", saturation);
                TransferShader.SetBuffer(0, $"ColorBuffer{i}", colorBuffer);
                TransferShader.SetBuffer(0, $"PositionBuffer{i}", positionBuffer);
                TransferShader.SetBuffer(0, $"RemapBuffer{i}", remapBuffer);
            }

            var colorsGpuOutput = new ComputeBuffer(MaxPoints, sizeof(float) * 3);
            var positionsGpuOutput = new ComputeBuffer(MaxPoints, sizeof(float) * 3);

            TransferShader.SetVector("CutoffMin", CutoffMin);
            TransferShader.SetVector("CutoffMax", CutoffMax);
            TransferShader.SetBuffer(0, "Colors", colorsGpuOutput);
            TransferShader.SetBuffer(0, "Positions", positionsGpuOutput);

            int gfxThreadWidth = MaxPoints / 64;

            TransferShader.Dispatch(0, gfxThreadWidth, 1, 1);

            AsyncGPUReadback.RequestIntoNativeArray(ref ColorsSpoutBuffer, colorsGpuOutput, MaxPoints * sizeof(float) * 3, 0);
            AsyncGPUReadback.RequestIntoNativeArray(ref PositionsSpoutBuffer, positionsGpuOutput, MaxPoints * sizeof(float) * 3, 0);
            AsyncGPUReadback.WaitAllRequests();

            var colorsPtr = (byte*) GetUnsafeBufferPointerWithoutChecks(ColorsSpoutBuffer);
            var positionsPtr = (byte*) GetUnsafeBufferPointerWithoutChecks(PositionsSpoutBuffer);

            // perhaps we can combine these textures into
            ColorSender.SendImage(colorsPtr, StreamWidth, StreamHeight, Gl.RGBA, false, 0);
            VertexSender.SendImage(positionsPtr, StreamWidth, StreamHeight, Gl.RGBA, false, 0);

            colorsGpuOutput.Release();
            positionsGpuOutput.Release();
        }

        void OnDestroy()
        {
            PositionsSpoutBuffer.Dispose();
            ColorsSpoutBuffer.Dispose();
            ColorSender.ReleaseSender(0);
            ColorSender.Dispose();
            VertexSender.ReleaseSender(0);
            VertexSender.Dispose();
            DeviceContext?.DeleteContext(GLContext);
            DeviceContext?.Dispose();
            DeviceContext = null;
        }


    }
}
