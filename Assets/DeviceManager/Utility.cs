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

    static class ComputeShaderExtensions
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
    }
}
