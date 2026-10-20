using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Bastard
{
    public readonly struct CameraExtensionsUnmanaged
    {
        public delegate void GetGroundAABBDelegate(out float2 min, out float2 max);

        private struct GetGroundAABBTag { }
        public static readonly SharedStatic<FunctionPointer<GetGroundAABBDelegate>> GetGroundAABB = SharedStatic<FunctionPointer<GetGroundAABBDelegate>>.GetOrCreate<GetGroundAABBTag>();
    }

    public static class CameraExtensions
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void InitializeGetGroundAABB()
        {
            CameraExtensionsUnmanaged.GetGroundAABB.Data = new(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(m_GetGroundAABB));
        }

        private readonly static CameraExtensionsUnmanaged.GetGroundAABBDelegate m_GetGroundAABB = GetGroundAABBManaged;
        [AOT.MonoPInvokeCallback(typeof(CameraExtensionsUnmanaged.GetGroundAABBDelegate))]
        private static void GetGroundAABBManaged(out float2 min, out float2 max)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                min = default;
                max = default;
                return;
            }
            camera.GetGroundAABB(out var a, out var b);
            min = a.xz;
            max = b.xz;
        }

        public static void GetGroundAABB(out float2 min, out float2 max)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GetGroundAABBManaged(out min, out max);
#else
            CameraExtensionsUnmanaged.GetGroundAABB.Data.Invoke(out min, out max);
#endif
        }

        public static void GetGroundAABB(this Camera self, out float3 min, out float3 max)
        {
            var ground = new Plane(Vector3.up, Vector3.zero);

            if (!TryGetGroundPoint(ground, self.ViewportPointToRay(new Vector3(0f, 0f, 0f)), out var bottomLeft) ||
                !TryGetGroundPoint(ground, self.ViewportPointToRay(new Vector3(1f, 0f, 0f)), out var bottomRight) ||
                !TryGetGroundPoint(ground, self.ViewportPointToRay(new Vector3(0f, 1f, 0f)), out var topLeft) ||
                !TryGetGroundPoint(ground, self.ViewportPointToRay(new Vector3(1f, 1f, 0f)), out var topRight))
            {
                var position = self.transform.position;
                var halfWidth = self.orthographicSize * self.aspect;
                var forward = self.transform.forward;
                min = new float3(forward.x == 0f ? position.x - halfWidth : float.MinValue, float.MinValue, forward.z == 0f ? position.z - halfWidth : float.MinValue);
                max = new float3(forward.x == 0f ? position.x + halfWidth : float.MaxValue, float.MaxValue, forward.z == 0f ? position.z + halfWidth : float.MaxValue);
                return;
            }

            var xxxx = new float4(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);
            var zzzz = new float4(bottomLeft.z, bottomRight.z, topLeft.z, topRight.z);
            min = new float3(math.cmin(xxxx), float.MinValue, math.cmin(zzzz));
            max = new float3(math.cmax(xxxx), float.MaxValue, math.cmax(zzzz));
        }

        private static bool TryGetGroundPoint(Plane ground, Ray ray, out float3 point)
        {
            if (!ground.Raycast(ray, out var distance))
            {
                point = default;
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }
    }
}
