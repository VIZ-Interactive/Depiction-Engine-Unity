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
        public enum DestroyDelay
        {
            None,
            Delayed,
            Delayed_Late
        };

        /// <summary>
        /// The different types of destroy context. <br/><br/>
        /// <b><see cref="Unknown"/>:</b> <br/>
        /// The context is unknown. <br/><br/>
        /// <b><see cref="Programmatically"/>:</b> <br/>
        /// The destroy was triggered programmatically. <br/><br/>
        /// <b><see cref="Editor"/>:</b> <br/>
        /// The destroy was triggered in the editor. <br/><br/>
        /// <b><see cref="Editor_UndoRedo"/>:</b> <br/>
        /// The destroy was triggered in the editor as a result of an undo or redo action. <br/><br/>
        /// </summary> 
        public enum DestroyContext
        {
            Unknown,
            Programmatically,
            Editor,
            Editor_UndoRedo,
        };

        public static DestroyContext destroyingContext = DestroyContext.Unknown;

        public static LinkedList<Tuple<object, DestroyContext>> DelayedDispose;
        public static LinkedList<Tuple<object, DestroyContext>> DelayedDisposeLate;

        /// <summary>
        /// Will dispose of an object by sending it to the pool if pooling is enabled, it implements <see cref="DepictionEngine.IDisposable"/> and no <see cref="DepictionEngine.DisposeManager.DestroyContext"/> is specified. Otherwise it will be destroyed.
        /// </summary>
        /// <param name="obj">The object to dispose.</param>
        /// <param name="destroyContext">The context under which the object is being destroyed.</param>
        /// <param name="destroyDelay">Specify whether the destroy part of the dispose should happen immediatly or later.</param>
        public static void Dispose(object obj, DestroyContext destroyContext = DestroyContext.Unknown, DestroyDelay destroyDelay = DestroyDelay.None)
        {
            if (Object.ReferenceEquals(obj, null) || obj.Equals(null) || SceneManager.IsSceneBeingDestroyed())
                return;

            //Force Destroy if object is MonoBehaviour or Pooling is not enabled
            PoolManager poolManager = PoolManager.Instance(false);
            if (obj is MonoBehaviour || poolManager == Disposable.NULL || !poolManager.enablePooling)
                destroyContext = destroyContext == DestroyContext.Editor ? DestroyContext.Editor : DestroyContext.Programmatically;

            //Capture the current last Node before we trigger the Disposing() to maintain proper order in the LinkedList
            LinkedListNode<Tuple<object, DestroyContext>> currentDelayedDisposeNode = null;
            if (destroyDelay == DestroyDelay.Delayed)
                currentDelayedDisposeNode = DelayedDispose != null ? DelayedDispose.Last : null;
            if (destroyDelay == DestroyDelay.Delayed_Late)
                currentDelayedDisposeNode = DelayedDisposeLate != null ? DelayedDisposeLate.Last : null;

            List<object> disposing = new List<object>();

            IterateOverAllDisposable(obj, (obj) =>
            {
                DestroyContext destroyingContext = destroyContext;
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;

                    if (destroyingContext == DestroyContext.Unknown)
                    {
                        Component[] components = go.GetComponents<Component>();
                        IDisposable disposable = go.GetDisposableInComponents(components);

                        if (!IsNullOrDisposed(disposable) && disposable.destroyingContext == DestroyContext.Unknown)
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
                                    destroyingContext = DestroyContext.Programmatically;
                                    break;
                                }
                            }
                        }
                        else
                            destroyingContext = DestroyContext.Programmatically;
                    }

                    bool monoBehaviourDisposableDisposing = false;
                    MonoBehaviourDisposable[] monoBehaviourDisposables = go.GetComponents<MonoBehaviourDisposable>();
                    if (monoBehaviourDisposables.Length != 0)
                    {
                        foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                        {
                            DestroyingContext(() =>
                            {
                                if (monoBehaviourDisposable.OnDisposing() && monoBehaviourDisposable.OnDispose())
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
                    if (destroyingContext == DestroyContext.Unknown)
                    {
                        if(disposable is IScriptableBehaviour && (disposable as IScriptableBehaviour).hasEditorUndoRedo)
                            destroyingContext = DestroyContext.Programmatically;
                    }
#endif

                    DestroyingContext(() => 
                    {
                        if (disposable.OnDisposing() && disposable.OnDispose())
                            disposing.Add(obj);
                    }, destroyingContext);
                }
                else
                    disposing.Add(obj);
            });

            foreach (object disposingObject in disposing)
            {
                if (destroyDelay == DestroyDelay.None)
                    Dispose(disposingObject, destroyContext);
                else
                    AddDelayedDispose(new Tuple<object, DestroyContext>(disposingObject, destroyContext), currentDelayedDisposeNode, destroyDelay == DestroyDelay.Delayed_Late);
            }
        }

        private static void Dispose(object obj, DestroyContext defaultDestroyContext)
        {
            if (IsNullOrDisposed(obj))
                return;

            IDisposable disposable = null;
            if (obj is GameObject)
                disposable = (obj as GameObject).GetDisposableInComponents();
            else if (obj is IDisposable)
                disposable = obj as IDisposable;

            DestroyContext destroyContext = defaultDestroyContext;
            if (!IsNullOrDisposed(disposable))
                destroyContext = disposable.destroyingContext;
            else if (obj is UnityEngine.Object)
            {
                if (destroyContext == DestroyContext.Unknown)
                    destroyContext = DestroyContext.Programmatically;
            }

            if (destroyContext != DestroyContext.Unknown)
            {
                if (obj is UnityEngine.Object)
                    Destroy(obj as UnityEngine.Object, destroyContext);
                else if (!IsNullOrDisposed(disposable))
                    disposable.OnDisposedInternal(destroyContext);
            }
            else if (!IsNullOrDisposed(disposable))
            {
                PoolManager poolManager = PoolManager.Instance();
                if (poolManager != null)
                    poolManager.AddToPool(disposable);

                if (obj is GameObject)
                    IterateOverAllMonoBehaviourDisposable(obj as GameObject, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDisposedInternal(destroyContext); });
                else
                    disposable.OnDisposedInternal(destroyContext);

                disposable.disposedComplete = true;
            } 
        }

        /// <summary>
        /// Destroy any UnityEngine.Object.
        /// </summary>
        /// <param name="unityObject">The UnityEngine.Object.</param>
        /// <param name="destroyContext">The context under which the object is being destroyed.</param>
        public static void Destroy(UnityEngine.Object unityObject, DestroyContext destroyContext = DestroyContext.Programmatically)
        {
            if (unityObject == null)
                return;

            if (destroyContext == DestroyContext.Unknown)
                destroyContext = DestroyContext.Programmatically;

            DestroyingContext(() =>
            {
#if UNITY_EDITOR
                if (destroyContext != DestroyContext.Editor || !Editor.UndoManager.DestroyObjectImmediate(unityObject))
                    GameObject.DestroyImmediate(unityObject, true);
#else
                GameObject.Destroy(unityObject);
#endif

                //MonoBehaviourDisposable who have not been activated yet will not trigger OnDestroy so we do it manually
                IterateOverAllDisposable(unityObject, 
                    (obj) => 
                    { 
                        IterateOverAllMonoBehaviourDisposable(obj as UnityEngine.Object, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDestroy(); }); 
                    });
            
            }, destroyContext);

#if UNITY_EDITOR
            //Force refresh the hierarchy window to remove the Destroyed objects
            UnityEditor.EditorApplication.DirtyHierarchyWindowSorting();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDisposing(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
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
            if (Object.ReferenceEquals(obj, null))
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

        public static void DestroyingContext(Action callback, DestroyContext destroyingContext)
        {
            DestroyContext lastDestroyingType = DisposeManager.destroyingContext;
            DisposeManager.destroyingContext = destroyingContext;
            callback();
            DisposeManager.destroyingContext = lastDestroyingType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TriggerOnDestroyIfNull(IScriptableBehaviour scriptableBehaviour)
        {
            if (!Object.ReferenceEquals(scriptableBehaviour, null) && (scriptableBehaviour as UnityEngine.Object) == null)
            {
                DestroyingContext(() => { scriptableBehaviour.OnDestroy(); }, DestroyContext.Editor_UndoRedo);
                return true;
            }
            return false;
        }

        private static void IterateOverAllDisposable(object obj, Action<object> callback)
        {
            if (obj is GameObject)
            {
                GameObject go = obj as GameObject;
                if (go != null)
                {
                    for (int i = go.transform.childCount - 1; i >= 0; i--)
                        IterateOverAllDisposable(go.transform.GetChild(i).gameObject, callback);

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

        private static void AddDelayedDispose(Tuple<object, DestroyContext> value, LinkedListNode<Tuple<object, DestroyContext>> addAfterNode = null, bool late = false)
        {
            if (late)
            {
                if (DelayedDisposeLate == null)
                    DelayedDisposeLate = new LinkedList<Tuple<object, DestroyContext>>();
              
                if (addAfterNode == null)
                    DelayedDisposeLate.AddFirst(value);
                else
                    DelayedDisposeLate.AddAfter(addAfterNode, value);
            }
            else
            {
                if (DelayedDispose == null)
                    DelayedDispose = new LinkedList<Tuple<object, DestroyContext>>();
      
                if (addAfterNode == null)
                    DelayedDispose.AddFirst(value);
                else
                    DelayedDispose.AddAfter(addAfterNode, value);
            }
        }

        public static void InvokeActions()
        {
            InvokeAction(ref DelayedDispose, "DelayedDispose", SceneManager.UpdateExecutionState.DelayedDispose);

            InvokeAction(ref DelayedDisposeLate, "DelayedDisposeLate", SceneManager.UpdateExecutionState.DelayedDisposeLate);
        }

        private static void InvokeAction(ref LinkedList<Tuple<object, DestroyContext>> action, string actionName, SceneManager.UpdateExecutionState sceneExecutionState)
        {
            if (action != null)
            {
#if UNITY_EDITOR
                int delegatesCount = action.Count;
#endif

                SceneManager.sceneExecutionState = sceneExecutionState;
                foreach (Tuple<object, DestroyContext> node in action)
                    Dispose(node.Item1, node.Item2);
                SceneManager.sceneExecutionState = SceneManager.UpdateExecutionState.None;

#if UNITY_EDITOR
                if (delegatesCount != action.Count)
                    Debug.LogError("DelayedDispose delegate list Changed!");
#endif
                action.Clear();
            }
        }
    }
}
