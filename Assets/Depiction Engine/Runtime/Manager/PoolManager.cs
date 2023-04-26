// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing the pooling of objects.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(PoolManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class PoolManager : ManagerBase
    {
        [Serializable]
        private class PoolStackDictionary : SerializableDictionary<Type, string> { };

        [BeginFoldout("Pool")]
        [SerializeField, Tooltip("When enabled, pooling improves performance by reusing the instances to reduce the number of expensive operations such as object creation or garbage collection. The trade-off is an increased memory footprint.")]
        private bool _enablePooling;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(ClearPoolBtn)), ConditionalShow(nameof(GetShowClearPool)), Tooltip("Destroy all pooled objects."), EndFoldout]
        private bool _clearPool;
#endif

        [BeginFoldout("Dynamic Resizing")]
        [SerializeField, Tooltip("The maximum number of instances of each type we can keep. The excess instances will be deleted.")]
        private int _maxSize;
        [SerializeField, Tooltip("The interval (in seconds) at which we call the '"+nameof(ResizePools)+"' function.")]
        private float _resizeInterval;
        [SerializeField, Tooltip("How many instances should we destroy at a time during each '"+nameof(ResizePools)+"' calls."), EndFoldout]
        private int _destroyCount;

        private Dictionary<int, List<IDisposable>> _pools;
        private List<Type> _types;

        private Tween _resizeTimer;

#if UNITY_EDITOR
        [SerializeField, ConditionalShow(nameof(GetShowDebug))]
        private PoolStackDictionary _debug;

        private PoolStackDictionary debug
        {
            get { _debug ??= new PoolStackDictionary(); return _debug; }
        }

        private void ClearPoolBtn()
        {
            DestroyAllDisposables();
        }

        private bool GetShowClearPool()
        {
            return enablePooling;
        }
#endif

        private static PoolManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        public static PoolManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<PoolManager>(createIfMissing);
            return _instance;
        }

#if UNITY_EDITOR
        private MethodInfo _setExpandedRecursiveMethodInfo;
        private UnityEditor.EditorWindow _hierarchyWindow;

        private bool InitSetExpandedMethod()
        {
            if (_setExpandedRecursiveMethodInfo == null || _hierarchyWindow == null)
            {
                Type sceneHierarchyWindowType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                _setExpandedRecursiveMethodInfo = sceneHierarchyWindowType.GetMethod("SetExpandedRecursive");
                UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(sceneHierarchyWindowType);
                _hierarchyWindow = wins.Length > 0 ? (wins[0] as UnityEditor.EditorWindow) : null;
                return _setExpandedRecursiveMethodInfo != null && _hierarchyWindow != null;
            }
            return true;
        }
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => enablePooling = value, true, initializingContext);
            InitValue(value => maxSize = value, 150, initializingContext);
            InitValue(value => resizeInterval = value, 10.0f, initializingContext);
            InitValue(value => destroyCount = value, 50, initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            StartDynamicResizing();
        }

#if UNITY_EDITOR
        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                StartDynamicResizing();

                return true;
            }
            return false;
        }

        protected override void DebugChanged()
        {
            base.DebugChanged();

            IterateOverDisposable((disposable) => { UpdateHideFlags(disposable); });
        }
#endif

        private static void UpdateHideFlags(IDisposable disposable)
        {
            UnityEngine.Object unityObject = GetUnityObject(disposable);
            if (unityObject != null)
            {
                unityObject.hideFlags = SceneManager.Debugging() ? HideFlags.None : HideFlags.HideInHierarchy;
                unityObject.hideFlags |= HideFlags.DontSave;
            }
        }

        /// <summary>
        /// When enabled, pooling improves performance by reusing the instances to reduce the number of expensive operations such as object creation or garbage collection. The trade-off is an increased memory footprint.
        /// </summary>
        [Json]
        public bool enablePooling
        {
            get => _enablePooling;
            set
            {
                SetValue(nameof(enablePooling), value, ref _enablePooling, (newValue, oldValue) =>
                {
                    if (initialized && !newValue)
                        DestroyAllDisposables();
                });
            }
        }

        /// <summary>
        /// The maximum number of instances of each type we can keep. The excess instances will be deleted.
        /// </summary>
        [Json]
        public int maxSize
        {
            get => _maxSize;
            set => SetValue(nameof(maxSize), value, ref _maxSize);
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="DepictionEngine.PoolManager.ResizePools"/> function.
        /// </summary>
        [Json]
        public float resizeInterval
        {
            get => _resizeInterval;
            set
            {
                if (value <= 0.0f)
                    value = 0.01f;
                SetValue(nameof(resizeInterval), value, ref _resizeInterval, (newValue, oldValue) =>
                {
                    StartDynamicResizing();
                });
            }
        }

        /// <summary>
        /// How many instances should we destroy at a time during each <see cref="DepictionEngine.PoolManager.ResizePools"/> calls to bring use closer to our <see cref="DepictionEngine.PoolManager.maxSize"/>.
        /// </summary>
        [Json]
        public int destroyCount
        {
            get => _destroyCount;
            set => SetValue(nameof(destroyCount), value, ref _destroyCount);
        }

        public override void IterateOverChildrenAndSiblings(Action<PropertyMonoBehaviour> callback)
        {

        }

        protected override void IterateOverChildren(Action<PropertyMonoBehaviour> callback)
        {

        }

        private Tween resizeTimer
        {
            get => _resizeTimer;
            set
            {
                if (Object.ReferenceEquals(_resizeTimer, value))
                    return;

                DisposeManager.Dispose(_resizeTimer);

                _resizeTimer = value;
            }
        }

        private void StartDynamicResizing()
        {
            if (resizeInterval != 0.0f)
            {
                TweenManager tweenManager = TweenManager.Instance();
                if (tweenManager != Disposable.NULL)
                    resizeTimer = tweenManager.DelayedCall(resizeInterval, null, ResizePools);
            }
        }

        public void AddToPool(IDisposable disposable)
        {
            if (!enablePooling)
                return;

            if (disposable != null)
            {
                int typeHashCode = GetHashCodeFromType(disposable.GetType());

                _pools ??= new Dictionary<int, List<IDisposable>>();
                lock (_pools)
                {
                    if (!_pools.TryGetValue(typeHashCode, out List<IDisposable> pool))
                        _pools[typeHashCode] = pool = new List<IDisposable>();
                    lock (pool)
                    {
                        pool.Add(disposable);

                        if (disposable is Object || disposable is Visual)
                        {
                            GameObject go = (disposable as MonoBehaviour).gameObject;

#if UNITY_EDITOR
                            //Deselect the GameObject if it is Selected
                            if (UnityEditor.Selection.activeGameObject == go)
                                UnityEditor.Selection.activeGameObject = null;
#endif

                            go.SetActive(false);
                            go.transform.SetParent(transform, false);

#if UNITY_EDITOR
                            //Make sure GameObject is not expanded in the hierarchy
                            if (InitSetExpandedMethod())
                            {
                                _setExpandedRecursiveMethodInfo.Invoke(_hierarchyWindow, new object[] { go.GetInstanceID(), false });
                                _setExpandedRecursiveMethodInfo.Invoke(_hierarchyWindow, new object[] { gameObject.GetInstanceID(), false });
                            }
#endif
                        }

                        UpdateHideFlags(disposable);
#if UNITY_EDITOR
                        UpdateDebug(disposable.GetType(), pool);
#endif
                    }
                }
            }
        }

        public IDisposable GetFromPool(Type type)
        {
            if (!enablePooling || type == null)
                return null;

            IDisposable disposable = null;

            if (_pools != null)
            {
                lock (_pools)
                {
                    if (_pools.TryGetValue(GetHashCodeFromType(type), out List<IDisposable> pool))
                    {
                        lock (pool)
                        {
                            if (pool.Count > 0)
                            {
                                int index = -1;

                                for (int i = pool.Count - 1; i >= 0; i--)
                                {
                                    IDisposable disposed = pool[i];
                                    if ((disposed is not IMultithreadSafe || !(disposed as IMultithreadSafe).locked) && disposed.poolComplete)
                                    {
                                        index = i;
                                        break;
                                    }
                                }

                                disposable = RemoveFromPool(pool, index);

                                if (disposable is not null)
                                {
#if UNITY_EDITOR
                                    UpdateDebug(type, pool);
#endif
                                    if (disposable is MonoBehaviour)
                                    {
                                        GameObject go = (disposable as MonoBehaviour).gameObject;
                                        go.SetActive(true);
#if UNITY_EDITOR
                                        //Make sure GameObject is visible
                                        if (UnityEditor.SceneVisibilityManager.instance.IsHidden(go, true))
                                            UnityEditor.SceneVisibilityManager.instance.Show(go, true);

                                        //Make sure GameObject is pickable
                                        if (UnityEditor.SceneVisibilityManager.instance.IsPickingDisabled(go, true))
                                            UnityEditor.SceneVisibilityManager.instance.EnablePicking(go, true);
#endif

                                        MonoBehaviourDisposable[] monoBehaviourDisposables = go.GetComponents<MonoBehaviourDisposable>();
                                        foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                                            monoBehaviourDisposable.Recycle();
                                    }
                                    else
                                        disposable.Recycle();
                                }
                            }
                        }
                    }
                }
            }

            return disposable;
        }

        private IDisposable RemoveFromPool(List<IDisposable> pool, int index)
        {
            IDisposable disposable = null;

            if (index != -1)
            {
                disposable = pool[index];
                pool.RemoveAt(index);
            }

            return disposable;
        }

        public int GetHashCodeFromType(Type type)
        {
            int index = 0;
            _types ??= new List<Type>();
            lock (_types)
            {
                index = _types.IndexOf(type);
                if (index == -1)
                {
                    _types.Add(type);
                    index = _types.Count - 1;
                }
            }
            return index;
        }

        private void ResizePools()
        {
            StartDynamicResizing();

            if (_pools != null)
            {
                foreach (List<IDisposable> pool in _pools.Values)
                {
                    int popCount = pool.Count - destroyCount <= maxSize ? pool.Count - maxSize : destroyCount;
                    for (int i = 0; i < popCount; i++)
                    {
                        if (i >= 0 && i < pool.Count)
                        {
                            IDisposable disposable = pool[i];
                            if (disposable.poolComplete)
                            {
                                pool.RemoveAt(i);
                                Destroy(disposable);
#if UNITY_EDITOR
                                UpdateDebug(disposable.GetType(), pool);
#endif
                            }
                        }
                        else
                            break;
                    }
                }
            }
        }

        private void IterateOverDisposable(Action<IDisposable> callback)
        {
            if (_pools != null)
            {
                foreach (List<IDisposable> pool in _pools.Values)
                {
                    for (int i = pool.Count - 1; i >= 0; i--)
                        callback?.Invoke(pool[i]);
                }
            }
        }

#if UNITY_EDITOR
        private void UpdateDebug(Type type, List<IDisposable> pool)
        {
            SceneManager sceneManager = SceneManager.Instance(false);
            if (sceneManager != Disposable.NULL && pool != null)
            {
                lock (debug)
                {
                    debug[type] = type + " (" + pool.Count + " Instances)";
                }
            }
        }
#endif

        /// <summary>
        /// Destroy all pooled objects.
        /// </summary>
        public void DestroyAllDisposables()
        {
            try
            {
                IterateOverDisposable((disposable) => 
                { 
                    Destroy(disposable); 
                });
            }
            catch(InvalidOperationException e)
            {
                Debug.LogError(e);
            }

            _pools?.Clear();
            _types?.Clear();

#if UNITY_EDITOR
            _debug?.Clear();
#endif
        }

        private static UnityEngine.Object GetUnityObject(IDisposable disposable)
        {
            if (disposable is UnityEngine.Object unityOject)
            {
                if (unityOject is Component component)
                    unityOject = component.gameObject;

                return unityOject;
            }
            return null;
        }

        private void Destroy(IDisposable disposable)
        {
            DisposeManager.Destroy(GetUnityObject(disposable));
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                resizeTimer = null;

                DestroyAllDisposables();

                return true;
            }
            return false;
        }
    }
}
