// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    [InitializeOnLoad]
    public class APIKeyInputsPopup : EditorWindow
    {
        public enum DialogCloseState { Ok, Cancel};

        [SerializeField]
        public List<string> labels;
        [SerializeField]
        public List<string> inputs;

        public Action<DialogCloseState, List<string>> ClosedEvent;

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            SerializedObject serializedObject = new SerializedObject(this);

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200;

            for (int i = 0; i < inputs.Count; i++)
                inputs[i] = EditorGUILayout.TextField(labels[i], inputs[i]);

            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.BeginHorizontal();

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 10;
            style.alignment = TextAnchor.LowerCenter;
            EditorGUILayout.LabelField("The default keys are for experimental purpose only.", style);

            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Create", GUILayout.Width(100.0f)))
                ClosePopup(DialogCloseState.Ok, inputs);
            if (GUILayout.Button("Cancel", GUILayout.Width(100.0f)))
                ClosePopup(DialogCloseState.Cancel, inputs);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void ClosePopup(DialogCloseState closeState, List<string> inputs)
        {
            Close();
            ClosedEvent?.Invoke(closeState, inputs);
        }
    }
}
