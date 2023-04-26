// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class ComponentUtility
    {
        private static MethodInfo _moveComponentRelativeToComponentMethod;
        public static void MoveComponentRelativeToComponent(Component component, Component targetComponent, bool aboveTarget)
        {
            if (UnityEditor.PrefabUtility.GetPrefabAssetType(component) == UnityEditor.PrefabAssetType.NotAPrefab)
            {
                //Avoid 'Don't try to find visible index of Invisible component' error by making the transform visible
                HideFlags lasHideFlags = component.hideFlags;
                HideFlags lasTransformHideFlags = component.transform.hideFlags;

                component.hideFlags = component.transform.hideFlags = HideFlags.None;

                if (_moveComponentRelativeToComponentMethod == null)
                    _moveComponentRelativeToComponentMethod = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditorInternal.ComponentUtility").GetMethod("MoveComponentRelativeToComponent", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Component), typeof(Component), typeof(bool), typeof(bool) }, null);
                _moveComponentRelativeToComponentMethod.Invoke(_moveComponentRelativeToComponentMethod, new object[] { component, targetComponent.transform, aboveTarget, false });

                component.hideFlags = lasHideFlags;
                component.transform.hideFlags = lasTransformHideFlags;
            }
        }
    }
}
#endif