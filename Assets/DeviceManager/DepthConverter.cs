using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static UnityEngine.Experimental.Rendering.GraphicsFormat;
using IntPtr = System.IntPtr;
using RealSense = Intel.RealSense;

namespace Eidetic.Rs2
{
    // a lot of this was taken from Rsvfx by keijiro

    public class DepthConverter : System.IDisposable
    {
        public int2 Dimensions;

        public ComputeBuffer ColorBuffer;
        public ComputeBuffer PositionBuffer;
        public ComputeBuffer RemapBuffer;

        public (Vector4 color, Vector4 depth) Intrinsics;

        // Load color data (a video frame) into the internal buffer.
        public void LoadColorData
            (RealSense.VideoFrame frame, in RealSense.Intrinsics intrinsics)
        {
            if (frame == null) return;
            if (frame.Data == IntPtr.Zero) return;

            var size = frame.Width * frame.Height;

            if (ColorBuffer != null && ColorBuffer.count != size)
            {
                ColorBuffer.Dispose();
                ColorBuffer = null;
            }

            if (ColorBuffer == null)
                ColorBuffer = new ComputeBuffer(size, 4);

            UnsafeUtility.SetUnmanagedData(ColorBuffer, frame.Data, size, 4);

            Intrinsics.color = IntrinsicsToVector(intrinsics);
            Dimensions = math.int2(frame.Width, frame.Height);
        }

        // Load point data (a Points instance) into the internal buffer.
        public void LoadPointData
            (RealSense.Points points, in RealSense.Intrinsics intrinsics)
        {
            if (points == null) return;
            if (points.VertexData == IntPtr.Zero) return;
            if (points.TextureData == IntPtr.Zero) return;

            var countx2 = points.Count * 2;
            var countx3 = points.Count * 3;

            if (PositionBuffer != null && PositionBuffer.count != countx3)
            {
                PositionBuffer.Dispose();
                PositionBuffer = null;
            }

            if (RemapBuffer != null && RemapBuffer.count != countx2)
            {
                RemapBuffer.Dispose();
                RemapBuffer = null;
            }

            if (PositionBuffer == null)
                PositionBuffer = new ComputeBuffer(countx3, sizeof(float));

            if (RemapBuffer == null)
                RemapBuffer = new ComputeBuffer(countx2, sizeof(float));

            UnsafeUtility.SetUnmanagedData
                (PositionBuffer, points.VertexData, countx3, sizeof(float));

            UnsafeUtility.SetUnmanagedData
                (RemapBuffer, points.TextureData, countx2, sizeof(float));

            Intrinsics.depth = IntrinsicsToVector(intrinsics);
        }

        public void Dispose()
        {
            ColorBuffer?.Dispose();
            ColorBuffer = null;
            PositionBuffer?.Dispose();
            PositionBuffer = null;
            RemapBuffer?.Dispose();
            RemapBuffer = null;
        }

        Vector4 IntrinsicsToVector(RealSense.Intrinsics i)
        {
            return new Vector4(i.ppx, i.ppy, i.fx, i.fy);
        }
    }
}
