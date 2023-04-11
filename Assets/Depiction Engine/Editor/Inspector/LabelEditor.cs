// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    [CustomEditor(typeof(Label), true)]
    public class LabelEditor : EditorBase
    {
        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (UnityEditor.Selection.objects.Length > 1)
                return;

            Label label = (Label)target;
            if (label.useEndCoordinate)
            {
                EditorGUI.BeginChangeCheck();

                SceneCamera sceneCamera = Camera.current as SceneCamera;
                if (sceneCamera != Disposable.NULL)
                {
                    UIVisual uiVisual = label.GetVisualForCamera(sceneCamera);
                    if (uiVisual != Disposable.NULL)
                    {
                        Quaternion uiVisualRotation = label.transform.rotation * label.GetUIVisualLocalRotation(sceneCamera);

                        PropertyInfo endCoordinatePropertyInfo = null;
                        Vector3Double endCoordinate = TransformDouble.AddOrigin(Handles.PositionHandle(TransformDouble.SubtractOrigin(label.endCoordinate), uiVisualRotation));
                        if (EditorGUI.EndChangeCheck())
                            endCoordinatePropertyInfo = MemberUtility.GetMemberInfoFromMemberName<PropertyInfo>(typeof(Label), nameof(Label.endCoordinate));

                        PropertyInfo textCurvePropertyInfo = null;
                        List<Vector3Double> textCurve = new List<Vector3Double>();
                        foreach (Vector3Double point in label.textCurve)
                        {
                            EditorGUI.BeginChangeCheck();
                            textCurve.Add(TransformDouble.AddOrigin(Handles.PositionHandle(TransformDouble.SubtractOrigin(point), uiVisualRotation)));
                            if (EditorGUI.EndChangeCheck())
                                textCurvePropertyInfo = MemberUtility.GetMemberInfoFromMemberName<PropertyInfo>(typeof(Label), nameof(Label.textCurve));
                        }

                        if (endCoordinatePropertyInfo != null || textCurvePropertyInfo != null)
                        {
                            if (endCoordinatePropertyInfo != null)
                                SetPropertyValue(label, endCoordinatePropertyInfo, endCoordinate);

                            if (textCurvePropertyInfo != null)
                                SetPropertyValue(label, textCurvePropertyInfo, textCurve);
                        }
                    }
                }
            }
        }
    }
}