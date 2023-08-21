// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class Tools
    {
        public static bool hidden
        {
            get => UnityEditor.Tools.hidden;
            set => UnityEditor.Tools.hidden = value;
        }

        public static UnityEditor.Tool current
        {
            get => UnityEditor.Tools.current;
            set => UnityEditor.Tools.current = value;
        }

        public static UnityEditor.PivotMode pivotMode
        {
            get => UnityEditor.Tools.pivotMode;
            set => UnityEditor.Tools.pivotMode = value;
        }

        public static Quaternion handleRotation
        {
            get => UnityEditor.Tools.handleRotation;
            set => UnityEditor.Tools.handleRotation = value;
        }

        public static bool handlePositionComputed
        {
            set => typeof(UnityEditor.Tools).GetField("s_HandlePositionComputed", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        }

        public static bool vertexDragging
        {
            get => (bool)typeof(UnityEditor.Tools).GetField("vertexDragging", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        public static Vector3 handleOffset
        {
            get => (Vector3)typeof(UnityEditor.Tools).GetField("handleOffset", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        public static Vector3 GetHandlePosition()
        {
            GetHandlePosition(out Vector3 position);
            return position;
        }

        public static bool GetHandlePosition(out Vector3 position)
        {
            bool isValidHandlePosition = true;

            if (pivotMode == UnityEditor.PivotMode.Center || current == UnityEditor.Tool.Rect)
            {
                foreach(Transform transform in UnityEditor.Selection.transforms)
                {
                    if (transform.position.magnitude > 40000000.0f)
                    {
                        isValidHandlePosition = false;
                        break;
                    }
                }
            }

            if (isValidHandlePosition)
            {
                position = (Vector3)typeof(UnityEditor.Tools).GetMethod("GetHandlePosition", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

                if (float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
                    isValidHandlePosition = false;
            }
            else
                position = Vector3.positiveInfinity;

            return isValidHandlePosition;
        }


        public static bool GetHandlePosition(out Vector3Double position)
        {
            OriginShiftSnapshot originShiftSnapshot = null;

            bool isValidHandlePosition = Selection.MoveOriginCloserToPoint(GetHandlePosition, (position) =>
            {
                originShiftSnapshot = TransformDouble.GetOriginShiftSnapshot();

                return GetMostPreciseHandle(position);
            });

            position = isValidHandlePosition ? TransformDouble.AddOrigin(GetHandlePosition()) : Vector3Double.positiveInfinity;

            if (originShiftSnapshot != null)
                TransformDouble.ApplyOriginShifting(originShiftSnapshot);

            return isValidHandlePosition;
        }

        public static Vector3 GetMostPreciseHandle(Vector3 position)
        {
            if (pivotMode == UnityEditor.PivotMode.Center || current == UnityEditor.Tool.Rect)
            {
                UnityEditor.PivotMode lastPivotMode = pivotMode;
                UnityEditor.Tool lastCurrent = current;

                pivotMode = UnityEditor.PivotMode.Pivot;
                current = UnityEditor.Tool.Move;

                position = GetHandlePosition();

                pivotMode = lastPivotMode;
                current = lastCurrent;
            }

            return position;
        } 
    }
}
#endif
