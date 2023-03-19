// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    [RequireComponent(typeof(DatasourceRoot))]
    public class Index2DLoaderBase : LoaderBase
    {
        [Serializable]
        private class IndexLoadScopeDictionary : SerializableDictionary<int, Index2DLoadScope> { };

        public const int MAX_ZOOM = 30;

        [BeginFoldout("Index")]
        [SerializeField, MinMaxRange(0.0f, MAX_ZOOM), Tooltip("A min and max clamping values for zoom.")]
        private Vector2Int _minMaxZoom;
        [SerializeField, Tooltip("Which type of values to send as URL parameters to the "+nameof(RestDatasource)+" to receive the proper tile.")]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnabledIndexUrlParamType))]
#endif
        private Index2DLoadScope.URLParametersType _indexUrlParamType;
        [SerializeField, Tooltip("The horizontal to vertical tiles ratio. A ratio of 2.0 means the grid contains twice as many tiles horizontally then vertically. A ratio of 0.5 means the grid contains twice as many tiles vertically then horizontally. The max value is 10.0 and min value is 0.1.")]
        private float _xyTilesRatio;

        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private IndexLoadScopeDictionary _index2DLoadScopes;

#if UNITY_EDITOR
        protected virtual bool GetEnabledIndexUrlParamType()
        {
            return isFallbackValues || datasource is RestDatasource;
        }
#endif

        public override void Recycle()
        {
            base.ClearLoadScopes();

            _index2DLoadScopes?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                _index2DLoadScopes?.Clear();

            InitValue(value => minMaxZoom = value, new Vector2Int(0, 20), initializingContext);
            InitValue(value => indexUrlParamType = value, Index2DLoadScope.URLParametersType.ZoomXY, initializingContext);
            InitValue(value => xyTilesRatio = value, 1.0f, initializingContext);
        }

        protected override int GetLoadScopeCount()
        {
            return base.GetLoadScopeCount() + index2DLoadScopes.Count;
        }

        private IndexLoadScopeDictionary index2DLoadScopes
        {
            get 
            {
                _index2DLoadScopes ??= new IndexLoadScopeDictionary();
                return _index2DLoadScopes; 
            }
            set { _index2DLoadScopes = value; }
        }

        /// <summary>
        /// A min and max clamping values for zoom.
        /// </summary>
        [Json]
        public Vector2Int minMaxZoom
        {
            get { return _minMaxZoom; }
            set
            {
                SetValue(nameof(minMaxZoom), value, ref _minMaxZoom, (newValue, oldValue) =>
                {
                    QueueAutoUpdate();
                });
            }
        }

        /// <summary>
        /// Which type of values to send as URL parameters to the <see cref="DepictionEngine.RestDatasource"/> to receive the proper tile.
        /// </summary>
        [Json]
        public Index2DLoadScope.URLParametersType indexUrlParamType
        {
            get { return _indexUrlParamType; }
            set { SetValue(nameof(indexUrlParamType), value, ref _indexUrlParamType); }
        }

        /// <summary>
        /// The horizontal to vertical tiles ratio. A ratio of 2.0 means the grid contains twice as many tiles horizontally then vertically. 
        /// A ratio of 0.5 means the grid contains twice as many tiles vertically then horizontally.
        /// The max value is 10.0 and min value is 0.1.
        /// </summary>
        [Json]
        public float xyTilesRatio
        {
            get { return _xyTilesRatio; }
            set
            {
                if (value >= 1.0f)
                {
                    value = (int)value;
                    if (value > 10)
                        value = 10;
                }
                else
                {
                    if (value >= 1.0f / 2.0f)
                        value = 1.0f / 2.0f;
                    else if (value >= 1.0f / 3.0f)
                        value = 1.0f / 3.0f;
                    else if (value >= 1.0f / 4.0f)
                        value = 1.0f / 4.0f;
                    else if (value >= 1.0f / 5.0f)
                        value = 1.0f / 5.0f;
                    else if (value >= 1.0f / 6.0f)
                        value = 1.0f / 6.0f;
                    else if (value >= 1.0f / 7.0f)
                        value = 1.0f / 7.0f;
                    else if (value >= 1.0f / 8.0f)
                        value = 1.0f / 8.0f;
                    else if (value >= 1.0f / 9.0f)
                        value = 1.0f / 9.0f;
                    else
                        value = 1.0f / 10.0f;
                }

                SetValue(nameof(xyTilesRatio), value, ref _xyTilesRatio, (newValue, oldValue) =>
                {
                    QueueAutoUpdate();
                });
            }
        }

        protected Vector2Int GetGrid2DDimensionsFromZoom(int zoom)
        {
            return MathPlus.GetGrid2DDimensionsFromZoom(zoom, xyTilesRatio);
        }

        protected override bool AddLoadScopeInternal(LoadScope loadScope)
        {
            if (base.AddLoadScopeInternal(loadScope))
            {
                if (loadScope is Index2DLoadScope)
                {
                    Index2DLoadScope index2DLoadScope = loadScope as Index2DLoadScope;

                    int key = index2DLoadScope.GetHashCode();
                    if (!index2DLoadScopes.ContainsKey(key))
                    {
                        index2DLoadScopes.Add(key, index2DLoadScope);
                        return true;
                    }
                }
            }
            return false;
        }

        protected override bool RemoveLoadScopeInternal(LoadScope loadScope)
        {
            if (base.RemoveLoadScopeInternal(loadScope))
            {
                if (loadScope is Index2DLoadScope)
                {
                    Index2DLoadScope index2DLoadScope = loadScope as Index2DLoadScope;

                    if (index2DLoadScopes.Remove(index2DLoadScope.GetHashCode()))
                        return true;
                }
            }
            return false;
        }

        protected override bool RemoveLoadScopeKey(object key)
        {
            return base.RemoveLoadScopeKey(key) && index2DLoadScopes.Remove((int)key);
        }

        private bool IsValidZoom(int zoom)
        {
            return zoom >= minMaxZoom.x || zoom <= minMaxZoom.y;
        }

        public override bool GetLoadScope(out LoadScope loadScope, IPersistent persistent)
        {
            if (persistent is IGrid2DIndex)
            {
                IGrid2DIndex gridIndexObject = persistent as IGrid2DIndex;
                if (GetLoadScope(out Index2DLoadScope index2DLoadScope, gridIndexObject.grid2DDimensions, gridIndexObject.grid2DIndex))
                {
                    loadScope = index2DLoadScope;
                    return true;
                }
            }
            loadScope = null;
            return false;
        }

        /// <summary>
        /// Returns true if a loadScope exists or a new one was created.
        /// </summary>
        /// <param name="loadScope">Will be set to an existing or new loadScope.</param>
        /// <param name="dimensions"></param>
        /// <param name="index"></param>
        /// <param name="loadInterval"></param>
        /// <param name="reload">If true any pre-existing loadScope will be reloaded before being returned.</param>
        /// <param name="createIfMissing">Create a new load scope if none exists.</param>
        /// <returns></returns>
        public bool GetLoadScope(out Index2DLoadScope loadScope, Vector2Int dimensions, Vector2Int index, float loadInterval = 0.0f, bool reload = false, bool createIfMissing = false)
        {
            bool load = false;

            loadScope = null;

            if (index != Vector2Int.minusOne)
            {
                int zoom = MathPlus.GetZoomFromGrid2DDimensions(dimensions);
                if (IsValidZoom(zoom))
                {
                    if (index2DLoadScopes.TryGetValue(Index2DLoadScope.GetHashCode(dimensions, index), out loadScope) && loadScope != Disposable.NULL)
                        load = reload;
                    else if (createIfMissing)
                    {
                        if (loadScope is not null)
                            RemoveLoadScope(loadScope);

                        loadScope = CreateIndex2DLoadScope(index, dimensions);

                        if (AddLoadScope(loadScope))
                            load = true;
                    }
                }
            }

            if (loadScope != Disposable.NULL)
            {
                if (load)
                    Load(loadScope, loadInterval);

                return true;
            }
            else
                return false;
        }

        protected override List<LoadScope> GetListedLoadScopes(bool reload)
        {
            List<LoadScope> loadScopes = base.GetListedLoadScopes(reload);
            
            IterateOverLoadScopeList(
                (Vector2Int index, Vector2Int dimensions, Vector2Int centerIndex) =>
                {
                    float loadInterval = IsFullyInitialized() ? GetLoadInterval(index, dimensions, centerIndex) : 0.0f;

                    if (GetLoadScope(out Index2DLoadScope loadScope, dimensions, index, loadInterval, reload, true))
                    {
                        loadScopes ??= new List<LoadScope>();
                        loadScopes.Add(loadScope);
                    }
                });

            return loadScopes;
        }

        protected virtual float GetLoadInterval(Vector2Int index, Vector2Int dimensions, Vector2Int centerIndex)
        {
            return 0.0f;
        }

        public override bool IsInList(LoadScope loadScope)
        {
            if (base.IsInList(loadScope))
            {
                Index2DLoadScope indexLoadScope = loadScope as Index2DLoadScope;

                if (indexLoadScope != Disposable.NULL)
                    return IsValidZoom(indexLoadScope.GetZoom());
            }
            return false;
        }

        protected virtual void IterateOverLoadScopeList(Action<Vector2Int, Vector2Int, Vector2Int> callback)
        {

        }

        public override bool IterateOverLoadScopes(Func<object,LoadScope, bool> callback)
        {
            if (base.IterateOverLoadScopes(callback))
            {
                if (callback != null)
                {
                    for (int i = index2DLoadScopes.Count - 1; i >= 0; i--)
                    {
                        KeyValuePair<int, Index2DLoadScope> indexLoadScope = index2DLoadScopes.ElementAt(i);
                        if (indexLoadScope.Value != Disposable.NULL && !callback(indexLoadScope.Key, indexLoadScope.Value))
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        protected virtual Index2DLoadScope CreateIndex2DLoadScope(Vector2Int index, Vector2Int dimensions)
        { 
            return (CreateLoadScope(typeof(Index2DLoadScope)) as Index2DLoadScope).Init(this, index, dimensions, indexUrlParamType);
        }
    }
}
