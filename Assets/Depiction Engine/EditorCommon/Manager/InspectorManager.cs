// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System.Collections.Generic;

namespace DepictionEngine.Editor
{
    public class InspectorManager
    {
        private static List<UnityEngine.Object> _resettingObjects = new();
        public static void Resetting(UnityEngine.Object scriptableBehaviour)
        {
            _resettingObjects.Add(scriptableBehaviour);
        }

        private static List<(IJson, JSONObject)> _pastingComponentValuesToObjects = new();
        public static void PastingComponentValues(IJson iJson, JSONObject json)
        {
            if (json[nameof(IProperty.id)] != null)
                json.Remove(nameof(IProperty.id));

            _pastingComponentValuesToObjects.Add((iJson, json));
        }

        public static void DetectInspectorActions()
        {
            DetectReset();

            DetectPasteComponentValues();
        }

        private static void DetectReset()
        {
            if (_resettingObjects != null && _resettingObjects.Count > 0)
            {
                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _resettingObjects[0] as IScriptableBehaviour;
                string groupName = "Reset " + (_resettingObjects.Count == 1 ? firstUnityObject.name : "Object") + " " + firstUnityObject.GetType().Name;

                UndoManager.SetCurrentGroupName(groupName);
                UndoManager.RecordObjects(_resettingObjects.ToArray());

                foreach (UnityEngine.Object unityObject in _resettingObjects)
                {
                    if (unityObject is IProperty)
                        (unityObject as IProperty).InspectorReset();
                }

                _resettingObjects.Clear();
            }
        }

        private static void DetectPasteComponentValues()
        {
            if (_pastingComponentValuesToObjects != null && _pastingComponentValuesToObjects.Count > 0)
            {
                SceneManager.sceneExecutionState = SceneManager.ExecutionState.PastingComponentValues;

                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _pastingComponentValuesToObjects[0].Item1;
                //'Pasted' is not a typo it is used to distinguish Unity 'Paste Component Values' action from the one we create. If there is no distinction then if the undo/redo actions are played again this code will be executed again and a new action will be recorded in the history erasing any subsequent actions.
                string groupName = "Pasted " + firstUnityObject.GetType().FullName + " Values";

                UndoManager.SetCurrentGroupName(groupName);

                UnityEngine.Object[] recordObjects = new UnityEngine.Object[_pastingComponentValuesToObjects.Count];
                for (int i = 0; i < recordObjects.Length; i++)
                    recordObjects[i] = _pastingComponentValuesToObjects[i].Item1 as UnityEngine.Object;
                UndoManager.RecordObjects(recordObjects);

                foreach ((IJson, JSONObject) pastingComponentValuesToObject in _pastingComponentValuesToObjects)
                {
                    IJson iJson = pastingComponentValuesToObject.Item1;
                    JSONObject json = pastingComponentValuesToObject.Item2;

                    SceneManager.StartUserContext();

                    JsonUtility.ApplyJsonToObject(iJson, json);

                    SceneManager.EndUserContext();
                }
                _pastingComponentValuesToObjects.Clear();

                SceneManager.sceneExecutionState = SceneManager.ExecutionState.None;
            }
        }
    }
}
#endif
