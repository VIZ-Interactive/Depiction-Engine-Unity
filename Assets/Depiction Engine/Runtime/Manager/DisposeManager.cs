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
        /// <b><see cref="Editor_Unknown"/>:</b> <br/>
        /// The destroy was triggered in the editor by the context is unknown.
        /// </summary> 
        public enum DestroyContext
        {
            Unknown,
            Programmatically,
            Editor,
            Editor_UndoRedo,
            Editor_Unknown,
        };

        public static DestroyContext destroyingState = DestroyContext.Editor_Unknown;

        public static LinkedList<Tuple<object, DestroyContext>> DelayedDispose;
        public static LinkedList<Tuple<object, DestroyContext>> DelayedDisposeLate;

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
                DestroyContext destroyingState = destroyContext;
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;

                    if (destroyingState == DestroyContext.Unknown)
                    {
                        Component[] components = go.GetComponents<Component>();
                        IDisposable disposable = go.GetDisposableInComponents(components);

                        if (!IsNullOrDisposed(disposable) && disposable.destroyingState == DestroyContext.Unknown)
                        {
                            foreach (Component component in components)
                            {
                                bool invalidForPool = false;
                                if (component is MonoBehaviourBase)
                                {
#if UNITY_EDITOR
                                    invalidForPool = (component as MonoBehaviourBase).hasEditorUndoRedo;
#endif
                                }
                                else if (!((disposable is Object && component is Transform) || (disposable is Visual && (component is Transform || component is MeshFilter || component is MeshRenderer || component is Collider))))
                                    invalidForPool = true;

                                if (invalidForPool)
                                {
                                    destroyingState = DestroyContext.Programmatically;
                                    break;
                                }
                            }
                        }
                        else
                            destroyingState = DestroyContext.Programmatically;
                    }

                    bool monoBehaviourBaseDisposing = false;
                    MonoBehaviourBase[] monoBehaviourBases = go.GetComponents<MonoBehaviourBase>();
                    if (monoBehaviourBases.Length != 0)
                    {
                        foreach (MonoBehaviourBase monoBehaviourBase in monoBehaviourBases)
                        {
                            DestroyingState(() =>
                            {
                                if (monoBehaviourBase.OnDisposing() && monoBehaviourBase.OnDispose())
                                    monoBehaviourBaseDisposing = true;
                            }, destroyingState);
                        }
                    }
                    else
                        monoBehaviourBaseDisposing = true;

                    if (monoBehaviourBaseDisposing)
                        disposing.Add(obj);
                }
                else if (obj is IDisposable)
                {
                    IDisposable disposable = obj as IDisposable;

#if UNITY_EDITOR
                    if (destroyingState == DestroyContext.Unknown)
                    {
                        if(disposable is IScriptableBehaviour && (disposable as IScriptableBehaviour).hasEditorUndoRedo)
                            destroyingState = DestroyContext.Programmatically;
                    }
#endif

                    DestroyingState(() => 
                    {
                        if (disposable.OnDisposing() && disposable.OnDispose())
                            disposing.Add(obj);
                    }, destroyingState);
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

        private static void Dispose(object obj, DestroyContext defaultDestroyState)
        {
            if (IsNullOrDisposed(obj))
                return;

            IDisposable disposable = null;
            if (obj is GameObject)
                disposable = (obj as GameObject).GetDisposableInComponents();
            else if (obj is IDisposable)
                disposable = obj as IDisposable;

            DestroyContext destroyState = defaultDestroyState;
            if (!IsNullOrDisposed(disposable))
                destroyState = disposable.destroyingState;
            else if (obj is UnityEngine.Object)
            {
                if (destroyState == DestroyContext.Unknown)
                    destroyState = DestroyContext.Programmatically;
            }

            if (destroyState != DestroyContext.Unknown)
            {
                if (obj is UnityEngine.Object)
                    Destroy(obj as UnityEngine.Object, destroyState);
                else if (!IsNullOrDisposed(disposable))
                    disposable.OnDisposedInternal(destroyState);
            }
            else if (!IsNullOrDisposed(disposable))
            {
                PoolManager poolManager = PoolManager.Instance();
                if (poolManager != null)
                    poolManager.AddToPool(disposable);

                if (obj is GameObject)
                    IterateOverAllMonoBehaviourBase(obj as GameObject, (monoBehaviourBase) => { monoBehaviourBase.OnDisposedInternal(destroyState); });
                else
                    disposable.OnDisposedInternal(destroyState);

                disposable.disposedComplete = true;
            } 
        }
      
        public static void Destroy(UnityEngine.Object unityObject, DestroyContext destroyState = DestroyContext.Programmatically)
        {
            if (unityObject == null)
                return;

            if (destroyState == DestroyContext.Unknown)
                destroyState = DestroyContext.Programmatically;

            DestroyingState(() =>
            {
#if UNITY_EDITOR
                if (destroyState != DestroyContext.Editor || !Editor.UndoManager.DestroyObjectImmediate(unityObject))
                    GameObject.DestroyImmediate(unityObject, true);
#else
                GameObject.Destroy(unityObject);
#endif

                //MonoBehaviourBase who have not been activated yet will not trigger OnDestroy so we do it manually
                IterateOverAllDisposable(unityObject, 
                    (obj) => 
                    { 
                        IterateOverAllMonoBehaviourBase(obj as UnityEngine.Object, (monoBehaviourBase) => { monoBehaviourBase.OnDestroy(); }); 
                    });
            
            }, destroyState);

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

        private static void DestroyingState(Action callback, DestroyContext destroyingState)
        {
            DestroyContext lastDestroyingType = DisposeManager.destroyingState;
            DisposeManager.destroyingState = destroyingState;
            callback();
            DisposeManager.destroyingState = lastDestroyingType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TriggerOnDestroyIfNull(IScriptableBehaviour scriptableBehaviour)
        {
            if (!Object.ReferenceEquals(scriptableBehaviour, null) && (scriptableBehaviour as UnityEngine.Object) == null)
            {
                DestroyingState(() => { scriptableBehaviour.OnDestroy(); }, DestroyContext.Editor_UndoRedo);
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

        private static void IterateOverAllMonoBehaviourBase(UnityEngine.Object obj, Action<MonoBehaviourBase> callback)
        {
            if (obj is GameObject)
                IterateOverAllMonoBehaviourBase(obj as GameObject, callback);
            else if (obj is MonoBehaviourBase)
                callback(obj as MonoBehaviourBase);
        }

        private static void IterateOverAllMonoBehaviourBase(GameObject go, Action<MonoBehaviourBase> callback)
        {
            MonoBehaviourBase[] monoBehaviourBases = go.GetComponents<MonoBehaviourBase>();
            foreach (MonoBehaviourBase monoBehaviourBase in monoBehaviourBases)
                callback(monoBehaviourBase);
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
