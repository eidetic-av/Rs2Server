using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Eidetic.Rs2
{
    public class ArucoGenerator : MonoBehaviour
    {
        static ArucoGenerator instance;
        public static ArucoGenerator Instance =>
            instance ?? (instance = GameObject.Find("Aruco").GetComponent<ArucoGenerator>());

        public float MarkerSize;
        public Vector3 offset;

        //Changes in pos/rot below these thresholds are ignored
        public float positionLowPass = 0.005f; //Value in meters
        public float rotationLowPass = 3; //Value in degrees

        public int avgFilterMemoryLength = 1;
        PoseRunningAverage average;

        public Dictionary<int, PoseData> poseDict;

        public Texture2D Image;
        [HideInInspector] public Color32[] ImageData;

        public float CenterX, CenterY, FocalX, FocalY;
        public float[] Distortion = new float[] {0, 0, 0, 0, 0};

        public bool Generate(int cameraIndex, out (Vector3 pos, Quaternion rot) pose)
        {
            var cameraDriver = Rs2Server.Instance.Drivers
                .Values.ElementAt(cameraIndex);

            Image = cameraDriver.ColorTexture.AsFormat(TextureFormat.RGB24);

            int width = Image.width;
            int height = Image.height;
            ImageData = Image.GetPixels32();

            var rawIntrinsics = cameraDriver.Intrinsics.color;
            var intrinsics = DepthConverter.IntrinsicsToVector(rawIntrinsics);
            FocalX = intrinsics[2];
            FocalY = intrinsics[3];
            CenterX = intrinsics[0];
            CenterY = intrinsics[1];
            Distortion = rawIntrinsics.coeffs;

            float[] cameraParams = new float[4 + 5];
            cameraParams[0] = FocalX;
            cameraParams[1] = FocalY;
            cameraParams[2] = CenterX;
            cameraParams[3] = CenterY;
            for (int i = 0; i < 5; i++)
                cameraParams[4 + i] = Distortion[i];

            if(width > 0 && height > 0)
                ArucoTracking.init(width, height, MarkerSize, cameraParams, 1);

            average = new PoseRunningAverage(avgFilterMemoryLength);
            poseDict = new Dictionary<int, PoseData>();

            // make sure the ArucoTracking native lib is initialised
            var sleepTime = 0;
            while (!ArucoTracking.lib_inited)
            {
                if (sleepTime > 5000)
                {
                    pose = (Vector3.zero, Vector4.zero.AsQuaternion());
                    return false;
                }
                System.Threading.Thread.Sleep(500);
                sleepTime += 500;
            }

            // run the tracking
            Color32[] img_data = ImageData;
            ArucoTracking.detect_markers(img_data);

            var  newDict = ArucoTrackingUtil.createUnityPoseData(
                    ArucoTracking.marker_count, ArucoTracking.ids, ArucoTracking.rvecs, ArucoTracking.tvecs);

            ArucoTrackingUtil.addCamSpaceOffset(newDict, offset); //Doing this first is important, since PoseDict also has positions with added offset
            ArucoTrackingUtil.posRotLowpass(poseDict, newDict, positionLowPass, rotationLowPass);
            average.averageNewState(newDict);

            poseDict = newDict;

            if (poseDict.Values.Count() == 0)
            {
                pose = (Vector3.zero, Vector4.zero.AsQuaternion());
                return false;
            }

            var poseValue = poseDict.Values.First();
            pose = (poseValue.pos, poseValue.rot);
            return true;
        }
    }
}
