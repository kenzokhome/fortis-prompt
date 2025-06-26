using LiteNetLib.Utils;
using System;
using System.Numerics;

namespace Server.Utils
{
    public static class Extensions
    {
        public static void PutV2(this NetDataWriter writer, Vector2 vector)
        {
            writer.Put(vector.X);
            writer.Put(vector.Y);
        }

        public static Vector2 GetVector2(this NetDataReader reader)
        {
            Vector2 v = new Vector2();
            v.X = reader.GetFloat();
            v.Y = reader.GetFloat();
            return v;
        }

        public static void PutV3(this NetDataWriter writer, Vector3 vector)
        {
            writer.Put(vector.X);
            writer.Put(vector.Y);
            writer.Put(vector.Z);
        }

        public static Vector3 GetVector3(this NetDataReader reader)
        {
            Vector3 v = new Vector3();
            v.X = reader.GetFloat();
            v.Y = reader.GetFloat();
            v.Z = reader.GetFloat();
            return v;
        }

        public static T GetRandomElement<T>(this T[] array)
        {
            return array[RandomHelper.Range(0, array.Length)];
        }

        public static void PutQuat(this NetDataWriter writer, Quaternion quat)
        {
            writer.Put(quat.X);
            writer.Put(quat.Y);
            writer.Put(quat.Z);
            writer.Put(quat.W);
        }

        public static Quaternion GetQuaternion(this NetDataReader reader)
        {
            Quaternion v = new Quaternion();
            v.X = reader.GetFloat();
            v.Y = reader.GetFloat();
            v.Z = reader.GetFloat();
            v.W = reader.GetFloat();
            return v;
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);

            Matrix4x4 matrix = new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                forward.X, forward.Y, forward.Z, 0,
                0, 0, 0, 1
            );

            return Quaternion.CreateFromRotationMatrix(matrix);
        }

        public static Quaternion LookRotation(Vector3 forward)
        {
            return LookRotation(forward, Vector3.UnitY);
        }

        public static Vector3 QuaternionToEulerAngles(Quaternion q)
        {
            // Convert to radians
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch;
            if (MathF.Abs(sinp) >= 1)
                pitch = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
            else
                pitch = MathF.Asin(sinp);

            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

            // Convert radians to degrees and return as Vector3 (X = pitch, Y = yaw, Z = roll)
            return new Vector3(
                pitch * (180f / MathF.PI),
                yaw * (180f / MathF.PI),
                roll * (180f / MathF.PI)
            );
        }

        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat((b - a + 180f), 360f) - 180f;
            return a + delta * Clamp01(t);
        }

        public static float Repeat(float t, float length)
        {
            return t - (float)Math.Floor(t / length) * length;
        }

        public static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }

    public class RandomHelper
    {
        private static readonly Random _random = new Random();

        public static float Range(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public static int Range(int min, int max)
        {
            return (int)(_random.Next(min,max));
        }
    }
}
