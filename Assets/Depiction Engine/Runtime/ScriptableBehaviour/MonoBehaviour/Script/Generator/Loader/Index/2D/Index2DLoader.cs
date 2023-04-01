// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
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

            _indices?.Clear();
            _indexReferences?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                _indices?.Clear();
                _indexReferences?.Clear();
            }

            InitValue(value => indices = value, new List<Grid2DIndex>(), initializingContext);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                lastIndexReferences.Clear();
                for (int i = indexReferences.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<Grid2DIndex, ReferencesList> keyValuePair = indexReferences.ElementAt(i);
                    ReferencesList referencesList = new();
                    lastIndexReferences.Add(keyValuePair.Key, referencesList);
                    for (int e = keyValuePair.Value.Count - 1; e >= 0; e--)
                        referencesList.Add(keyValuePair.Value[e]);
                }
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private IndexReferencesDictionary _lastIndexReferences;
        private IndexReferencesDictionary lastIndexReferences
        {
            get => _lastIndexReferences ??= new();
        }

        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            UndoRedoPerformedReferencesLoadScopes(indexReferences, lastIndexReferences, index2DLoadScopes, lastIndex2DLoadScopes);
        }
#endif

        protected override void IterateOverLoadScopeKeys(Action<int, object, ReferencesList> callback)
        {
            base.IterateOverLoadScopeKeys(callback);

            if (callback != null)
            {
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    Grid2DIndex index = indices[i];

                    indexReferences.TryGetValue(index, out ReferencesList references);

                    callback(i, index, references);
                }
            }
        }

        /// <summary>
        /// The list of indices to load. 
        /// </summary>
        [Json]
        protected List<Grid2DIndex> indices
        {
            get 
            { 
                _indices ??= new List<Grid2DIndex>();
                return _indices; 
            }
            set { SetValue(nameof(indices), value, ref _indices); }
        }

        private IndexReferencesDictionary indexReferences
        {
            get => _indexReferences ??= new IndexReferencesDictionary();
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

        public override bool AddReference(object loadScopeKey, ReferenceBase reference)
        {
            Grid2DIndex scopeGrid2DIndex = (Grid2DIndex)loadScopeKey;
            if (scopeGrid2DIndex != Grid2DIndex.Empty)
            {
#if UNITY_EDITOR
                if (!lastIndexReferences.TryGetValue(scopeGrid2DIndex, out ReferencesList lastReferences))
                {
                    lastReferences = new ReferencesList();
                    lastIndexReferences[scopeGrid2DIndex] = lastReferences;
                }

                if (!lastReferences.Contains(reference))
                    lastReferences.Add(reference);
#endif

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
            return false;
        }

        public override bool RemoveReference(object loadScopeKey, ReferenceBase reference = null, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.RemoveReference(loadScopeKey, reference, disposeContext))
            {
                Grid2DIndex loadScopeGrid2DIndex = (Grid2DIndex)loadScopeKey;
                if (loadScopeGrid2DIndex != Grid2DIndex.Empty)
                {
#if UNITY_EDITOR
                    if (disposeContext == DisposeContext.Editor_Destroy)
                        Editor.UndoManager.RecordObject(this);

                    if (lastIndexReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList lastReferences))
                    {
                        if (lastReferences.IsEmpty() || lastReferences.Remove(reference))
                            lastIndexReferences.Remove(loadScopeGrid2DIndex);
                    }
#endif

                    if (indexReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList references))
                    {
                        if (references.IsEmpty() || references.Remove(reference))
                        {
                            indexReferences.Remove(loadScopeGrid2DIndex);

                            RemoveIndex(loadScopeGrid2DIndex);

                            if (GetLoadScope(out LoadScope loadScope, loadScopeKey))
                                DisposeLoadScope(loadScope, disposeContext);

                            return true;
                        }
                    }

#if UNITY_EDITOR
                    if (disposeContext == DisposeContext.Editor_Destroy)
                        Editor.UndoManager.FlushUndoRecordObjects();
#endif
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
