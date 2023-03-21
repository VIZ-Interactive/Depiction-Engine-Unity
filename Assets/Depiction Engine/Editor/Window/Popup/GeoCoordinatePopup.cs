// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    [InitializeOnLoad]
    public class GeoCoordinatePopup : EditorWindow
    {
        [SerializeField]
        public GeoCoordinate3Double _geoCoordinate;

        public void FocusOnLatControl()
        {
            _focusOnLatConttrol = true;
        }

        bool _focusOnLatConttrol;
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            SerializedObject serializedObject = new SerializedObject(this);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_geoCoordinate"), true);
            if (_focusOnLatConttrol)
            {
                EditorGUI.FocusTextInControl("GeoCoordinate_Lat");
                _focusOnLatConttrol = false;
            }

            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Move", GUILayout.Width(100.0f)))
            {
                SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
                if (sceneViewDouble != Disposable.NULL && sceneViewDouble.alignViewToGeoAstroObject != Disposable.NULL)
                {
                    sceneViewDouble.pivot = sceneViewDouble.alignViewToGeoAstroObject.GetPointFromGeoCoordinate(_geoCoordinate);
                    sceneViewDouble.rotation = sceneViewDouble.alignViewToGeoAstroObject.GetUpVectorFromGeoCoordinate(_geoCoordinate) * QuaternionDouble.Euler(70.0d, 0.0d, 0.0d);
                    sceneViewDouble.cameraDistance = 1000.0d;
                }
                Close();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100.0f)))
                Close();
            GUILayout.EndHorizontal();
        }
    }
}
