// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class InternalEditorUtility
    {
        public static Bounds CalculateSelectionBounds(bool usePivotOnlyForParticles, bool onlyUseActiveSelection, bool ignoreEditableField)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));

            MethodInfo calculateSelectionBoundsMethodInfo = assembly.GetType("UnityEditorInternal.InternalEditorUtility").GetMethod("CalculateSelectionBounds", new Type[] { typeof(bool), typeof(bool), typeof(bool) });

            return (Bounds)calculateSelectionBoundsMethodInfo.Invoke(null, new object[] { usePivotOnlyForParticles, onlyUseActiveSelection, ignoreEditableField }); ;
        }
    }
}
#endif
