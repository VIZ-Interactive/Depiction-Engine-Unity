﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace DepictionEngine.Editor
{
    public class UndoManager
    {
        private enum UndoOperationType
        {
            Created, 
            Complete
        };

        private static List<UnityEngine.Object> _validatedObjects;

        private static bool _wasFirstUpdated;

        private static int _currentGroupIndex;
        private static string _currentGroupName;
        private static bool _groupIncrementedDuringFrame;

        private static bool _processingEditorCopyPasteOrDuplicateOrDragDropComponent;
        private static List<Tuple<UnityEngine.Object, UndoOperationType>> _undoOperationsQueue;

        public static Action UndoRedoPerformedEvent;

        public static void UpdateAllDelegates(bool isDisposing)
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            if (!isDisposing)
                Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private static void UndoRedoPerformed()
        {
            if (UndoRedoPerformedEvent != null)
                UndoRedoPerformedEvent();
        }

        private static void QueueUndoOperation(UnityEngine.Object objectToUndo, UndoOperationType undoType)
        {
            if (_undoOperationsQueue == null)
                _undoOperationsQueue = new List<Tuple<UnityEngine.Object, UndoOperationType>>();

            _undoOperationsQueue.Add(Tuple.Create(objectToUndo, undoType));
        }

        public static void PostInitialize()
        {
            //Increment the Undo Operation Group to stop recording changes after a CopyPaste/Duplicate/DragDrop
            if (_processingEditorCopyPasteOrDuplicateOrDragDropComponent)
            {
                _processingEditorCopyPasteOrDuplicateOrDragDropComponent = false;
                IncrementCurrentGroup();
            }
        }

        public static void PreGetMonoBehaviourInstance()
        {
            if (!_groupIncrementedDuringFrame)
                IncrementCurrentGroup();
        }

        public static void PostGetMonoBehaviourInstance()
        {
            FlushUndoRecordObjects();
        }

        private static MethodInfo _getRecordsMethodInfo;
        public static void GetRecords(List<string> undoRecords, List<string> redoRecords)
        {
            if (_getRecordsMethodInfo == null)
                _getRecordsMethodInfo = typeof(Undo).GetMethod("GetRecords", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(List<string>), typeof(List<string>) }, null);
            _getRecordsMethodInfo.Invoke(null, new object[] { undoRecords, redoRecords });
        }

        /// <summary>
        /// Unity automatically groups undo operations by the current group index.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentGroup()
        {
            return Undo.GetCurrentGroup();
        }

        private static bool CaptureEditorCurrentGroupChange()
        {
            bool currentGroupNameChanged = SetCurrentGroupName(Undo.GetCurrentGroupName());

            int index = Undo.GetCurrentGroup();
            if (_currentGroupIndex != index)
            {
                _currentGroupIndex = index;
                _groupIncrementedDuringFrame = true;
            }

            return currentGroupNameChanged;
        }

        public static string GetCurrentGroupName()
        {
            return currentGroupName;
        }

        /// <summary>
        /// Increment current group index and change its name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initializationState"></param>
        public static void CreateNewGroup(string name, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Editor)
        {
            if (initializationState == InstanceManager.InitializationContext.Editor || initializationState == InstanceManager.InitializationContext.Editor_Duplicate)
            {
                IncrementCurrentGroup();
                currentGroupName = name;
            }
        }

        private static string currentGroupName
        {
            get { return _currentGroupName; }
            set { SetCurrentGroupName(value); }
        }

        /// <summary>
        /// Set the name of the current undo group.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetCurrentGroupName(string value)
        {
            if (_currentGroupName == value)
                return false;

            _currentGroupName = value;

            SetUnityCurrentGroupName(value);

            return true;
        }

        private static void CorrectCurrentGroupName()
        {
            //Correct illegitimate GroupName Change made by Unity by falling back to the last specified GroupName
            //string name = GetCurrentGroupName();
            //if (Undo.GetCurrentGroupName() != name)
            //    SetUnityCurrentGroupName(name);
        }

        private static string SetUnityCurrentGroupName(string name)
        {
            if (Undo.GetCurrentGroupName() != name)
                Undo.SetCurrentGroupName(name);
            return name;
        }

        /// <summary>
        /// Unity automatically groups undo operations by the current group index.
        /// </summary>
        public static void IncrementCurrentGroup()
        {
            Undo.IncrementCurrentGroup();
            _groupIncrementedDuringFrame = true;
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializationState"></param>
        public static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Editor)
        {
            if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo) && objectToUndo is UnityEngine.Object && (initializationState == InstanceManager.InitializationContext.Editor || initializationState == InstanceManager.InitializationContext.Editor_Duplicate))
            {
                //Problem : When a CopyPaste or Duplicate or DragDrop_Component operation is performed in the Editor an Undo operation is recorded but the Undo.GetCurrentGroupName will not be updated until after the Awake() is called. Generating a new Undo operation at this time will associate it with the wrong Group
                //Fix: We Queue the operation to perform it later(at the Beginning of the next Update) when the Undo Group name as been updated
                if (initializationState == InstanceManager.InitializationContext.Editor_Duplicate)
                {
                    QueueUndoOperation(objectToUndo, UndoOperationType.Created);
                    _processingEditorCopyPasteOrDuplicateOrDragDropComponent = true;
                }
                else
                    RegisterCreatedObjectUndo(objectToUndo);
            }
        }

        private static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo)
        {
            Undo.RegisterCreatedObjectUndo(objectToUndo, Undo.GetCurrentGroupName());
            CorrectCurrentGroupName();
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializationState"></param>
        public static void RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Editor)
        {
            if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo) && objectToUndo is UnityEngine.Object && (initializationState == InstanceManager.InitializationContext.Editor || initializationState == InstanceManager.InitializationContext.Editor_Duplicate))
            {
                //Problem : When a CopyPaste or Duplicate or DragDrop_Component operation is performed in the Editor an Undo operation is recorded but the Undo.GetCurrentGroupName will not be updated until after the Awake() is called. Generating a new Undo operation at this time will associate it with the wrong Group
                //Fix: We Queue the operation to perform it later(at the Beginning of the next Update) when the Undo Group name as been updated
                if (initializationState == InstanceManager.InitializationContext.Editor_Duplicate)
                {
                    QueueUndoOperation(objectToUndo, UndoOperationType.Complete);
                    _processingEditorCopyPasteOrDuplicateOrDragDropComponent = true;
                }
                else
                    RegisterCompleteObjectUndo(objectToUndo);
            }
        }

        private static void RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo)
        {
            Undo.RegisterCompleteObjectUndo(objectToUndo, Undo.GetCurrentGroupName());
            CorrectCurrentGroupName();
        }

        /// <summary>
        /// Copy the states of a hierarchy of objects onto the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static void RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo)
        {
            Undo.RegisterFullObjectHierarchyUndo(objectToUndo, Undo.GetCurrentGroupName());
            CorrectCurrentGroupName();
        }

        /// <summary>
        /// Records any changes done on the object after the RecordObject function.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static void RecordObject(UnityEngine.Object objectToUndo)
        {
            if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo))
            {
                Undo.RecordObject(objectToUndo, Undo.GetCurrentGroupName());
                CorrectCurrentGroupName();
            }
        }

        /// <summary>
        /// Records multiple undoable objects in a single call. This is the same as calling Undo.RecordObject multiple times.
        /// </summary>
        /// <param name="objectToUndos"></param>
        public static void RecordObjects(UnityEngine.Object[] objectToUndos)
        {
            int objectToUndosCount = 0;

            foreach (UnityEngine.Object objectToUndo in objectToUndos)
            {
                if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo))
                    objectToUndosCount++;
            }

            if (objectToUndosCount != 0)
            {
                UnityEngine.Object[] filteredObjetToUndos = objectToUndos;

                if (objectToUndos.Length != objectToUndosCount)
                {
                    filteredObjetToUndos = new UnityEngine.Object[objectToUndosCount];
                    for (int i = 0; i < objectToUndos.Length; i++)
                    {
                        UnityEngine.Object objectToUndo = objectToUndos[i];
                        if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo))
                            filteredObjetToUndos[i] = objectToUndo;
                    }
                }

                Undo.RecordObjects(filteredObjetToUndos, Undo.GetCurrentGroupName());
                CorrectCurrentGroupName();
            }
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        public static void SetTransformParent(Transform transform, Transform newParent)
        {
            Undo.SetTransformParent(transform, newParent, Undo.GetCurrentGroupName());
            CorrectCurrentGroupName();
        }

        /// <summary>
        /// Destroys the object and records an undo operation so that it can be recreated.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <returns></returns>
        public static bool DestroyObjectImmediate(UnityEngine.Object objectToUndo)
        {
            if (!IsDisposing(objectToUndo) && !IsEditorObject(objectToUndo))
            {
                Undo.DestroyObjectImmediate(objectToUndo);

                UpdateCurrentGroupNameIfCapturingInInspector(objectToUndo as IDisposable);

                CorrectCurrentGroupName();

                return true;
            }

            return false;
        }

        private static bool IsDisposing(UnityEngine.Object objectToUndo)
        {
            return Object.ReferenceEquals(objectToUndo, null) || objectToUndo.Equals(Disposable.NULL);
        }

        /// <summary>
        /// Adds a component to the game object and registers an undo operation for this action.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Component AddComponent(GameObject gameObject, Type type)
        {
            Component component = null;

            if (!SceneManager.IsEditorNamespace(type))
            {
                component = Undo.AddComponent(gameObject, type);

                UpdateCurrentGroupNameIfCapturingInInspector(gameObject.GetDisposableInComponents());

                CorrectCurrentGroupName();
            }

            return component;
        }

        /// <summary>
        /// Performs the last undo operation but does not record a redo operation.
        /// </summary>
        public static void RevertAllInCurrentGroup()
        {
            Undo.RevertAllInCurrentGroup();
        }

        private static void UpdateCurrentGroupNameIfCapturingInInspector(IDisposable objectToUndo)
        {
            //Are there still inspector Operation that can trigger the Creation or Destruction of component?
            //Should use the QueueUndoOperation approach instead?
            //if (!Disposable.IsDisposed(objectToUndo) && objectToUndo is IScriptableBehaviour)
            //{
            //    IScriptableBehaviour scriptableBehaviour = objectToUndo as IScriptableBehaviour;
            //    if (scriptableBehaviour.IsEditorChange() && scriptableBehaviour.isEditorInspectorChange)
            //        _currentGroupName = "Inspector";
            //}
        }

        /// <summary>
        /// Ensure objects recorded using RecordObject or RecordObjects are registered as an undoable action. In most cases there is no reason to invoke FlushUndoRecordObjects since it's automatically done right after mouse-up and certain other events that conventionally marks the end of an action.
        /// </summary>
        public static void FlushUndoRecordObjects()
        {
            Undo.FlushUndoRecordObjects();
        }

        /// <summary>
        /// Collapses all undo operation up to group index together into one step.
        /// </summary>
        /// <param name="groupIndex"></param>
        public static void CollapseUndoOperations(int groupIndex)
        {
            Undo.CollapseUndoOperations(groupIndex);
        }

        //This should not be necessary since no Undo operations should be performed on Editor UnityObjects  
        private static bool IsEditorObject(UnityEngine.Object unityObject)
        {
            if (unityObject is TransformBase)
            {
                TransformBase transform = unityObject as TransformBase;
                if (transform.objectBase != Disposable.NULL)
                    unityObject = transform.objectBase;
            }
            return SceneManager.IsEditorNamespace(unityObject.GetType());
        }

        public static void Validating(UnityEngine.Object value)
        {
            if (_validatedObjects == null)
                _validatedObjects = new List<UnityEngine.Object>();
            _validatedObjects.Add(value);
        }

        public static void PerformUndoRedoPropertyChange<T>(Action<T> callback, ref T field, ref T lastField)
        {
            T newValue = field;
            field = lastField;
            if (callback != null)
                callback(newValue);
        }

        public static void Update()
        {
            _groupIncrementedDuringFrame = false;

            if (CaptureEditorCurrentGroupChange() && _wasFirstUpdated)
            {
                if (_validatedObjects != null && currentGroupName != null)
                {
                    string[] splitCurrentGroupName = currentGroupName.Split();
                    if (splitCurrentGroupName.Length > 2 && splitCurrentGroupName[0] == "Paste" && splitCurrentGroupName[splitCurrentGroupName.Length - 1] == "Values")
                    {
                        foreach (IJson validatedObject in _validatedObjects)
                            SceneManager.PastingComponentValues(validatedObject, validatedObject.GetJson());

                        RevertAllInCurrentGroup();
                    }
                }
            }

            if (_validatedObjects != null)
                _validatedObjects.Clear();

            if (_undoOperationsQueue != null && _undoOperationsQueue.Count > 0)
            {
                foreach (Tuple<UnityEngine.Object, UndoOperationType> operationParam in _undoOperationsQueue)
                {
                    if (operationParam.Item1 != null)
                    {
                        if (operationParam.Item2 == UndoOperationType.Created)
                            RegisterCreatedObjectUndo(operationParam.Item1);
                        if (operationParam.Item2 == UndoOperationType.Complete)
                            RegisterCompleteObjectUndo(operationParam.Item1);
                    }
                }

                _undoOperationsQueue.Clear();
            }

            _wasFirstUpdated = true;
        }
    }
}
#endif