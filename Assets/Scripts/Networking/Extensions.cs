using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.Utils
{
    public static class Extensions
    {
        public static void PutV3(this NetDataWriter writer, Vector3 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        public static Vector3 GetVector3(this NetDataReader reader)
        {
            Vector3 v = new Vector3();
            v.x = reader.GetFloat();
            v.y = reader.GetFloat();
            v.z = reader.GetFloat();
            return v;
        }

        public static void PutV2(this NetDataWriter writer, Vector2 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
        }

        public static Vector2 GetVector2(this NetDataReader reader)
        {
            Vector2 v = new Vector2();
            v.x = reader.GetFloat();
            v.y = reader.GetFloat();
            return v;
        }

        public static void PutQuat(this NetDataWriter writer, Quaternion quat)
        {
            writer.Put(quat.x);
            writer.Put(quat.y);
            writer.Put(quat.z);
            writer.Put(quat.w);
        }

        public static Quaternion GetQuaternion(this NetDataReader reader)
        {
            Quaternion v = new Quaternion();
            v.x = reader.GetFloat();
            v.y = reader.GetFloat();
            v.z = reader.GetFloat();
            v.w = reader.GetFloat();
            return v;
        }

        public static T GetRandomElement<T>(this T[] array)
        {
            return array[Random.Range(0, array.Length)];
        }
    }
}