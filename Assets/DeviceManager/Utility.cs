using System.Reflection;
using UnityEngine;
using Unity.Mathematics;
using IntPtr = System.IntPtr;

namespace Eidetic.Rs2
{
    // taken from Rsvfx by keijiro
    static class UnsafeUtility
    {
        static MethodInfo _method;
        static object [] _args5 = new object[5];

        public static void SetUnmanagedData
            (ComputeBuffer buffer, IntPtr pointer, int count, int stride, int srcOffset = 0, int bufferOffset =0)
        {
            if (_method == null)
            {
                _method = typeof(ComputeBuffer).GetMethod(
                    "InternalSetNativeData",
                    BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );
            }

            _args5[0] = pointer;
            _args5[1] = srcOffset;
            _args5[2] = bufferOffset;
            _args5[3] = count;
            _args5[4] = stride;

            _method.Invoke(buffer, _args5);
        }
    }

    static class UnityEngineExtensions
    {
        static int[] _intArgs2 = new int [2];

        public static void SetInts
            (this ComputeShader shader, string name, Vector2Int args)
        {
            _intArgs2[0] = args.x;
            _intArgs2[1] = args.y;
            shader.SetInts(name, _intArgs2);
        }

        public static void SetInts
            (this ComputeShader shader, string name, int2 args)
        {
            _intArgs2[0] = args.x;
            _intArgs2[1] = args.y;
            shader.SetInts(name, _intArgs2);
        }

        public static void SetVector
            (this ComputeShader shader, string name,
             float x, float y = 0, float z = 0, float w = 0)
        {
            shader.SetVector(name, new Vector4(x, y, z, w));
        }

        public static Texture2D AsFormat(this Texture2D source, TextureFormat newFormat)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width, source.height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height, newFormat, false);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        public static Vector4 AsVector(this Quaternion quaternion) =>
            new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);

        public static Quaternion AsQuaternion(this Vector4 vector) =>
            new Quaternion(vector.x, vector.y, vector.z, vector.w);

        public static float Map(this float value, float minIn, float maxIn, float minOut, float maxOut) =>
            ((value - minIn) / (maxIn - minIn)) * (maxOut - minOut) + minOut;

        public static float Map(this float value, float minIn, float maxIn, float minOut, float maxOut, float exponent)
        {
            var raised = Mathf.Pow(value.Map(minIn, maxIn, minOut, maxOut), exponent);
            return raised.Map(minOut, Mathf.Pow(maxOut, exponent), minOut, maxOut);
        }
    }
}
