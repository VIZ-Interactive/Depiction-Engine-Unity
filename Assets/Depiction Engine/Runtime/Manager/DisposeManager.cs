// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Manager handling the pooling or destruction of objects.
    /// </summary>
    public static class DisposeManager
    {
        /// <summary>
        /// The different type of destroy delay. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// The destroy will happen immediately. <br/><br/>
        /// <b><see cref="Delayed"/>:</b> <br/>
        /// The destroy will happen during the late update. <br/><br/>
        /// <b><see cref="Delayed_Late"/>:</b> <br/>
        /// The destroy will happend right after the Delayed destroy.
        /// </summary> 
        public enum DisposeDelay
        {
            None,
            Delayed,
            Delayed_Late
        };

        /// <summary>
        /// The different types of dispose context. <br/><br/>
        /// <b><see cref="Unknown"/>:</b> <br/>
        /// The context is unknown. <br/><br/>
        /// <b><see cref="Programmatically"/>:</b> <br/>
        /// The dispose was triggered programmatically. <br/><br/>
        /// <b><see cref="Editor"/>:</b> <br/>
        /// The destroy was triggered in the editor. <br/><br/>
        /// <b><see cref="Editor_UndoRedo"/>:</b> <br/>
        /// The destroy was triggered in the editor as a result of an undo or redo action. <br/><br/>
        /// </summary> 
        public enum DisposeContext
        {
            Unknown,
            Programmatically,
            Editor,
            Editor_UndoRedo,
        };

        public static DisposeContext disposingContext = DisposeContext.Unknown;

        public static LinkedList<Tuple<object, DisposeContext>> DelayedDispose;
        public static LinkedList<Tuple<object, DisposeContext>> DelayedDisposeLate;

        /// <summary>
        /// Will dispose of an object by sending it to the pool if pooling is enabled, it implements <see cref="DepictionEngine.IDisposable"/> and no <see cref="DepictionEngine.DisposeManager.DisposeContext"/> is specified. Otherwise it will be destroyed.
        /// </summary>
        /// <param name="obj">The object to dispose.</param>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        /// <param name="disposeDelay">Specify whether the destroy part of the dispose should happen immediatly or later.</param>
        public static void Dispose(object obj, DisposeContext disposeContext = DisposeContext.Unknown, DisposeDelay disposeDelay = DisposeDelay.None)
        {
            if (obj is null || obj.Equals(null) || SceneManager.IsSceneBeingDestroyed())
                return;

            //Force Destroy if object is MonoBehaviour or Pooling is not enabled
            PoolManager poolManager = PoolManager.Instance(false);
            if (obj is MonoBehaviour || poolManager == Disposable.NULL || !poolManager.enablePooling)
                disposeContext = disposeContext == DisposeContext.Editor ? DisposeContext.Editor : DisposeContext.Programmatically;

            //Capture the current last Node before we trigger the Disposing() to maintain proper order in the LinkedList
            LinkedListNode<Tuple<object, DisposeContext>> currentDelayedDisposeNode = null;
            if (disposeDelay == DisposeDelay.Delayed)
                currentDelayedDisposeNode = DelayedDispose?.Last;
            if (disposeDelay == DisposeDelay.Delayed_Late)
                currentDelayedDisposeNode = DelayedDisposeLate?.Last;

            List<object> disposing = new();

            IterateOverAllObjects(obj, (obj) =>
            {
                DisposeContext destroyingContext = disposeContext;
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;

                    if (destroyingContext == DisposeContext.Unknown)
                    {
                        Component[] components = go.GetComponents<Component>();
                        IDisposable disposable = go.GetDisposableInComponents(components);

                        if (!IsNullOrDisposed(disposable) && disposable.destroyingContext == DisposeContext.Unknown)
                        {
                            foreach (Component component in components)
                            {
                                bool invalidForPool = false;
                                if (component is MonoBehaviourDisposable)
                                {
#if UNITY_EDITOR
                                    invalidForPool = (component as MonoBehaviourDisposable).hasEditorUndoRedo;
#endif
                                }
                                else if (!((disposable is Object && component is Transform) || (disposable is Visual && (component is Transform || component is MeshFilter || component is MeshRenderer || component is Collider))))
                                    invalidForPool = true;

                                if (invalidForPool)
                                {
                                    destroyingContext = DisposeContext.Programmatically;
                                    break;
                                }
                            }
                        }
                        else
                            destroyingContext = DisposeContext.Programmatically;
                    }

                    bool monoBehaviourDisposableDisposing = false;
                    MonoBehaviourDisposable[] monoBehaviourDisposables = go.GetComponents<MonoBehaviourDisposable>();
                    if (monoBehaviourDisposables.Length != 0)
                    {
                        foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                        {
                            DestroyingContext(() =>
                            {
                                if (monoBehaviourDisposable.OnDisposing(destroyingContext) && monoBehaviourDisposable.UpdateDestroyingContext())
                                    monoBehaviourDisposableDisposing = true;
                            }, destroyingContext);
                        }
                    }
                    else
                        monoBehaviourDisposableDisposing = true;

                    if (monoBehaviourDisposableDisposing)
                        disposing.Add(obj);
                }
                else if (obj is IDisposable)
                {
                    IDisposable disposable = obj as IDisposable;

#if UNITY_EDITOR
                    if (destroyingContext == DisposeContext.Unknown)
                    {
                        if(disposable is IScriptableBehaviour && (disposable as IScriptableBehaviour).hasEditorUndoRedo)
                            destroyingContext = DisposeContext.Programmatically;
                    }
#endif

                    DestroyingContext(() => 
                    {
                        if (disposable.OnDisposing(destroyingContext) && disposable.UpdateDestroyingContext())
                            disposing.Add(obj);
                    }, destroyingContext);
                }
                else
                    disposing.Add(obj);
            });

            foreach (object disposingObject in disposing)
            {
                if (disposeDelay == DisposeDelay.None)
                    Dispose(disposingObject, disposeContext);
                else
                    AddDelayedDispose(new Tuple<object, DisposeContext>(disposingObject, disposeContext), currentDelayedDisposeNode, disposeDelay == DisposeDelay.Delayed_Late);
            }
        }

        private static void Dispose(object obj, DisposeContext defaultDisposeContext)
        {
            if (IsNullOrDisposed(obj))
                return;

            IDisposable disposable = null;
            if (obj is GameObject)
                disposable = (obj as GameObject).GetDisposableInComponents();
            else if (obj is IDisposable)
                disposable = obj as IDisposable;

            DisposeContext disposeContext = defaultDisposeContext;
            if (!IsNullOrDisposed(disposable))
                disposeContext = disposable.destroyingContext;
            else if (obj is UnityEngine.Object)
            {
                if (disposeContext == DisposeContext.Unknown)
                    disposeContext = DisposeContext.Programmatically;
            }

            if (disposeContext != DisposeContext.Unknown)
            {
                if (obj is UnityEngine.Object)
                    Destroy(obj as UnityEngine.Object, disposeContext);
                else if (!IsNullOrDisposed(disposable))
                    disposable.OnDisposedInternal(disposeContext);
            }
            else if (!IsNullOrDisposed(disposable))
            {
                bool pooled = false;

                PoolManager poolManager = PoolManager.Instance();
                if (poolManager != null)
                { 
                    poolManager.AddToPool(disposable);
                    pooled = true;
                }

                if (obj is GameObject)
                    IterateOverAllMonoBehaviourDisposable(obj as GameObject, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDisposedInternal(disposeContext, pooled); });
                else
                    disposable.OnDisposedInternal(disposeContext, pooled);

                disposable.disposedComplete = true;
            } 
        }

        /// <summary>
        /// Destroy any UnityEngine.Object.
        /// </summary>
        /// <param name="unityObject">The UnityEngine.Object.</param>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        public static void Destroy(UnityEngine.Object unityObject, DisposeContext disposeContext = DisposeContext.Programmatically)
        {
            if (unityObject == null)
                return;

            if (disposeContext == DisposeContext.Unknown)
                disposeContext = DisposeContext.Programmatically;

            DestroyingContext(() =>
            {
#if UNITY_EDITOR
                if (disposeContext != DisposeContext.Editor || !Editor.UndoManager.DestroyObjectImmediate(unityObject))
                    GameObject.DestroyImmediate(unityObject, true);
#else
                GameObject.Destroy(unityObject);
#endif

                //MonoBehaviourDisposable who have not been activated yet will not trigger OnDestroy so we do it manually
                IterateOverAllObjects(unityObject, 
                    (obj) => 
                    { 
                        IterateOverAllMonoBehaviourDisposable(obj as UnityEngine.Object, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDestroy(); }); 
                    });
            
            }, disposeContext);

#if UNITY_EDITOR
            //Force refresh the hierarchy window to remove the Destroyed objects
            UnityEditor.EditorApplication.DirtyHierarchyWindowSorting();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDisposing(object obj)
        {
            if (obj is null)
                return true;

            if (obj is IDisposable)
            {
                IDisposable disposable = obj as IDisposable;
                if (disposable.IsDisposing())
                    return true;
#if UNITY_EDITOR
                //Edge case where an IDisposable might have been Destroyed during an Editor(Undo/Redo) Operation and the OnDestroy() as not been called yet
                if (IsUnityNull(disposable))
                    return true;
#endif
            }

            if (IsUnityNull(obj))
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNullOrDisposed(object obj)
        {
            if (obj is null)
                return true;

            if (obj is IDisposable)
            {
                IDisposable disposable = obj as IDisposable;
                if (disposable.IsDisposed())
                    return true;
#if UNITY_EDITOR
                //Edge case where an IDisposable might have been Destroyed during an Editor(Undo/Redo) Operation and the OnDestroy() as not been called yet
                if (IsUnityNull(disposable))
                    return true;
#endif
            }

            if (IsUnityNull(obj))
                return true;

            return false;
        }

        private static bool IsUnityNull(object obj)
        {
            return obj is UnityEngine.Object && (obj as UnityEngine.Object) == null;
        }

        public static void DestroyingContext(Action callback, DisposeContext destroyingContext)
        {
            DisposeContext lastDestroyingType = DisposeManager.disposingContext;
            DisposeManager.disposingContext = destroyingContext;
            callback();
            DisposeManager.disposingContext = lastDestroyingType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TriggerOnDestroyIfNull(IScriptableBehaviour scriptableBehaviour)
        {
            if (scriptableBehaviour is not null && (scriptableBehaviour as UnityEngine.Object) == null)
            {
                DestroyingContext(() => { scriptableBehaviour.OnDestroy(); }, DisposeContext.Editor_UndoRedo);
                return true;
            }
            return false;
        }

        private static void IterateOverAllObjects(object obj, Action<object> callback)
        {
            if (obj is GameObject)
            {
                GameObject go = obj as GameObject;
                if (go != null)
                {
                    for (int i = go.transform.childCount - 1; i >= 0; i--)
                        IterateOverAllObjects(go.transform.GetChild(i).gameObject, callback);

                    callback(go);
                }
            }
            else
            {
                if (obj != null)
                    callback(obj);
            }
        }

        private static void IterateOverAllMonoBehaviourDisposable(UnityEngine.Object obj, Action<MonoBehaviourDisposable> callback)
        {
            if (obj is GameObject)
                IterateOverAllMonoBehaviourDisposable(obj as GameObject, callback);
            else if (obj is MonoBehaviourDisposable)
                callback(obj as MonoBehaviourDisposable);
        }

        private static void IterateOverAllMonoBehaviourDisposable(GameObject go, Action<MonoBehaviourDisposable> callback)
        {
            MonoBehaviourDisposable[] monoBehaviourDisposables = go.GetComponents<MonoBehaviourDisposable>();
            foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                callback(monoBehaviourDisposable);
        }

        private static void AddDelayedDispose(Tuple<object, DisposeContext> value, LinkedListNode<Tuple<object, DisposeContext>> addAfterNode = null, bool late = false)
        {
            if (late)
            {
                DelayedDisposeLate ??= new LinkedList<Tuple<object, DisposeContext>>();
              
                if (addAfterNode == null)
                    DelayedDisposeLate.AddFirst(value);
                else
                    DelayedDisposeLate.AddAfter(addAfterNode, value);
            }
            else
            {
                DelayedDispose ??= new LinkedList<Tuple<object, DisposeContext>>();
      
                if (addAfterNode == null)
                    DelayedDispose.AddFirst(value);
                else
                    DelayedDispose.AddAfter(addAfterNode, value);
            }
        }

        public static void InvokeActions()
        {
            InvokeAction(ref DelayedDispose, "DelayedDispose", SceneManager.ExecutionState.DelayedDispose);

            InvokeAction(ref DelayedDisposeLate, "DelayedDisposeLate", SceneManager.ExecutionState.DelayedDisposeLate);
        }

        private static void InvokeAction(ref LinkedList<Tuple<object, DisposeContext>> action, string actionName, SceneManager.ExecutionState sceneExecutionState)
        {
            if (action != null)
            {
#if UNITY_EDITOR
                int delegatesCount = action.Count;
#endif

                SceneManager.sceneExecutionState = sceneExecutionState;
                foreach (Tuple<object, DisposeContext> node in action)
                    Dispose(node.Item1, node.Item2);
                SceneManager.sceneExecutionState = SceneManager.ExecutionState.None;

#if UNITY_EDITOR
                if (delegatesCount != action.Count)
                    Debug.LogError("DelayedDispose delegate list Changed!");
#endif
                action.Clear();
            }
        }
    }
}
