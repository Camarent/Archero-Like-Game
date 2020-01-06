using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace NavJob.Systems
{
    public static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(float3 from, float3 to)
        {
            double num = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            return num < 1.00000000362749E-15 ? 0.0f : (float) math.acos(math.clamp(math.dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            var num1 = Angle(from, to);
            var num2 = (float) (from.y * (double) to.z - from.z * (double) to.y);
            var num3 = (float) (from.z * (double) to.x - from.x * (double) to.z);
            var num4 = (float) (from.x * (double) to.y - from.y * (double) to.x);
            var num5 = Mathf.Sign((float) (axis.x * (double) num2 + axis.y * (double) num3 + axis.z * (double) num4));
            return num1 * num5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Euler(this quaternion quaternion)
        {
            var q = quaternion.value;
            double3 res;

            var sinr_cosp = +2.0 * (q.w * q.x + q.y * q.z);
            var cosr_cosp = +1.0 - 2.0 * (q.x * q.x + q.y * q.y);
            res.x = math.atan2(sinr_cosp, cosr_cosp);

            var sinp = +2.0 * (q.w * q.y - q.z * q.x);
            if (math.abs(sinp) >= 1)
            {
                res.y = math.PI / 2 * math.sign(sinp);
            }
            else
            {
                res.y = math.asin(sinp);
            }

            var siny_cosp = +2.0 * (q.w * q.z + q.x * q.y);
            var cosy_cosp = +1.0 - 2.0 * (q.y * q.y + q.z * q.z);
            res.z = math.atan2(siny_cosp, cosy_cosp);
            return (float3) res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToAngleAxis(this quaternion quaternion, out float angle, out float3 axis)
        {
            quaternion.ToAngleAxisRad(out angle, out axis);
            angle = math.degrees(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToAngleAxisRad(this quaternion q, out float angle, out float3 axis)
        {
            if (math.abs(q.value.w) > 1.0f)
                q = math.normalize(q);
            angle = 2.0f * math.acos(q.value.w); // angle
            var den = math.sqrt(1.0 - q.value.w * q.value.w);
            axis = den > 0.0001f ? q.value.xyz : new float3(1, 0, 0);
        }
    }
}