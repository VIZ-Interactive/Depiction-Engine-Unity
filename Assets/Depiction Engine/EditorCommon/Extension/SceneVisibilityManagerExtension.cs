// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Linq;

namespace DepictionEngine.Editor
{
    public static class SceneVisibilityManagerExtension
    {
        private static Type[] _refEditorTypes;
        private static Type _refSceneVisibilityStateType;
        private static System.Reflection.MethodInfo _refSceneVisibilityState_GetInstanceMethod;
        private static System.Reflection.MethodInfo _refSceneVisibilityState_SetGameObjectHiddenMethod;
        private static System.Reflection.PropertyInfo _refSceneVisibilityState_VisibilityActiveProperty;

        private static void BuildReflectionCache()
        {
            try
            {
                if (_refEditorTypes != null)
                    return;

                _refEditorTypes = typeof(UnityEditor.Editor).Assembly.GetTypes();
                if (_refEditorTypes != null && _refEditorTypes.Length > 0)
                {
                    _refSceneVisibilityStateType = _refEditorTypes.FirstOrDefault(t => t.Name == "SceneVisibilityState");
                    if (_refSceneVisibilityStateType != null)
                    {
                        _refSceneVisibilityState_GetInstanceMethod = _refSceneVisibilityStateType.GetMethod(
                            "GetInstance",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        _refSceneVisibilityState_SetGameObjectHiddenMethod = _refSceneVisibilityStateType.GetMethod(
                            "SetGameObjectHidden",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        _refSceneVisibilityState_VisibilityActiveProperty = _refSceneVisibilityStateType.GetProperty("visibilityActive");
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Based on the info found here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneView/SceneVisibilityState.bindings.cs#L20
        /// and here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneVisibilityManager.cs
        /// </summary>
        /// <returns></returns>
        public static UnityEngine.Object GetSceneVisibilityStateViaReflection()
        {
            try
            {
                BuildReflectionCache();
                return (UnityEngine.Object)_refSceneVisibilityState_GetInstanceMethod.Invoke(null, new object[] { });
            }
            catch (Exception)
            {
                // fail silently
                return null;
            }
        }

        /// <summary>
        /// Based on the info found here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneView/SceneVisibilityState.bindings.cs#L20
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isHidden"></param>
        /// <param name="includeChildren"></param>
        /// <returns>True if the reflection code has executed without exceptions.</returns>
        public static bool SetGameObjectHiddenNoUndoViaReflection(UnityEngine.GameObject gameObject, bool isHidden, bool includeChildren)
        {
            try
            {
                BuildReflectionCache();
                UnityEngine.Object state = GetSceneVisibilityStateViaReflection();
                if (state != null)
                    _refSceneVisibilityState_SetGameObjectHiddenMethod.Invoke(state, new object[] { gameObject, isHidden, includeChildren });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Return true is visiblity is active, otherwise false<br />
        /// Notice: It will return false if reflection failed.
        /// </summary>
        /// <returns></returns>
        public static bool IsVisibilityActiveViaReflection()
        {
            try
            {
                BuildReflectionCache();
                UnityEngine.Object state = GetSceneVisibilityStateViaReflection();
                if (state != null)
                    return (bool)_refSceneVisibilityState_VisibilityActiveProperty.GetValue(state, null);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
#endif
