// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TransformDouble), true)]
    public class TransformDoubleEditor : EditorBase
    {
        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                SceneCamera sceneCamera = Camera.current as SceneCamera;

                if (sceneCamera != Disposable.NULL)
                {
                    SceneManager sceneManager = SceneManager.Instance(false);
                    if (sceneManager != Disposable.NULL)
                    {
                        SceneViewDouble sceneViewDouble = SceneViewDouble.GetSceneViewDouble(sceneCamera);

                        SceneViewDouble lastActiveSceneViewDouble = SceneViewDouble.lastActiveOrMouseOverSceneViewDouble;
                        if (lastActiveSceneViewDouble != Disposable.NULL)
                        {
                            bool showMockHandles = false;
                            if (UnityEditor.Selection.transforms.Length == Selection.GetTransformDoubleSelectionCount())
                                showMockHandles = sceneViewDouble.toolsHidden;

                            if (sceneViewDouble != Disposable.NULL && showMockHandles && sceneViewDouble.handleCount == 0)
                            {
                                if (Selection.ApplyOriginShifting(sceneCamera.GetOrigin()))
                                {
                                    Vector3 handlePosition;

                                    if (Tools.GetHandlePosition(out handlePosition))
                                    {
                                        Quaternion handleRotation = Tools.handleRotation;

                                        Vector3 handleSize = Vector3.one;

                                        switch (Tools.current)
                                        {
                                            case Tool.Move:
                                                Handles.PositionHandle(handlePosition, handleRotation);
                                                break;
                                            case Tool.Rotate:
                                                Handles.RotationHandle(handleRotation, handlePosition);
                                                break;
                                            case Tool.Scale:
                                                Handles.ScaleHandle(handleSize, handlePosition, handleRotation);
                                                break;
                                            case Tool.Transform:
                                                Handles.TransformHandle(ref handlePosition, ref handleRotation, ref handleSize);
                                                break;
                                        }
                                    }
                                }

                                sceneViewDouble.handleCount++;
                            }
                        }
                    }
                }
            }
        }
    }
}
