// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/" + nameof(Index2DLoader))]
    public class Index2DLoader : Index2DLoaderBase
    {
        [Serializable]
        private class IndexReferencesDictionary : SerializableDictionary<Grid2DIndex, ReferencesList> { };

        [BeginFoldout("Indices")]
        [SerializeField, Tooltip("The list of indices to load."), EndFoldout]
        private List<Grid2DIndex> _indices;

        [SerializeField, HideInInspector]
        private IndexReferencesDictionary _indexReferences;

        public override void Recycle()
        {
            base.Recycle();

            if (_indexReferences != null)
                _indexReferences.Clear();
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => indices = value, new List<Grid2DIndex>(), initializingState);
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                if (_indices != null && _indexReferences != null)
                {
                    for (int i = _indices.Count - 1; i >= 0; i--)
                    {
                        Grid2DIndex index = _indices[i];

                        if (_indexReferences.TryGetValue(index, out ReferencesList references))
                        {
                            if (references.RemoveNullReferences())
                            {
                                _indexReferences.Remove(index);
                                _indices.RemoveAt(i);

                                if (GetLoadScope(out Index2DLoadScope loadScope, index.dimensions, index.index))
                                    DisposeLoadScope(loadScope);
                            }
                        }
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// The list of indices to load. 
        /// </summary>
        [Json]
        protected List<Grid2DIndex> indices
        {
            get 
            { 
                if (_indices == null)
                    _indices = new List<Grid2DIndex>();
                return _indices; 
            }
            set { SetValue(nameof(indices), value, ref _indices); }
        }

        private IndexReferencesDictionary indexReferences
        {
            get
            {
                if (_indexReferences == null)
                    _indexReferences = new IndexReferencesDictionary();
                return _indexReferences;
            }
        }

        public bool AddIndex(Grid2DIndex index)
        {
            if (!indices.Contains(index))
            {
                indices.Add(index);

                PropertyAssigned(this, nameof(indices), indices, null);

                return true;
            }
            return false;
        }

        public bool RemoveIndex(Grid2DIndex index)
        {
            if (indices.Remove(index))
            {
                PropertyAssigned(this, nameof(indices), indices, null);

                return true;
            }
            return false;
        }

        public override bool AddReference(LoadScope loadScope, ReferenceBase reference)
        {
            Index2DLoadScope index2DLoadScope = loadScope as Index2DLoadScope;
            if (index2DLoadScope != Disposable.NULL)
            {
                Grid2DIndex scopeGrid2DIndex = index2DLoadScope.scopeGrid2DIndex;
                if (scopeGrid2DIndex != Grid2DIndex.empty)
                {
                    if (!indexReferences.TryGetValue(scopeGrid2DIndex, out ReferencesList references))
                    {
                        references = new ReferencesList();
                        indexReferences[scopeGrid2DIndex] = references;
                    }

                    if (!references.Contains(reference))
                    {
                        references.Add(reference);

                        AddIndex(scopeGrid2DIndex);

                        return true;
                    }
                }
            }
            return false;
        }

        public override bool RemoveReference(LoadScope loadScope, ReferenceBase reference)
        {
            Index2DLoadScope index2DLoadScope = loadScope as Index2DLoadScope;
            if (index2DLoadScope != Disposable.NULL)
            {
                Grid2DIndex scopeGrid2DIndex = index2DLoadScope.scopeGrid2DIndex;
                if (scopeGrid2DIndex != Grid2DIndex.empty)
                {
                    if (indexReferences.TryGetValue(scopeGrid2DIndex, out ReferencesList references) && references.Remove(reference))
                    {
                        if (references.IsEmpty())
                        {
                            indexReferences.Remove(scopeGrid2DIndex);

                            RemoveIndex(scopeGrid2DIndex);
                            DisposeLoadScope(loadScope);
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        public override bool IsInList(LoadScope loadScope)
        {
            if (base.IsInList(loadScope))
            {
                Index2DLoadScope indexLoadScope = loadScope as Index2DLoadScope;

                if (indexLoadScope != Disposable.NULL)
                    return indices.Contains(indexLoadScope.scopeGrid2DIndex);
            }
            return false;
        }

        protected override void IterateOverLoadScopeList(Action<Vector2Int, Vector2Int, Vector2Int> callback)
        {
            foreach (Grid2DIndex grid2DProperties in indices)
                callback(grid2DProperties.index, grid2DProperties.dimensions, Vector2Int.zero);
        }
    }
}
