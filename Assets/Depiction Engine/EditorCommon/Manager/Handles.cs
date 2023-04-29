// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class Handles
    {
        private static FieldInfo _s_FreeMoveHandleHash;
        public static int s_FreeMoveHandleHash 
        {
            get 
            {
                _s_FreeMoveHandleHash ??= typeof(UnityEditor.Handles).GetField("s_FreeMoveHandleHash", BindingFlags.Static | BindingFlags.NonPublic);
                return (int)_s_FreeMoveHandleHash.GetValue(null);
            }
        }

        public static Vector3 PositionHandle(Vector3 position, Quaternion rotation)
        {
            return UnityEditor.Handles.PositionHandle(position, rotation);
        }

        public static Quaternion RotationHandle(Quaternion rotation, Vector3 position)
        {
            return UnityEditor.Handles.RotationHandle(rotation, position);
        }

        public static Vector3 ScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation)
        {
            return UnityEditor.Handles.ScaleHandle(scale, position, rotation);
        }

        public static void TransformHandle(ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            UnityEditor.Handles.TransformHandle(ref position, ref rotation, ref scale);
        }
    }
}
#endif
