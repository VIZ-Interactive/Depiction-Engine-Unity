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

        public static string NEW_GAME_OBJECT_NAME = "New Game Object";
        public static string NEW_SCRIPT_OBJECT_NAME = "New Script Object";

        [BeginFoldout("Pool")]
        [SerializeField, Tooltip("When enabled, pooling improves performance by reusing the instances to reduce the number of expensive operations such as object creation or garbage collection. The trade-off is an increased memory footprint.")]
        public bool _enablePooling;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(ClearPoolBtn)), ConditionalShow(nameof(GetShowClearPool)), Tooltip("Destroy all pooled objects."), EndFoldout]
        public bool _clearPool;
#endif

        [BeginFoldout("Dynamic Resizing")]
        [SerializeField, Tooltip("How many instances of each type we can keep before we start destroying them.")]
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

        private bool GetShowDebug()
        {
            SceneManager sceneManager = SceneManager.Instance(false);
            if (sceneManager != Disposable.NULL)
                return sceneManager.debug;
            return false;
        }

        private PoolStackDictionary debug
        { 
            get 
            {
                if (_debug == null)
                    _debug = new PoolStackDictionary();
                return _debug; 
            }
        }

        private void ClearPoolBtn()
        {
            ClearPool();
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
            if (_instance == Disposable.NULL && createIfMissing)
                _instance = GetManagerComponent<PoolManager>();
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

        private void InitTypes()
        {
            if (_types == null)
                _types = new List<Type>();
        }

        private void InitPools()
        {
            if (_pools == null)
                _pools = new Dictionary<int, List<IDisposable>>();
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            StartDynamicResizing();
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => enablePooling = value, true, initializingState);
            InitValue(value => maxSize = value, 150, initializingState);
            InitValue(value => resizeInterval = value, 10.0f, initializingState);
            InitValue(value => destroyCount = value, 50, initializingState);
        }

        /// <summary>
        /// When enabled, pooling improves performance by reusing the instances to reduce the number of expensive operations such as object creation or garbage collection. The trade-off is an increased memory footprint.
        /// </summary>
        [Json]
        public bool enablePooling
        {
            get { return _enablePooling; }
            set
            {
                SetValue(nameof(enablePooling), value, ref _enablePooling, (newValue, oldValue) =>
                {
                    if (!newValue)
                        ClearPool();
                });
            }
        }

        /// <summary>
        /// How many instances of each type we can keep before we start destroying them.
        /// </summary>
        [Json]
        public int maxSize
        {
            get { return _maxSize; }
            set { SetValue(nameof(maxSize), value, ref _maxSize); }
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="ResizePools"/> function.
        /// </summary>
        [Json]
        public float resizeInterval
        {
            get { return _resizeInterval; }
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
        /// How many instances should we destroy at a time during each <see cref="ResizePools"/> calls.
        /// </summary>
        [Json]
        public int destroyCount
        {
            get { return _destroyCount; }
            set { SetValue(nameof(destroyCount), value, ref _destroyCount); }
        }

        private Tween resizeTimer
        {
            get { return _resizeTimer; }
            set
            {
                if (Object.ReferenceEquals(_resizeTimer, value))
                    return;

                Dispose(_resizeTimer);

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
            InitPools();
            if (_pools != null)
            {
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

                    //Make sure GameObject is visible
                    UnityEditor.SceneVisibilityManager.instance.Show(go, true);

                    //Make sure GameObject is pickable
                    UnityEditor.SceneVisibilityManager.instance.EnablePicking(go, true);
#endif
                }

                if (disposable != null)
                {
                    int typeHashCode = GetHashCodeFromType(disposable.GetType());
                    List<IDisposable> pool;
                    lock (_pools)
                    {
                        if (!_pools.TryGetValue(typeHashCode, out pool))
                            _pools[typeHashCode] = pool = new List<IDisposable>();
                        lock (pool)
                        {
                            pool.Add(disposable);
#if UNITY_EDITOR
                            UpdateDebug(disposable.GetType(), pool);
#endif
                        }
                    }
                }
            }
        }

        public IDisposable GetFromPool(Type type)
        {
            if (type == null)
                return null;

            IDisposable disposable = null;

            if (_pools != null)
            {
                List<IDisposable> pool;
                lock (_pools)
                {
                    if (_pools.TryGetValue(GetHashCodeFromType(type), out pool))
                    {
                        lock (pool)
                        {
                            if (pool.Count > 0)
                            {
                                int index = -1;

                                for (int i = pool.Count - 1; i >= 0; i--)
                                {
                                    IDisposable disposed = pool[i];
                                    if ((!(disposed is IMultithreadSafe) || !(disposed as IMultithreadSafe).locked) && disposed.disposedComplete)
                                    {
                                        index = i;
                                        break;
                                    }
                                }

                                disposable = RemoveFromPool(pool, index);

                                if (!Object.ReferenceEquals(disposable, null))
                                {
#if UNITY_EDITOR
                                    UpdateDebug(type, pool);
#endif
                                    if (disposable is MonoBehaviour)
                                    {
                                        GameObject go = (disposable as MonoBehaviour).gameObject;
                                        MonoBehaviourBase[] monoBehaviourBases = go.GetComponents<MonoBehaviourBase>();
                                        foreach (MonoBehaviourBase monoBehaviourBase in monoBehaviourBases)
                                            monoBehaviourBase.Recycle();
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

        protected override void IterateOverChildrenAndSiblings(Action<PropertyMonoBehaviour> callback)
        {

        }

        protected override void IterateOverChildren(Action<PropertyMonoBehaviour> callback)
        {

        }

        public int GetHashCodeFromType(Type type)
        {
            int index = 0;
            InitTypes();
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
                            if (disposable.disposedComplete)
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

#if UNITY_EDITOR
        private void UpdateDebug(Type type, List<IDisposable> pool)
        {
            if (GetShowDebug() && pool != null)
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
        public void ClearPool()
        {
            if (_pools != null)
            {
                try
                {
                    foreach (int key in _pools.Keys)
                    {
                        foreach (IDisposable disposable in _pools[key])
                            Destroy(disposable);
                    }
                }
                catch(InvalidOperationException e)
                {
                    Debug.LogError(e);
                }
               
                _pools.Clear();
            }

            if (_types != null)
                _types.Clear();

#if UNITY_EDITOR
            if (_debug != null)
                _debug.Clear();
#endif
        }

        private void Destroy(IDisposable disposable)
        {
            object obj = disposable;

            if (disposable is MonoBehaviour)
                obj = (disposable as MonoBehaviour).gameObject;

            if (obj is UnityEngine.Object)
                DisposeManager.Destroy(obj as UnityEngine.Object);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ClearPool();
        }

        public override bool OnDisposing()
        {
            if (base.OnDisposing())
            {
                resizeTimer = null;

                return true;
            }
            return false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            ClearPool();
        }
    }
}
