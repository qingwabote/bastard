using Unity.Mathematics;
using UnityEngine;

namespace Bastard
{
    public static class ColorExtensions
    {
        public static float4 ToFloat4(this Color color) => new(color.r, color.g, color.b, color.a);
    }
}