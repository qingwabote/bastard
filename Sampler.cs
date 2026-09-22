using Unity.Collections;
using Unity.Mathematics;

namespace Bastard
{
    public static class Sampler
    {
        public static unsafe int Seek(float* times, int length, float value)
        {
            if (value <= times[0])
            {
                return 0;
            }

            if (value >= times[length - 1])
            {
                return length - 1;
            }

            return NativeSortExtension.BinarySearch(times, length, value);
        }

        public static unsafe float Float(float* times, float* values, int length, float time)
        {
            int index = Seek(times, length, time);
            if (index >= 0)
            {
                return values[index];
            }

            int next = ~index;
            int prev = next - 1;

            float t = (time - times[prev]) / (times[next] - times[prev]);
            return math.lerp(values[prev], values[next], t);
        }

        public static unsafe float3 Vec3(float* times, float3* values, int length, float time)
        {
            int index = Seek(times, length, time);
            if (index >= 0)
            {
                return *(values + index);
            }

            int next = ~index;
            int prev = next - 1;

            float t = (time - times[prev]) / (times[next] - times[prev]);
            return math.lerp(*(values + prev), *(values + next), t);
        }

        public static unsafe quaternion Quat(float* times, quaternion* values, int length, float time)
        {
            int index = Seek(times, length, time);
            if (index >= 0)
            {
                return *(values + index);
            }

            int next = ~index;
            int prev = next - 1;

            float t = (time - times[prev]) / (times[next] - times[prev]);
            return math.slerp(*(values + prev), *(values + next), t);
        }
    }
}
