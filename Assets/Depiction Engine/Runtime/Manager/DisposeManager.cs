// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// The different types of dispose context. <br/><br/>
    /// <b><see cref="Editor_Unknown"/>:</b> <br/>
    /// The context can be either 'Editor' or 'Editor_UndoRedo'. Used internally. <br/><br/>
    /// <b><see cref="Programmatically_Destroy"/>:</b> <br/>
    /// The dispose was triggered programmatically and the object should be destroyed. <br/><br/>
    /// <b><see cref="Programmatically_Pool"/>:</b> <br/>
    /// The dispose was triggered programmatically and the object should be pooled. <br/><br/>
    /// <b><see cref="Editor_Destroy"/>:</b> <br/>
    /// The destroy was triggered in the editor. <br/><br/>
    /// <b><see cref="Editor_UndoRedo"/>:</b> <br/>
    /// The destroy was triggered in the editor as a result of an undo or redo action. <br/><br/>
    /// </summary> 
    public enum DisposeContext
    {
        Editor_Unknown,
        Programmatically_Destroy,
        Programmatically_Pool,
        Editor_Destroy,
        Editor_UndoRedo,
    };

    /// <summary>
    /// Manager handling the pooling or destruction of objects.
    /// </summary>
    public static class DisposeManager
    {
        public static DisposeContext disposingContext = DisposeContext.Editor_Unknown;

        private static List<Type> _requiredComponentTypes = new();

        /// <summary>
        /// Will dispose of an object by sending it to the pool if pooling is enabled, it implements <see cref="DepictionEngine.IDisposable"/> and no <see cref="DepictionEngine.DisposeContext"/> is specified. Otherwise it will be destroyed.
        /// </summary>
        /// <param name="obj">The object to dispose.</param>
        public static void Dispose(object obj)
        {
            Dispose(obj, DisposeContext.Programmatically_Pool);
        }

        /// <summary>
        /// Will dispose of an object by sending it to the pool if pooling is enabled, it implements <see cref="DepictionEngine.IDisposable"/> and no <see cref="DepictionEngine.DisposeContext"/> is specified. Otherwise it will be destroyed.
        /// </summary>
        /// <param name="obj">The object to dispose.</param>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        public static void Dispose(object obj, DisposeContext disposeContext)
        {
            if (obj is null || obj.Equals(null) || SceneManager.IsSceneBeingDestroyed())
                return;

            //Force Destroy if object is MonoBehaviour or Pooling is not enabled
            PoolManager poolManager = PoolManager.Instance(false);
            if (SceneManager.sceneClosing || obj is MonoBehaviour || poolManager == Disposable.NULL || !poolManager.enablePooling)
                disposeContext = disposeContext == DisposeContext.Editor_Destroy ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Destroy;
            else
            {
                if (disposeContext == DisposeContext.Editor_Unknown || disposeContext == DisposeContext.Editor_UndoRedo)
                    disposeContext = DisposeContext.Programmatically_Destroy;
            }

            List<(object, DisposeContext)> disposingData = new();

            IterateOverAllObjects(obj, (obj) =>
            {
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;
                    DisposeContext goDisposeContext = disposeContext;

                    if (goDisposeContext == DisposeContext.Programmatically_Pool)
                    {
                        Component[] components = go.GetComponents<Component>();
                        IDisposable disposable = go.GetDisposableInComponents(components);

                        if (disposable is not null)
                        {
#if UNITY_EDITOR
                            //if some components are not compatible with pooling we destroy the object
                            foreach (Component component in components)
                            {
                                if (component is MonoBehaviourDisposable)
                                {
                                    if ((component as MonoBehaviourDisposable).notPoolable)
                                    {
                                        goDisposeContext = DisposeContext.Programmatically_Destroy;
                                        break;
                                    }
                                }
                            }
#endif
                            //Destroy components that are not required
                            if (goDisposeContext == DisposeContext.Programmatically_Pool && disposable is IRequiresComponents)
                            {
                                (disposable as IRequiresComponents).GetRequiredComponentTypes(ref _requiredComponentTypes);
                                foreach (Component component in components)
                                {
                                    if (!Object.ReferenceEquals(component,disposable) && component is not Transform && !_requiredComponentTypes.Remove(component.GetType()))
                                        Destroy(component);
                                }
                            }
                        }
                        else
                            goDisposeContext = DisposeContext.Programmatically_Destroy;
                    }

                    bool monoBehaviourDisposableDisposing = false;
                    MonoBehaviourDisposable[] monoBehaviourDisposables = go.GetComponents<MonoBehaviourDisposable>();
                    if (monoBehaviourDisposables.Length != 0)
                    {
                        foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                        {
                            DisposingContext(() =>
                            {
                                if (monoBehaviourDisposable.OnDisposing() && monoBehaviourDisposable.UpdateDisposingContext())
                                    monoBehaviourDisposableDisposing = true;
                            }, goDisposeContext);
                        }
                    }
                    else
                        monoBehaviourDisposableDisposing = true;

                    if (monoBehaviourDisposableDisposing)
                        disposingData.Add((go, goDisposeContext));
                }
                else if (obj is IDisposable)
                {
                    IDisposable disposable = obj as IDisposable;

#if UNITY_EDITOR
                    if (disposable is IScriptableBehaviour && (disposable as IScriptableBehaviour).notPoolable)
                        disposeContext = disposeContext == DisposeContext.Editor_Destroy ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Destroy;
#endif

                    DisposingContext(() =>
                    {
                        if (disposable.OnDisposing() && disposable.UpdateDisposingContext())
                            disposingData.Add((obj, disposeContext));
                    }, disposeContext);
                }
                else
                    disposingData.Add((obj, disposeContext == DisposeContext.Editor_Destroy ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Destroy));
            });

            foreach ((object, DisposeContext) disposingObject in disposingData)
                Dispose(disposingObject.Item1, disposingObject.Item2);

            void Dispose(object obj, DisposeContext disposeContext)
            {
                if (IsNullOrDisposed(obj))
                    return;

                if (disposeContext != DisposeContext.Programmatically_Pool && obj is UnityEngine.Object)
                {
                    //Destroy
                    Destroy(obj as UnityEngine.Object, disposeContext);
                }
                else
                {
                    IDisposable disposable = null;
                    if (obj is GameObject)
                        disposable = (obj as GameObject).GetDisposableInComponents();
                    else if (obj is IDisposable)
                        disposable = obj as IDisposable;

                    if (!IsNullOrDisposed(disposable))
                    {
                        if (disposeContext == DisposeContext.Programmatically_Pool)
                        {
                            //Pool
                            PoolManager poolManager = PoolManager.Instance();
                            if (poolManager != null)
                                poolManager.AddToPool(disposable);

                            if (obj is GameObject)
                                IterateOverAllMonoBehaviourDisposable(obj as GameObject, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDisposeInternal(disposeContext); });
                            else
                                disposable.OnDisposeInternal(disposeContext);

                            disposable.poolComplete = true;
                        }
                        else
                        {
                            //Dispose IDisposable that do not inherit from UnityEngine.Object
                            disposable.OnDisposeInternal(disposeContext);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Destroy any UnityEngine.Object.
        /// </summary>
        /// <param name="unityObject">The UnityEngine.Object.</param>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        public static void Destroy(UnityEngine.Object unityObject, DisposeContext disposeContext = DisposeContext.Programmatically_Destroy)
        {
            if (unityObject == null)
                return;
   
            if (disposeContext != DisposeContext.Editor_Destroy)
                disposeContext = DisposeContext.Programmatically_Destroy;

            DisposingContext(() =>
            {
                //MonoBehaviourDisposable who have not been activated yet will not trigger OnDestroy so we do it manually
                //We call OnDestroy before we trigger the Destroy because OnDestroy will not be triggered if the Destroy happens within the Object Awake() call.
                //Features such as Object.CanBeDuplicated() will Invalidate the initialization and delete the object within Awake().
                IterateOverAllObjects(unityObject, (obj) => { IterateOverAllMonoBehaviourDisposable(obj as UnityEngine.Object, (monoBehaviourDisposable) => { monoBehaviourDisposable.OnDestroy(); }); });

#if UNITY_EDITOR
                if (disposeContext != DisposeContext.Editor_Destroy || !Editor.UndoManager.DestroyObjectImmediate(unityObject))
                    GameObject.DestroyImmediate(unityObject, true);
#else
                GameObject.Destroy(unityObject);
#endif
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

        public static void DisposingContext(Action callback, DisposeContext disposingContext)
        {
            DisposeContext lastDestroyingType = DisposeManager.disposingContext;
            DisposeManager.disposingContext = disposingContext;
            callback();
            DisposeManager.disposingContext = lastDestroyingType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TriggerOnDestroyIfNull(IScriptableBehaviour scriptableBehaviour)
        {
            if (scriptableBehaviour is not null && (scriptableBehaviour as UnityEngine.Object) == null)
            {
                DisposingContext(() => { scriptableBehaviour.OnDestroy(); }, DisposeContext.Editor_UndoRedo);
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
    }
}
