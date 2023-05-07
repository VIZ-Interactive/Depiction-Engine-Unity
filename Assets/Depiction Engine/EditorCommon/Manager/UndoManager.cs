// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace DepictionEngine.Editor
{
    public class UndoManager
    {
        [Flags]
        private enum UndoOperationType
        {
            Created = 1 << 0,
            FullObjectHierarchy = 1 << 1,
            Complete = 1 << 2,
            RecordObject = 1 << 3,
            RecordObjectWrapper = 1 << 4,
            CreateNewGroup = 1 << 5,
            SetCurrentGroupName = 1 << 6,
            SetActiveTransform = 1 << 7
        };

        private static List<UnityEngine.Object> _validatedObjects;

        private static bool _wasFirstUpdated;

        private static int _currentGroupIndex;
        private static string _currentGroupName;

        private static Dictionary<int, Tuple<UndoOperationType, UnityEngine.Object, object, Action>> _undoOperationsQueue;

        /// <summary>
        /// Dispatched at the same time as <see cref="UnityEditor.Undo.undoRedoPerformed"/>.
        /// </summary>
        public static Action UndoRedoPerformedEvent;

        public static void UpdateAllDelegates(bool isDisposing)
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            if (!isDisposing)
                Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private static void UndoRedoPerformed()
        {
            //Object.FindObjectsOfType does not include objects with hideFlags 'dontSave'
            MonoBehaviourDisposable[] monoBehaviourDisposables = Object.FindObjectsOfType<MonoBehaviourDisposable>(true);
            foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                monoBehaviourDisposable.UndoRedoPerformed();
            ScriptableObjectDisposable[] scriptableObjectDisposables = Object.FindObjectsOfType<ScriptableObjectDisposable>(true);
            foreach (ScriptableObjectDisposable scriptableObjectDisposable in scriptableObjectDisposables)
                scriptableObjectDisposable.UndoRedoPerformed();

            UndoRedoPerformedEvent?.Invoke();
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

        private static bool DetectEditorCurrentGroupChange()
        {
            bool currentGroupChanged = SetCurrentGroupName(Undo.GetCurrentGroupName());

            int index = Undo.GetCurrentGroup();
            if (_currentGroupIndex != index)
            {
                _currentGroupIndex = index;
                currentGroupChanged = true;
            }
            return currentGroupChanged;
        }

        public static string GetCurrentGroupName()
        {
            return currentGroupName;
        }

        /// <summary>
        /// Increment current group index and change its name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initializingContext"></param>
        public static void QueueCreateNewGroup(string name)
        {
            QueueUndoOperation(UndoOperationType.CreateNewGroup, null, name);
        }

        /// <summary>
        /// Increment current group index and change its name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initializingContext"></param>
        public static void CreateNewGroup(string name)
        {
            IncrementCurrentGroup();
            currentGroupName = name;
        }

        /// <summary>
        /// Set the name of the current undo group.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void QueueSetCurrentGroupName(string name)
        {
            QueueUndoOperation(UndoOperationType.SetCurrentGroupName, null, name);
        }

        /// <summary>
        /// Set the name of the current undo group.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool SetCurrentGroupName(string name)
        {
            if (_currentGroupName == name)
                return false;

            _currentGroupName = name;

            SetUnityCurrentGroupName(name);

            return true;
        }

        private static string currentGroupName
        {
            get => _currentGroupName;
            set => SetCurrentGroupName(value);
        }

        public static int currentGroupIndex
        {
            get => _currentGroupIndex;
        }

        private static bool SetUnityCurrentGroupName(string name)
        {
            if (Undo.GetCurrentGroupName() != name)
            {
                Undo.SetCurrentGroupName(name);
                DetectEditorCurrentGroupChange();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Unity automatically groups undo operations by the current group index.
        /// </summary>
        public static void IncrementCurrentGroup()
        {
            Undo.IncrementCurrentGroup();
            DetectEditorCurrentGroupChange();
        }

        /// <summary>
        /// Performs the last undo operation but does not record a redo operation.
        /// </summary>
        public static void RevertAllInCurrentGroup()
        {
            Undo.RevertAllInCurrentGroup();
            DetectEditorCurrentGroupChange();
        }

        /// <summary>
        /// Ensure objects recorded using RecordObject or RecordObjects are registered as an undoable action. In most cases there is no reason to invoke FlushUndoRecordObjects since it's automatically done right after mouse-up and certain other events that conventionally marks the end of an action.
        /// </summary>
        public static void FlushUndoRecordObjects()
        {
            Undo.FlushUndoRecordObjects();
            DetectEditorCurrentGroupChange();
        }

        /// <summary>
        /// Collapses all undo operation up to group index together into one step.
        /// </summary>
        /// <param name="groupIndex"></param>
        public static void CollapseUndoOperations(int groupIndex)
        {
            Undo.CollapseUndoOperations(groupIndex);
            DetectEditorCurrentGroupChange();
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="disposeContext "></param>
        public static bool QueueRegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                QueueRegisterCreatedObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool QueueRegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                QueueRegisterCreatedObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static void QueueRegisterCreatedObjectUndo(UnityEngine.Object objectToUndo)
        {
            QueueUndoOperation(UndoOperationType.Created, objectToUndo);
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="disposeContext "></param>
        public static bool RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                RegisterCreatedObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                RegisterCreatedObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers an undo operation to undo the creation of an object.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static bool RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(objectToUndo))
            {
                if (IsInitialized(objectToUndo))
                {
                    Undo.RegisterCreatedObjectUndo(objectToUndo, Undo.GetCurrentGroupName());
                    UndoRedoPerformed(objectToUndo, UndoOperationType.Created);
                }
                else
                    QueueUndoOperation(UndoOperationType.Created, objectToUndo);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="disposeContext"></param>
        public static bool QueueRegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                QueueRegisterCreatedObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool QueueRegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                QueueRegisterCompleteObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static void QueueRegisterCompleteObjectUndo(UnityEngine.Object objectToUndo)
        {
            QueueUndoOperation(UndoOperationType.Complete, objectToUndo);
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="disposeContext"></param>
        public static bool RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                RegisterCompleteObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, InitializationContext initializingContext = InitializationContext.Editor)
        {
            if (IsValidContext(initializingContext))
            {
                RegisterCompleteObjectUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stores a copy of the object states on the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static bool RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(objectToUndo))
            {
                if (IsInitialized(objectToUndo))
                {
                    Undo.RegisterCompleteObjectUndo(objectToUndo, Undo.GetCurrentGroupName());
                    UndoRedoPerformed(objectToUndo, UndoOperationType.Complete);
                }
                else
                    QueueUndoOperation(UndoOperationType.Complete, objectToUndo);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Copy the states of a hierarchy of objects onto the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="disposeContext"></param>
        public static bool RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                RegisterFullObjectHierarchyUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Copy the states of a hierarchy of objects onto the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                RegisterFullObjectHierarchyUndo(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Copy the states of a hierarchy of objects onto the undo stack.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static bool RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(objectToUndo))
            {
                if (IsInitialized(objectToUndo))
                {
                    Undo.RegisterFullObjectHierarchyUndo(objectToUndo, Undo.GetCurrentGroupName());
                    UndoRedoPerformed(objectToUndo, UndoOperationType.FullObjectHierarchy);
                }
                else
                    QueueUndoOperation(UndoOperationType.FullObjectHierarchy, objectToUndo);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="disposeContext"></param>
        public static bool SetTransformParent(TransformBase transform, TransformBase newParent, DisposeContext disposeContext)
        {
            return SetTransformParent(transform, newParent, false, disposeContext);
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        public static bool SetTransformParent(TransformBase transform, TransformBase newParent, bool worldPositionStays, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                SetTransformParent(transform, newParent, worldPositionStays);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        public static bool SetTransformParent(TransformBase transform, TransformBase newParent, InitializationContext initializingContext)
        {
            return SetTransformParent(transform, newParent, false, initializingContext);
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        public static bool SetTransformParent(TransformBase transform, TransformBase newParent, bool worldPositionStays, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                SetTransformParent(transform, newParent, worldPositionStays);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        public static void SetTransformParent(TransformBase transform, TransformBase newParent, bool worldPositionStays = false)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(transform) && IsValidUnityObject(newParent))
            {
                if (IsInitialized(transform))
                {
                    Undo.SetTransformParent(transform != null ? transform.transform : null, newParent != null ? newParent.transform : null, worldPositionStays, Undo.GetCurrentGroupName());
                    UndoRedoPerformed(transform);
                }
            }
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="disposeContext"></param>
        public static bool SetTransformParent(Transform transform, Transform newParent, DisposeContext disposeContext)
        {
            return SetTransformParent(transform, newParent, false, disposeContext);
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        /// <param name="disposeContext"></param>
        public static bool SetTransformParent(Transform transform, Transform newParent, bool worldPositionStays, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                SetTransformParent(transform, newParent, worldPositionStays);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="initializingContext"></param>
        public static bool SetTransformParent(Transform transform, Transform newParent, InitializationContext initializingContext)
        {
            return SetTransformParent(transform, newParent, false, initializingContext);
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        /// <param name="initializingContext"></param>
        public static bool SetTransformParent(Transform transform, Transform newParent, bool worldPositionStays, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                SetTransformParent(transform, newParent, worldPositionStays);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the parent of transform to the new parent and records an undo operation.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        public static void SetTransformParent(Transform transform, Transform newParent, bool worldPositionStays = false)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(transform) && IsValidUnityObject(newParent))
            {
                Undo.SetTransformParent(transform, newParent, worldPositionStays, Undo.GetCurrentGroupName());
                UndoRedoPerformed(transform);
            }
        }

        /// <summary>
        /// Records multiple undoable objects in a single call. This is the same as calling Undo.RecordObject multiple times.
        /// </summary>
        /// <param name="objectsToUndo"></param>
        /// <param name="disposeContext"></param>
        public static bool RecordObjects(UnityEngine.Object[] objectsToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                RecordObjects(objectsToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Records multiple undoable objects in a single call. This is the same as calling Undo.RecordObject multiple times.
        /// </summary>
        /// <param name="objectsToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RecordObjects(UnityEngine.Object[] objectsToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                RecordObjects(objectsToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Records multiple undoable objects in a single call. This is the same as calling Undo.RecordObject multiple times.
        /// </summary>
        /// <param name="objectsToUndo"></param>
        public static void RecordObjects(UnityEngine.Object[] objectsToUndo)
        {
            foreach (UnityEngine.Object objectToUndo in objectsToUndo)
                RecordObject(objectToUndo);
        }

        /// <summary>
        /// Records any changes done on the object after the RecordObject function.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RecordObject(UnityEngine.Object objectToUndo, DisposeContext disposeContext)
        {
            if (IsValidContext(disposeContext))
            {
                RecordObject(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Records any changes done on the object after the RecordObject function.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <param name="initializingContext"></param>
        public static bool RecordObject(UnityEngine.Object objectToUndo, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                RecordObject(objectToUndo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Records any changes done on the object after the RecordObject function.
        /// </summary>
        /// <param name="objectToUndo"></param>
        public static bool RecordObject(UnityEngine.Object objectToUndo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(objectToUndo) && IsInitialized(objectToUndo))
            {
                Undo.RecordObject(objectToUndo, Undo.GetCurrentGroupName());
                UndoRedoPerformed(objectToUndo, UndoOperationType.RecordObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a component to the game object and registers an undo operation for this action.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="type"></param>
        /// <param name="component"></param>
        /// <param name="initializingContext"></param>
        /// <returns></returns>
        public static bool AddComponent(GameObject gameObject, Type type, ref Component component, InitializationContext initializingContext)
        {
            if (IsValidContext(initializingContext))
            {
                AddComponent(gameObject, type, ref component);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a component to the game object and registers an undo operation for this action.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="type"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public static bool AddComponent(GameObject gameObject, Type type, ref Component component)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(gameObject) && !SceneManager.IsEditorNamespace(type))
            {
                component = Undo.AddComponent(gameObject, type);
                UndoRedoPerformed(component);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Destroys the object and records an undo operation so that it can be recreated.
        /// </summary>
        /// <param name="objectToUndo"></param>
        /// <returns></returns>
        public static bool DestroyObjectImmediate(UnityEngine.Object objectToUndo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && IsValidUnityObject(objectToUndo))
            {
                Undo.DestroyObjectImmediate(objectToUndo);
                UndoRedoPerformed(objectToUndo);
                return true;
            }
            return false;
        }

        public static void QueueSetActiveTransform(TransformDouble transform)
        {
            QueueUndoOperation(UndoOperationType.SetActiveTransform, transform);
        }

        public static void QueueRecordObjectWrapper(UnityEngine.Object objectToUndo, Action callback)
        {
            QueueUndoOperation(UndoOperationType.RecordObjectWrapper, objectToUndo, null, callback);
        }

        public static void RecordObjectWrapper(UnityEngine.Object targetObject, Action callback)
        {
            try
            {
                if (callback != null)
                {
                    RecordObject(targetObject);

                    callback();

                    FlushUndoRecordObjects();
                }
            }
            catch (Exception)
            {

            }
        }

        private static bool IsValidUnityObject(UnityEngine.Object unityObject)
        {
            return unityObject is not null && unityObject != null && !IsEditorObject(unityObject);
        }

        private static bool IsInitialized(UnityEngine.Object unityObject)
        {
            if (unityObject is IScriptableBehaviour scriptableBehaviour)
            {
                return scriptableBehaviour.initialized || scriptableBehaviour.isFallbackValues;
            }
            return true;
        }

        private static bool IsValidContext(DisposeContext disposeContext)
        {
            return disposeContext == DisposeContext.Editor_Destroy;
        }

        private static bool IsValidContext(InitializationContext initializingContext)
        {
            return initializingContext == InitializationContext.Editor || initializingContext == InitializationContext.Editor_Duplicate;
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
            _validatedObjects ??= new List<UnityEngine.Object>();
            _validatedObjects.Add(value);
        }

        private static void QueueUndoOperation(UndoOperationType undoOperationType, UnityEngine.Object objectToUndo = null, object value = null, Action callback = null)
        {
            if (objectToUndo != null || undoOperationType == UndoOperationType.CreateNewGroup || undoOperationType == UndoOperationType.SetCurrentGroupName)
            {
                _undoOperationsQueue ??= new();

                int instanceId = 0;

                if (objectToUndo != null)
                    instanceId = objectToUndo.GetInstanceID();

                _undoOperationsQueue.TryGetValue(instanceId, out Tuple<UndoOperationType, UnityEngine.Object, object, Action > existingUndoOperationParam);
              
                if (existingUndoOperationParam != null)
                {
                    _undoOperationsQueue.Remove(instanceId);
                    undoOperationType |= existingUndoOperationParam.Item1;
                }

                _undoOperationsQueue.Add(instanceId, new(undoOperationType, objectToUndo, value, callback));
            }
        }

        private static void UndoRedoPerformed(UnityEngine.Object objectToUndo, UndoOperationType undoOperationType)
        {
            int objectInstanceId = objectToUndo.GetInstanceID();
            if (_undoOperationsQueue != null && _undoOperationsQueue.TryGetValue(objectInstanceId, out Tuple<UndoOperationType, UnityEngine.Object, object, Action> operationParam))
            {
                UndoOperationType existingUndoOperationType = operationParam.Item1;
                if (existingUndoOperationType.HasFlag(undoOperationType))
                {
                    existingUndoOperationType &= ~undoOperationType;
                    _undoOperationsQueue.Remove(objectInstanceId);
                    _undoOperationsQueue.Add(objectInstanceId, new(existingUndoOperationType, operationParam.Item2, operationParam.Item3, operationParam.Item4));
                }
            }
            Debug.LogError(objectToUndo +", "+undoOperationType);
            UndoRedoPerformed(objectToUndo);
        }

        private static void UndoRedoPerformed(UnityEngine.Object objectToUndo)
        {
            if (objectToUndo is IDisposable)
                (objectToUndo as IDisposable).MarkAsNotPoolable();
        }
     
        public static bool DetectEditorUndoRedoRegistered()
        {
            bool changesDetected = false;

            if (DetectEditorCurrentGroupChange() && _wasFirstUpdated)
            {
                if (!string.IsNullOrEmpty(currentGroupName))
                {
                    string[] splitCurrentGroupName = currentGroupName.Split();

                    if (splitCurrentGroupName[0] == "Paste")
                    {
                        if (splitCurrentGroupName.Length > 2 && splitCurrentGroupName[^1] == "Values")
                        {
                            if (_validatedObjects != null)
                            {
                                bool pastingComponentValues = false;

                                foreach (IJson validatedObject in _validatedObjects.Cast<IJson>())
                                {
                                    if (validatedObject.PasteComponentAllowed())
                                    {
                                        pastingComponentValues = true;
                                        InspectorManager.PastingComponentValues(validatedObject, JsonUtility.GetObjectJson(validatedObject) as JSONObject);
                                    }
                                }

                                if (pastingComponentValues)
                                    RevertAllInCurrentGroup();
                            }
                        }
                    }

                    //SiblingIndex is a typo in the UnityEngine I suppose.
                    if (splitCurrentGroupName.Length >= 2 && (splitCurrentGroupName[0] == "SiblingIndex" || splitCurrentGroupName[1] == "Index" || splitCurrentGroupName[1] == "Order") && splitCurrentGroupName[^1] == "Change")
                    {
                        //The added "d" is used to distinguish Unity 'Root / Sibling Order Change' action from the one we create. If there is no distinction then if the undo/redo actions are played again this code will be executed again and a new action will be recorded in the history erasing any subsequent actions.
                        SetCurrentGroupName(currentGroupName + "d");

                        List<TransformBase> affectedSiblings = new();

                        AddAffectSiblings(ref affectedSiblings);
                        Undo.PerformUndo();
                        AddAffectSiblings(ref affectedSiblings);
                        Undo.PerformRedo();

                        static void AddAffectSiblings(ref List<TransformBase> affectedSiblings)
                        {
                            foreach (GameObject selectedGameObject in UnityEditor.Selection.gameObjects)
                            {
                                if (selectedGameObject.transform.parent != null)
                                {
                                    foreach (Transform sibling in selectedGameObject.transform.parent)
                                        AddSibling(ref affectedSiblings, sibling);
                                }
                                else
                                {
                                    foreach (GameObject sibling in SceneManager.Instance().GetRootGameObjects())
                                        AddSibling(ref affectedSiblings, sibling.transform);
                                }

                                static void AddSibling(ref List<TransformBase> affectedSiblings, Transform sibling)
                                {
                                    TransformBase siblingTransform = sibling.GetComponent<TransformBase>();
                                    if (siblingTransform != Disposable.NULL && !affectedSiblings.Contains(siblingTransform))
                                        affectedSiblings.Add(siblingTransform);
                                }
                            }
                        }

                        foreach (TransformBase affectedSibling in affectedSiblings)
                            affectedSibling.MarkAsNotPoolable();
                    }
                }

                changesDetected = true;
            }

            _validatedObjects?.Clear();

            return changesDetected;
        }

        public static void ProcessQueuedOperations()
        {
            if (_undoOperationsQueue != null && _undoOperationsQueue.Count > 0)
            {
                for (int i = 0 ; i < _undoOperationsQueue.Count; i++)
                {
                    Tuple<UndoOperationType, UnityEngine.Object, object, Action> operationParam = _undoOperationsQueue.ElementAt(i).Value;
              
                    if (operationParam.Item1.HasFlag(UndoOperationType.Created))
                        RegisterCreatedObjectUndo(operationParam.Item2);
                    if (operationParam.Item1.HasFlag(UndoOperationType.FullObjectHierarchy))
                        RegisterFullObjectHierarchyUndo(operationParam.Item2);
                    if (operationParam.Item1.HasFlag(UndoOperationType.Complete))
                        RegisterCompleteObjectUndo(operationParam.Item2);
                    if (operationParam.Item1.HasFlag(UndoOperationType.RecordObject))
                        RecordObject(operationParam.Item2);
                    if (operationParam.Item1.HasFlag(UndoOperationType.RecordObjectWrapper))
                        RecordObjectWrapper(operationParam.Item2, operationParam.Item4);
                    if (operationParam.Item1.HasFlag(UndoOperationType.CreateNewGroup))
                        CreateNewGroup((string)operationParam.Item3);
                    if (operationParam.Item1.HasFlag(UndoOperationType.SetCurrentGroupName))
                        SetCurrentGroupName((string)operationParam.Item3);
                    if (operationParam.Item1.HasFlag(UndoOperationType.SetActiveTransform))
                    {
                        Selection.activeTransform = (TransformDouble)operationParam.Item2;
                        DetectEditorCurrentGroupChange();
                    }
                }

                _undoOperationsQueue.Clear();
            }

            _wasFirstUpdated = true;
        }
    }
}
#endif