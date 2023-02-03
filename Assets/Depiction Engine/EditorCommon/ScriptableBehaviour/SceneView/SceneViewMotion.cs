﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class SceneViewMotion
    {
        public static void ResetMotion()
        {
            GetUnitySceneViewMotionType().GetMethod("ResetMotion", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }

        public static Type GetUnitySceneViewMotionType()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));

            return assembly.GetType("UnityEditor.SceneViewMotion");
        }

        private static bool PatchedPreHandleMouseUp(SceneView view, int id, int button, int clickCount)
        {
            if (GUIUtility.hotControl == id)
            {
                bool movePivotToClickedPoint = true;

                FieldInfo shortcutKeyFieldInfo = GetUnitySceneViewMotionType().GetField("shortcutKey", BindingFlags.Static | BindingFlags.NonPublic);
                if (shortcutKeyFieldInfo != null)
                {
                    //2022.2
                    KeyCode shortcutKey = (KeyCode)shortcutKeyFieldInfo.GetValue(null);
                    movePivotToClickedPoint = shortcutKey == KeyCode.None || shortcutKey == (Event.current.keyCode == KeyCode.None ? KeyCode.Mouse0 + Event.current.button : Event.current.keyCode);
                }
                else
                {
                    //2022.1
                    FieldInfo s_CurrentStateFieldInfo = GetUnitySceneViewMotionType().GetField("s_CurrentState", BindingFlags.Static | BindingFlags.NonPublic);
                    if (s_CurrentStateFieldInfo != null)
                    {
                        Type motionStateType = GetUnitySceneViewMotionType().GetNestedType("MotionState", BindingFlags.NonPublic);
                        movePivotToClickedPoint = button == 2 && (int)s_CurrentStateFieldInfo.GetValue(null) != (int)Enum.Parse(motionStateType, "kDragging");
                    }
                }

                if (movePivotToClickedPoint)
                {
                    RenderingManager renderingManager = RenderingManager.Instance(false);
                    if (renderingManager != Disposable.NULL && renderingManager.originShifting)
                    {
                        SceneViewDouble sceneViewDouble = SceneViewDouble.GetSceneViewDouble(view);
                        if (sceneViewDouble != Disposable.NULL)
                            TransformDouble.ApplyOriginShifting(sceneViewDouble.camera.GetOrigin());
                    }
                }
            }
            return true;
        }
    }
}
#endif
