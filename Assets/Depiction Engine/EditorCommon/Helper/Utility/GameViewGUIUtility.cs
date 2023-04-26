// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public static class GameViewGUIUtility
    {
        private static Type _gameViewGUIType;
        private static MethodInfo _updateFrameTimeMethodInfo;
        private static FieldInfo _m_MaxFrameTimeFieldInfo;

        public static float GetFrameRate()
        {
            float framerate = 0.0f;

            _gameViewGUIType ??= typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewGUI");
            if (_gameViewGUIType != null)
            {
                _updateFrameTimeMethodInfo ??= _gameViewGUIType.GetMethod("UpdateFrameTime", BindingFlags.Static | BindingFlags.NonPublic);
                if (_updateFrameTimeMethodInfo != null)
                {
                    _m_MaxFrameTimeFieldInfo ??= _gameViewGUIType.GetField("m_MaxFrameTime", BindingFlags.Static | BindingFlags.NonPublic);

                    if (_m_MaxFrameTimeFieldInfo != null)
                    {
                        _updateFrameTimeMethodInfo.Invoke(null, null);
                        framerate = 1.0f / Mathf.Max((float)_m_MaxFrameTimeFieldInfo.GetValue(null), 1.0e-5f);
                    }
                }
            }

            return framerate;
        }
    }
}
#endif